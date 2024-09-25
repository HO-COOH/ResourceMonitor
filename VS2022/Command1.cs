using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using System.IO;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using VS2022;
using Microsoft.VisualStudio.Threading;

namespace ResourceMonitor
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1: AsyncPackage
    {
        private static Disk disk;

        /*Options*/


        static public ShellSettingsManager ShellSettingsManager;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("425d3eaa-9e76-410f-b38b-41b6afcd4eb0");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command1(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);//change here
            commandService.AddCommand(menuItem);
            Execute(null, null);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command1 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        static System.Windows.Controls.TextBlock s_textBlock;
        static VS2022.StatusBarInjector s_injector;
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command1(package, commandService);

            var service = (await Instance.ServiceProvider.GetServiceAsync(typeof(SVsSettingsManager))) as IVsSettingsManager;
            ShellSettingsManager =  new ShellSettingsManager(service);

            s_textBlock = new System.Windows.Controls.TextBlock()
            {
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            s_injector = new VS2022.StatusBarInjector(System.Windows.Application.Current.MainWindow);
            s_injector.InjectControl(s_textBlock);
        }



        private static string SizeUnitToStr(SizeUnit unit)
        {
            switch (unit)
            {
                case SizeUnit.KB:
                    return "KB";
                case SizeUnit.MB:
                    return "MB";
                case SizeUnit.GB:
                    return "GB";
            }

            return "";
        }

        private async Task GetSolutionDir()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            try
            {
                //var env = await ServiceProvider.GetServiceAsync(typeof(SDTE)) as DTE;  //This crash in VS2022
                var env =  (EnvDTE80.DTE2)await ServiceProvider.GetServiceAsync(typeof(SDTE));
                if (env.Solution == null || env.Solution.FullName.Length == 0)
                    return;

                var solutionDir = new FileInfo(env.Solution.FullName);
                disk = solutionDir.Extension.Length == 0 ? 
                    new Disk(env.Solution.FullName) : 
                    new Disk(solutionDir.Directory.FullName);
            }
            catch
            {
                disk = null;
            }
        }

        private async Task DoUpdate()
        {
            var statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            while (true)
            {
                var options = OptionPage.Fields;
                string str=string.Empty;
                if (options.showCPU)
                    str += $"CPU: {CPU.Usage} %";
                if (options.showRam || options.showVSRam != VSRamKind.None)
                {
                    str += $"  RAM: ";

                    switch (options.showVSRam)
                    {
                        case VSRamKind.None:
                            break;
                        case VSRamKind.MainProcessOnly:
                            if (options.ramUsageUnit != SizeUnit.GB)
                                str += $"{RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                            else
                                str += $"{RAM.VsUsage(SizeUnit.GB):0.#} GB";
                            break;
                        case VSRamKind.IncludeChildProcess:
                            if (options.ramUsageUnit != SizeUnit.GB)
                                str += $"{RAM.VsUsage(options.ramUsageUnit) + RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                            else
                                str += $"{RAM.VsUsage(options.ramUsageUnit) + RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB";
                            break;
                        case VSRamKind.SeparateMainAndChild:
                            if (options.ramUsageUnit != SizeUnit.GB)
                                str += $"{RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)} ({RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)})";
                            else
                                str += $"{RAM.VsUsage(options.ramUsageUnit)} GB ({RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB)";
                            break;
                    }


                    if (options.showVSRam != VSRamKind.None && options.showRam)
                        str += " / ";

                    if (options.showRam)
                    {
                        if(options.ramTotalUnit !=SizeUnit.GB)
                            str += $"{RAM.TotalUsage(options.ramTotalUnit):0.}";
                        else
                            str += $"{RAM.TotalUsage(SizeUnit.GB):0.#}";
                        str += SizeUnitToStr(options.ramTotalUnit);
                    }
                }
                if (options.showNumProcess)
                {
                    str += $"({RAM.NumChild})";
                }

                if (options.showDisk)
                {
                    if(disk!=null)
                        str += $"  Disk: {disk.SolutionSize(SizeUnit.MB):0.#}MB";
                    else
                        await GetSolutionDir();
                }

                if (options.showBatteryPercent || options.showBatteryTime)
                {
                    str += "  Battery:";
                    if (options.showBatteryPercent)
                        str += $" {Battery.BatteryPercent * 100} %";

                    if (options.showBatteryTime)
                    {
                        var batteryRemain = Battery.BatteryRemains;
                        str += $" {batteryRemain.Item1} h {batteryRemain.Item2} min";
                    }
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
                if (!s_injector.IsInjected(s_textBlock))
                {
                    s_injector.InjectControl(s_textBlock);
                }
                s_textBlock.Text = str;

                //detect theme changes and set the text color accordingly
                var foregroundColor = s_injector.GetForegroundColor();
                if(foregroundColor is System.Windows.Media.Color c)
                {
                    s_textBlock.Foreground = new System.Windows.Media.SolidColorBrush(c);
                }
                await TaskScheduler.Default;

                await Task.Delay(Math.Max(options.refreshInterval, 1) * 1000 - 500);
            }
        }


        private async void Execute(object sender, EventArgs e)
        {
            await Task.Run(DoUpdate);
        }
    }
}

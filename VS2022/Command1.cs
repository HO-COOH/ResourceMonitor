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
                if (options.showRam || options.showVSRam)
                {
                    str += $"  RAM: ";
                    if (options.showVSRam)
                    {
                        if(options.ramUsageUnit != SizeUnit.GB)
                            str += $"{RAM.VsUsage(options.ramUsageUnit):0.}";
                        else
                            str += $"{RAM.VsUsage(SizeUnit.GB):0.#}";
                        str += SizeUnitToStr(options.ramUsageUnit);
                    }

                    if (options.showVSRam && options.showRam)
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

                statusBar.GetText(out string existingText);
                var index = existingText.IndexOf('|');
                if(index >= 0)
                    existingText = existingText.Substring(0, index - 1);
                statusBar.FreezeOutput(0);
                statusBar?.SetText(existingText + " |  " + str);

                statusBar.FreezeOutput(1);
                await Task.Delay(options.refreshInterval * 1000);

            }
        }


        private async void Execute(object sender, EventArgs e)
        {
            await Task.Run(DoUpdate);
        }
    }
}

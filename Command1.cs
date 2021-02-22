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

namespace ResourceMonitor
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1: AsyncPackage
    {
        private static Disk disk;

        /*Options*/
        private static int refreshInterval = 1;

        private bool showCPU;

        private bool showRam;
        private static SizeUnit ramUsageUnit;
        private static SizeUnit ramTotalUnit;
        private bool showVSRam;

        private bool showDisk;

        private bool showBatteryPercent;
        private bool showBatteryTime;

        private void UpdateSettings()
        {
            var options = (OptionPage)GetDialogPage(typeof(OptionPage));

            showCPU = options.ShowCPU;

            showRam = options.ShowRAM;
            ramUsageUnit = options.RamUsageUnit;
            ramTotalUnit = options.TotalRamUnit;
            showVSRam = options.ShowVSRAM;

            showDisk = options.ShowDisk;
            
            showBatteryPercent = options.ShowBatteryPercent;
            showBatteryTime = options.ShowBatteryTime;
        }

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

        private async void GetSolutionDir()
        {
            try
            {
                var env = await ServiceProvider.GetServiceAsync(typeof(SDTE)) as DTE;
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
                UpdateSettings();
                string str=string.Empty;
                if (showCPU)
                    str += $"CPU: {CPU.Usage} %";
                if (showRam || showVSRam)
                {
                    str += $"  RAM: ";
                    if (showVSRam)
                    {
                        if(ramUsageUnit != SizeUnit.GB)
                            str += $"{RAM.VsUsage(ramUsageUnit):0.}";
                        else
                            str += $"{RAM.VsUsage(SizeUnit.GB):0.#}";
                        str += SizeUnitToStr(ramUsageUnit);
                    }

                    if (showVSRam && showRam)
                        str += " / ";

                    if (showRam)
                    {
                        if(ramTotalUnit!=SizeUnit.GB)
                            str += $"{RAM.TotalUsage(ramTotalUnit):0.}";
                        else
                            str += $"{RAM.TotalUsage(SizeUnit.GB):0.#}";
                        str += SizeUnitToStr(ramTotalUnit);
                    }
                }

                if (showDisk)
                {
                    if(disk!=null)
                        str += $"  Disk: {disk.SolutionSize(SizeUnit.MB):0.#}MB";
                    else
                        GetSolutionDir();
                }

                if (showBatteryPercent || showBatteryTime)
                {
                    str += "  Battery:";
                    if (showBatteryPercent)
                        str += $" {Battery.BatteryPercent * 100} %";

                    if (showBatteryTime)
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
                System.Threading.Thread.Sleep(refreshInterval * 1000);

            }
        }


        private async void Execute(object sender, EventArgs e)
        {
            await Task.Run(DoUpdate);
        }
    }
}

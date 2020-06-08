using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using Microsoft.VisualStudio.Debugger.Interop;

namespace ResourceMonitor
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1: AsyncPackage
    {
        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", Environment.MachineName);
        static PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
        static int memtotal = 0;
        public static int GetCpuUsage()
        {
            return (int)cpuCounter.NextValue();
        }
        public static int GetRamUsage()
        {
            //System.Threading.Thread.Sleep(500);
            return (memtotal - (int)ramCounter.NextValue());
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

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        
        private async void DoUpdate()
        {
            while (true)
            {
                var statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                int frozen;

                statusBar.IsFrozen(out frozen);
                if (frozen != 0)
                    statusBar.FreezeOutput(0);
                var mem = GetRamUsage();
                statusBar.SetText($"CPU: {GetCpuUsage()} %  RAM: {mem} MB / {(float)memtotal/1024.0:0.#} GB ({(float)mem/memtotal *100 :##} %)");
                System.Threading.Thread.Sleep(1000);
            }
        }
        private async void Execute(object sender, EventArgs e)
        {
            var info = new Microsoft.VisualBasic.Devices.ComputerInfo();
            memtotal = (int)(info.TotalPhysicalMemory / 1024 / 1024);

            Thread update = new Thread(DoUpdate);
            update.Start();
        }
    }
}

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
using VS2022Support;

namespace ResourceMonitor
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1: AsyncPackage
    {
        public static Disk Disk;

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
        //public static ToolWindowPane s_cpuToolWindow;

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

        //static System.Windows.Controls.TextBlock s_textBlock;
        static VS2022Support.ResourceMonitorStatusBar s_textBlock;
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

            s_textBlock = new VS2022Support.ResourceMonitorStatusBar
            {
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            s_injector = new VS2022.StatusBarInjector(System.Windows.Application.Current.MainWindow);
            s_injector.InjectControl(s_textBlock);
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
                Disk = solutionDir.Extension.Length == 0 ? 
                    new Disk(env.Solution.FullName) : 
                    new Disk(solutionDir.Directory.FullName);
            }
            catch
            {
                Disk = null;
            }
        }

        static bool s_injected = false;
        private async Task DoUpdate()
        {
            var statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            while (true)
            {
                ChildProcess.Update();
                if (Disk == null)
                    await GetSolutionDir();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
                if (!s_injected)
                {
                    s_injector.InjectControl(s_textBlock);
                    s_injected = s_injector.IsInjected(s_textBlock);
                }
                s_textBlock.Update();
                if(CPUToolWindow.Instance != null)
                    CPUToolWindow.Instance.Update();
                if (RAMToolWindow.Instance != null)
                    RAMToolWindow.Instance.Update();

                //await TaskScheduler.Default;
                await Task.Delay(Math.Max(OptionPage.Fields.refreshInterval, 1) * 1000);
                System.Diagnostics.Debug.WriteLine("Update");
            }
        }


        private async void Execute(object sender, EventArgs e)
        {
            await Task.Run(DoUpdate);
        }
    }
}

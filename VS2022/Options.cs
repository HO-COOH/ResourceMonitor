using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ResourceMonitor
{

    public class OptionPage:DialogPage
    {
        [Category("Refresh")]
        [DisplayName("Refresh interval (seconds)")]
        [Description("Refresh interval (seconds)")]
        public int RefreshInterval { get; set; } = 1;

        [Category("CPU")]
        [DisplayName("Show CPU Usage")]
        [Description("Show CPU Usage")]
        public bool ShowCPU { get; set; } = true;


        [Category("RAM")]
        [DisplayName("Show Total system RAM Usage")]
        [Description("Show Total system RAM Usage")]
        public bool ShowRAM { get; set; } = true;

        [Category("RAM")]
        [DisplayName("Show RAM Usage of Visual Studio")]
        [Description("Show RAM Usage of Visual Studio")]
        public bool ShowVSRAM { get; set; } = true;

        [Category("RAM")]
        [DisplayName("Unit for Visual Studio RAM usage")]
        [Description("Unit for Visual Studio RAM usage")]
        public SizeUnit RamUsageUnit { get; set; } = SizeUnit.MB;

        [Category("RAM")]
        [DisplayName("Unit for total system RAM usage")]
        [Description("Unit for total system RAM usage")]
        public SizeUnit TotalRamUnit { get; set; } = SizeUnit.GB;

        [Category("Disk")]
        [DisplayName("Show Solution Disk Usage")]
        [Description("Show Solution Disk Usage")]
        public bool ShowDisk { get; set; } = true;

        [Category("Battery")]
        [DisplayName("Show Battery Percentage")]
        [Description("Show Battery Percentage")]
        public bool ShowBatteryPercent { get; set; } = true;

        [Category("Battery")]
        [DisplayName("Show Battery Remaining Time")]
        [Description("Show Battery Remaining Time")]
        public bool ShowBatteryTime { get; set; } = true;
    }

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Options.PackageGuidString)]
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules", 
        "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")
    ]
    [ProvideOptionPage(
        typeof(OptionPage), 
        "Resource Monitor", 
        "General", 
        0, 
        0, 
        true)
    ]
    public sealed class Options : AsyncPackage
    {
        /// <summary>
        /// VSPackage1 GUID string.
        /// </summary>
        public const string PackageGuidString = "6329d6c8-99a3-4b45-9486-7dd686503757";

        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        public Options()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion
    }
}

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace ResourceMonitor
{
    public enum VSRamKind
    {
        MainProcessOnly,
        IncludeChildProcess,
        SeparateMainAndChild,
        None
    }
    [Serializable]
    public class Fields
    {
        public int refreshInterval { get; set; } = 1;
        public bool showCPU { get; set; } = true;
        public bool showRam { get; set; } = true;
        public SizeUnit ramUsageUnit { get; set; } = SizeUnit.MB;
        public SizeUnit ramTotalUnit { get; set; } = SizeUnit.GB;
        public VSRamKind showVSRam { get; set; } = VSRamKind.SeparateMainAndChild;
        public bool showDisk { get; set; } = true;
        public bool showBatteryPercent { get; set; } = true;
        public bool showBatteryTime { get; set; } = true;
    }

    public class OptionPage:DialogPage
    {
        [Category("Refresh")]
        [DisplayName("Refresh interval (seconds)")]
        [Description("Refresh interval (seconds)")]
        public int RefreshInterval
        {
            get => Fields.refreshInterval;
            set
            {
                Fields.refreshInterval = value;
                save();
            }
        }

        [Category("CPU")]
        [DisplayName("Show CPU Usage")]
        [Description("Show CPU Usage")]
        public bool ShowCPU
        {
            get => Fields.showCPU;
            set
            {
                Fields.showCPU = value;
                save();
            }
        }


        [Category("RAM")]
        [DisplayName("Show Total system RAM Usage")]
        [Description("Show Total system RAM Usage")]
        public bool ShowRAM
        {
            get => Fields.showRam;
            set
            {
                Fields.showRam = value;
                save() ;
            }
        }

        [Category("RAM")]
        [DisplayName("How RAM usage of Visual Studio are reported")]
        [Description("How RAM usage of Visual Studio are reported")]
        public VSRamKind ShowVSRAM
        {
            get => Fields.showVSRam;
            set
            {
                Fields.showVSRam = value;
                save();
            }
        }

        [Category("RAM")]
        [DisplayName("Unit for Visual Studio RAM usage")]
        [Description("Unit for Visual Studio RAM usage")]
        public SizeUnit RamUsageUnit
        {
            get => Fields.ramUsageUnit;
            set
            {
                Fields.ramUsageUnit = value;
                save();
            }
        }

        [Category("RAM")]
        [DisplayName("Unit for total system RAM usage")]
        [Description("Unit for total system RAM usage")]
        public SizeUnit TotalRamUnit
        {
            get => Fields.ramTotalUnit;
            set
            {
                Fields.ramTotalUnit = value;
                save();
            }
        }

        [Category("Disk")]
        [DisplayName("Show Solution Disk Usage")]
        [Description("Show Solution Disk Usage")]
        public bool ShowDisk
        {
            get => Fields.showDisk;
            set
            {
                Fields.showDisk = value;
                save();
            }
        }

        [Category("Battery")]
        [DisplayName("Show Battery Percentage")]
        [Description("Show Battery Percentage")]
        public bool ShowBatteryPercent
        {
            get => Fields.showBatteryPercent;
            set
            {
                Fields.showBatteryPercent = value;
                save();
            }
        }

        [Category("Battery")]
        [DisplayName("Show Battery Remaining Time")]
        [Description("Show Battery Remaining Time")]
        public bool ShowBatteryTime
        {
            get => Fields.showBatteryTime;
            set
            {
                Fields.showBatteryTime = value;
                save();
            }
        }

        const string CollectionName = "ResourceMonitor.OptionPage";

        static public Fields Fields;
        
        static OptionPage()
        {
            var store = Command1.ShellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            try
            {
                if (store.CollectionExists(CollectionName))
                {
                    using (var reader = new JsonTextReader(new StringReader(store.GetString(CollectionName, "Value"))))
                    {
                        Fields = new JsonSerializer().Deserialize<Fields>(reader);
                        return;
                    }
                }
            }
            catch (Exception)
            {
            }
            Fields = new Fields();
        }

        async void save()
        {
            var store = Command1.ShellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            store.CreateCollection("ResourceMonitor.OptionPage");
            using (var sw = new StringWriter())
            {
                using (var writer = new JsonTextWriter(sw))
                {
                    new JsonSerializer().Serialize(writer, Fields);
                    store.SetString("ResourceMonitor.OptionPage", "Value", sw.ToString());
                }
            }
        }
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

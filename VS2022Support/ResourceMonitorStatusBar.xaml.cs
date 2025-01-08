using Microsoft.VisualStudio.Shell.Interop;
using ResourceMonitor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VS2022;

namespace VS2022Support
{
    public class OverviewDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string CPU
        {
            get
            {
                return $"{ResourceMonitor.CPU.Usage} %";
            }
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

        public string RAM
        {
            get
            {
                var options = OptionPage.Fields;
                switch(options.showVSRam)
                {
                    case VSRamKind.MainProcessOnly:
                        if (options.ramUsageUnit != SizeUnit.GB)
                            return $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                        else
                            return $"{ResourceMonitor.RAM.VsUsage(SizeUnit.GB):0.#} GB";
                    case VSRamKind.IncludeChildProcess:
                        if (options.ramUsageUnit != SizeUnit.GB)
                            return $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit) + ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                        else
                            return $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit) + ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB";
                    case VSRamKind.SeparateMainAndChild:
                        if (options.ramUsageUnit != SizeUnit.GB)
                            return $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)} ({ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}, {ResourceMonitor.RAM.NumChild} Child)";
                        else
                            return $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit)} GB ({ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB, {ResourceMonitor.RAM.NumChild} Child)";
                    default:
                        return "";
                }
            }
        }

        public Visibility RamSeperatorVisibility
        {
            get
            {
                var options = OptionPage.Fields;
                return options.showRam && options.showVSRam != VSRamKind.None ?
                    Visibility.Visible :
                    Visibility.Collapsed;
            }
        }

        public string TotalRAM
        {
            get
            {
                if (OptionPage.Fields.showRam)
                {
                    if (OptionPage.Fields.ramTotalUnit != SizeUnit.GB)
                        return $"{ResourceMonitor.RAM.TotalUsage(OptionPage.Fields.ramTotalUnit):0.} {SizeUnitToStr(OptionPage.Fields.ramTotalUnit)}";
                    else
                        return $"{ResourceMonitor.RAM.TotalUsage(SizeUnit.GB):0.#} GB";
                }
                return "";
            }
        }

        public string BatteryPercent
        {
            get
            {
                if (OptionPage.Fields.showBatteryPercent)
                    return $" {ResourceMonitor.Battery.BatteryPercent * 100} %";

                return "";
            }
        }

        public string BatteryTime
        {
            get
            {
                if (OptionPage.Fields.showBatteryTime)
                {
                    var batteryRemain = Battery.BatteryRemains;
                    return $" {batteryRemain.Item1} h {batteryRemain.Item2} min";
                }
                return "";
            }
        }

        public string Disk
        {
            get
            {
                if(OptionPage.Fields.showDisk && Command1.Disk != null)
                    return $"{Command1.Disk.SolutionSize(SizeUnit.MB):0.#} MB";

                return "";
            }
        }

        public Visibility DiskVisibility
        {
            get
            {
                return Disk != "" ? 
                    Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void Update()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPU"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RAM"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalRAM"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Disk"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryPercent"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryTime"));
        }

        public void SettingsChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RamSeperatorVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DiskVisibility"));
        }
    }

    public partial class ResourceMonitorStatusBar : UserControl, INotifyPropertyChanged
    {
        public OverviewDataModel DataModel { get; set; } = new OverviewDataModel();
        public ResourceMonitorStatusBar()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CPUButton_Click(object sender, RoutedEventArgs e)
        {
            CPUPopup.IsOpen = true;
        }

        private void RAMButton_Click(object sender, RoutedEventArgs e)
        {
            RAMPopup.IsOpen = true;
        }

        public void Update()
        {
            DataModel.Update();
            if (CPUPopup.IsOpen)
            {
                CPUPanel.Update();
            }
            if(RAMPopup.IsOpen)
            {
                RAMPanel.Update();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("taskmgr.exe");
        }

        private void CPUPopup_Opened(object sender, EventArgs e)
        {
            CPUPanel.Update();
        }

        private void RAMPopup_Opened(object sender, EventArgs e)
        {
            RAMPanel.Update();
        }
    }
}

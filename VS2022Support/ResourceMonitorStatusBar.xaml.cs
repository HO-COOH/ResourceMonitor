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

        private string m_cpu;
        private void updateCPU()
        {
            m_cpu = $"{ResourceMonitor.CPU.Usage,3} %";
        }
        public string CPU => m_cpu;

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

        private string m_ram;
        private void updateRAM()
        {
            var options = OptionPage.Fields;
            switch (options.showVSRam)
            {
                case VSRamKind.MainProcessOnly:
                    if (options.ramUsageUnit != SizeUnit.GB)
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                    else
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(SizeUnit.GB):0.#} GB";
                    break;
                case VSRamKind.IncludeChildProcess:
                    if (options.ramUsageUnit != SizeUnit.GB)
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit) + ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}";
                    else
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit) + ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB";
                    break;
                case VSRamKind.SeparateMainAndChild:
                    if (options.ramUsageUnit != SizeUnit.GB)
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)} ({ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.} {SizeUnitToStr(options.ramUsageUnit)}, {ResourceMonitor.RAM.NumChild} Child)";
                    else
                        m_ram = $"{ResourceMonitor.RAM.VsUsage(options.ramUsageUnit)} GB ({ResourceMonitor.RAM.ChildProcessUsage(options.ramUsageUnit):0.#} GB, {ResourceMonitor.RAM.NumChild} Child)";
                    break;
                default:
                    return;
            }
        }
        public string RAM => m_ram;

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

        private string m_totalRAM;
        private void updateTotalRAM()
        {
            if (OptionPage.Fields.showRam)
            {
                if (OptionPage.Fields.ramTotalUnit != SizeUnit.GB)
                    m_totalRAM = $"{ResourceMonitor.RAM.TotalUsage(OptionPage.Fields.ramTotalUnit):0.} {SizeUnitToStr(OptionPage.Fields.ramTotalUnit)}";
                else
                    m_totalRAM = $"{ResourceMonitor.RAM.TotalUsage(SizeUnit.GB):0.#} GB";
            }
            else
                m_totalRAM = "";
        }
        public string TotalRAM => m_totalRAM;

        private string m_batteryPercent;
        private void updateBatteryPercent()
        {
            if (OptionPage.Fields.showBatteryPercent)
                m_batteryPercent = $" {ResourceMonitor.Battery.BatteryPercent * 100} %";
            else
                m_batteryPercent = "";
        }
        public string BatteryPercent => m_batteryPercent;

        private string m_batteryTime;
        private void updateBatteryTime()
        {
            if (OptionPage.Fields.showBatteryTime)
            {
                var batteryRemain = Battery.BatteryRemains;
                m_batteryTime = $" {batteryRemain.Item1} h {batteryRemain.Item2} min";
            }
            m_batteryTime = "";
        }
        public string BatteryTime => m_batteryTime;

        private string m_disk;
        private void updateDisk()
        {
            if (OptionPage.Fields.showDisk && Command1.Disk != null)
                m_disk = $"{Command1.Disk.SolutionSize(SizeUnit.MB):0.#} MB";
            else
                m_disk = "";
        }
        public string Disk => m_disk;


        public Visibility CPUVisibility => OptionPage.Fields.showCPU ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RAMVisibility => OptionPage.Fields.showRam || OptionPage.Fields.showVSRam != VSRamKind.None ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DiskVisibility => OptionPage.Fields.showDisk && Disk != "" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BatteryVisibility => OptionPage.Fields.showBatteryTime || OptionPage.Fields.showBatteryPercent ? Visibility.Visible : Visibility.Hidden;
        public Visibility BatteryPercentVisibility => OptionPage.Fields.showBatteryPercent ? Visibility.Visible : Visibility.Hidden;
        public Visibility BatteryTimeVisibility => OptionPage.Fields.showBatteryTime ? Visibility.Visible : Visibility.Hidden;
        public Visibility LabelTextVisibility => OptionPage.Fields.labelKind == LabelKind.Text ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LabelIconVisibility => OptionPage.Fields.labelKind == LabelKind.Icon? Visibility.Visible : Visibility.Collapsed;

        public void Update()
        {
            updateCPU();
            updateRAM();
            updateTotalRAM();
            updateDisk();
            updateBatteryPercent();
            updateBatteryTime();
        }

        public void RaiseDataChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPU"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RAM"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalRAM"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Disk"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DiskVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryPercent"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryTime"));
        }

        public void SettingsChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPUVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RAMVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RamSeperatorVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DiskVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryPercentVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryTimeVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LabelTextVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LabelIconVisibility"));
        }

        public OverviewDataModel()
        {
            OptionPage.OptionsChanged += (_, __) => { SettingsChanged(); };
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

        bool m_cpuPanelOpened = false;
        bool m_ramPanelOpened = false;
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
            if (m_cpuPanelOpened)
                CPUPanel.Update();
            if (m_ramPanelOpened)
                RAMPanel.Update();
        }

        public void RaiseDataChange()
        {
            DataModel.RaiseDataChanged();
            if (CPUPopup.IsOpen)
            {
                CPUPanel.RaiseDataChange();
            }
            if (RAMPopup.IsOpen)
            {
                RAMPanel.RaiseDataChange();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("taskmgr.exe");
        }

        private void CPUPopup_Opened(object sender, EventArgs e)
        {
            CPUPanel.Update();
            m_cpuPanelOpened = true;
        }

        private void RAMPopup_Opened(object sender, EventArgs e)
        {
            RAMPanel.Update();
            m_ramPanelOpened = true;
        }

        private void CPUPopup_Closed(object sender, EventArgs e)
        {
            m_cpuPanelOpened = false;
        }

        private void RAMPopup_Closed(object sender, EventArgs e)
        {
            m_ramPanelOpened = false;
        }
    }
}

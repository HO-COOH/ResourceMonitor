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
using System.Diagnostics;
using VS2022;
using Win32;
using System.Collections.ObjectModel;

namespace VS2022Support
{
    public class ProcessCPUUsageModel : IEquatable<ProcessCPUUsageModel>, INotifyPropertyChanged
    {
        public uint Pid { get; private set; }
        public string Name
        {
            get => m_process.ProcessName;
        }
        DateTime lastTime;
        TimeSpan lastTotalProcessorTime;
        DateTime curTime;
        TimeSpan curTotalProcessorTime;
        private float Percent;

        public void Update()
        {
            if (lastTime == null || lastTime == new DateTime())
            {
                lastTime = DateTime.Now;
                lastTotalProcessorTime = m_process.TotalProcessorTime;
                Percent = 0;
                return;
            }

            
            curTime = DateTime.Now;
            curTotalProcessorTime = m_process.TotalProcessorTime;

            double CPUUsage = (curTotalProcessorTime.TotalMilliseconds - lastTotalProcessorTime.TotalMilliseconds) / curTime.Subtract(lastTime).TotalMilliseconds / Convert.ToDouble(Environment.ProcessorCount);

            lastTime = curTime;
            lastTotalProcessorTime = curTotalProcessorTime;
            Percent = (float)CPUUsage;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ColoredWidth"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UncoloredWidth"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PercentText"));
        }

        public string PercentText
        {
            get => string.Format("{0:0.0%}", Percent);
        }

        public GridLength ColoredWidth
        {
            get
            {
                return new GridLength(Percent, GridUnitType.Star);
            }
        }

        public GridLength UncoloredWidth
        {
            get
            {
                return new GridLength(1.0f - Percent, GridUnitType.Star);
            }
        }

        private Process m_process;
        private int id;

        public event PropertyChangedEventHandler PropertyChanged;

        public ProcessCPUUsageModel(uint pid)
        {
            m_process = Process.GetProcessById((int)pid);
            Pid = pid;
        }

        public bool Equals(ProcessCPUUsageModel other)
        {
            return other.Pid == Pid;
        }
    }
    /// <summary>
    /// Interaction logic for CPUPanel.xaml
    /// </summary>
    public partial class CPUPanel : UserControl, INotifyPropertyChanged
    {
        static uint currentPid = (uint)Process.GetCurrentProcess().Id;
        public CPUPanel()
        {
            InitializeComponent();
            ProcessItems.Add(new ProcessCPUUsageModel((uint)Process.GetCurrentProcess().Id));
        }

        public ObservableCollection<ProcessCPUUsageModel> ProcessItems { get; set; } = new ObservableCollection<ProcessCPUUsageModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Update()
        {
            foreach(var item in ChildProcess.AllChildProcess)
            {
                if (!ProcessItems.Contains(new ProcessCPUUsageModel(item)))
                    ProcessItems.Add(new ProcessCPUUsageModel(item));
            }

            foreach (var item in ProcessItems)
            {
                if (!ChildProcess.AllChildProcess.Contains(item.Pid) && item.Pid != currentPid)
                    ProcessItems.Remove(item);
                else
                    item.Update();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProcessItems"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

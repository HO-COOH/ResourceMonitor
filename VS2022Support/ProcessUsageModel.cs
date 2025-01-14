using System;
using System.ComponentModel;
using System.Windows;
using System.Diagnostics;
using System.Management;
using System.Linq;

namespace VS2022Support
{
    public abstract class ProcessUsageBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract void Update();

        public int Pid { get; private set; }

        private Process m_process;
        public Process Process
        {
            get
            {
                if (m_process == null)
                {
                    m_process = Process.GetProcessById((int)Pid);
                }
                return m_process;
            }
        }

        public string Name => Process.ProcessName;

        public string CommandLine
        {
            get
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + Pid))
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                }
            }
        }
        public ProcessUsageBase(uint pid)
        {
            Pid = (int)pid;
        }

        protected void raisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class ProcessCPUUsageModel : ProcessUsageBase, IEquatable<ProcessCPUUsageModel>
    {

        DateTime lastTime;
        TimeSpan lastTotalProcessorTime;
        DateTime curTime;
        TimeSpan curTotalProcessorTime;
        private float Percent;

        override public void Update() 
        {
            try
            {
                if (lastTime == null || lastTime == new DateTime())
                {
                    lastTime = DateTime.Now;
                    lastTotalProcessorTime = Process.TotalProcessorTime;
                    Percent = 0;
                    return;
                }


                curTime = DateTime.Now;
                curTotalProcessorTime = Process.TotalProcessorTime;

                double CPUUsage = (curTotalProcessorTime.TotalMilliseconds - lastTotalProcessorTime.TotalMilliseconds) / curTime.Subtract(lastTime).TotalMilliseconds / Convert.ToDouble(Environment.ProcessorCount);

                lastTime = curTime;
                lastTotalProcessorTime = curTotalProcessorTime;
                Percent = (float)CPUUsage;

                raisePropertyChanged("ColoredWidth");
                raisePropertyChanged("UncoloredWidth");
                raisePropertyChanged("PercentText");
            }
            catch
            { }
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

        public ProcessCPUUsageModel(uint pid) : base(pid) { }

        public bool Equals(ProcessCPUUsageModel other)
        {
            return other.Pid == Pid;
        }
    }

    public class ProcessRAMUsageModel : ProcessUsageBase, IEquatable<ProcessRAMUsageModel>
    {
        public static float Clamp(float value, float min, float max)
        {
            if (min > max)
            {
                throw new ArgumentException();
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }


        public ProcessRAMUsageModel(uint pid) : base(pid)
        { 
        }

        static public long MaxProcessMemory { get; set; }

        private float Percent => Clamp(MaxProcessMemory == 0? 0:(float)Process.PrivateMemorySize64 / MaxProcessMemory, 0.01f, 0.99f);
        public GridLength ColoredWidth => new GridLength(Percent, GridUnitType.Star);
        public GridLength UncoloredWidth => new GridLength(1.0 - Percent, GridUnitType.Star);

        public string UsageText => $"{ResourceMonitor.RAM.ConvertUnit(Process.PrivateMemorySize64, ResourceMonitor.SizeUnit.MB):0.} MB";

        public override void Update()
        {
            Process.Refresh();
            raisePropertyChanged("ColoredWidth");
            raisePropertyChanged("UncoloredWidth");
            raisePropertyChanged("UsageText");
        }

        public bool Equals(ProcessRAMUsageModel other)
        {
            return other.Pid == Pid;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using System.Management;
using System.Linq;

namespace ResourceMonitor
{
    static class RAM
    {
        public static ulong total = new ComputerInfo().TotalPhysicalMemory; //unit: bytes
        
        private static readonly PerformanceCounter totalCounter = new PerformanceCounter(
            "Memory", 
            "Available Bytes", 
            true
        );

        //private static readonly PerformanceCounter vsCounter = new PerformanceCounter(
        //    "Process", 
        //    "Working Set", 
        //    "devenv", 
        //    true
        //); //This would throws InvalidArugumentException with VS2022 on Windows 11

        private static float ToKB(float value) => value / 1024.0f;
        private static float ToMB(float value) => value / 1024.0f / 1024.0f;
        private static float ToGB(float value) => value / 1024.0f / 1024.0f / 1024.0f;

        private static float ConvertUnit(float value, SizeUnit unit)
        {
            switch (unit)
            {
                case SizeUnit.KB:
                    return ToKB(value);
                case SizeUnit.MB:
                    return ToMB(value);
                case SizeUnit.GB:
                    return ToGB(value);
            }
            return 0.0f;
        }
        public static float TotalUsage(SizeUnit unit = SizeUnit.MB) => ConvertUnit((float)total - totalCounter.NextValue(), unit);
        public static float VsUsage(SizeUnit unit = SizeUnit.MB) => ConvertUnit(Process.GetCurrentProcess().PrivateMemorySize64, unit);

        public static int NumChild = 0;

        private static IEnumerable<Process> getChildProcesses(Process process)
        {
            List<Process> children = new List<Process>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));

            foreach (ManagementObject mo in mos.Get())
            {
                try
                {
                    children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
                    children.AddRange(getChildProcesses(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]))));
                }
                catch { }
            }

            return children;
        }
        public static float ChildProcessUsage(SizeUnit unit = SizeUnit.GB)
        {
            float sum = 0;
            int count = 0;
            foreach (var process in getChildProcesses(Process.GetCurrentProcess()))
            {
                sum += process.PrivateMemorySize64;
                ++count;
            }
            NumChild = count + 1;
            return ConvertUnit(sum, unit);
        }
    }
}

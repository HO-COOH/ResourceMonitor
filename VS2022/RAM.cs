using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

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

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using System.Management;
using System.Linq;
using EnvDTE;
using System.Security.Cryptography;

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
        public static float VsUsage(SizeUnit unit = SizeUnit.MB) => ConvertUnit(System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64, unit);

        public static int NumChild = 0;

        static private List<Win32.PROCESSENTRY32> getAllProcesses()
        {
            var handle = Win32.Kernel32.CreateToolhelp32Snapshot((uint)Win32.SnapshotFlags.Process, 0);
            var processes = new List<Win32.PROCESSENTRY32>();
            try
            {
                Win32.PROCESSENTRY32 entry = new Win32.PROCESSENTRY32()
                {
                    dwSize = (UInt32)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.PROCESSENTRY32))
                };
                if (Win32.Kernel32.Process32First(handle, ref entry))
                {
                    do
                    {
                        processes.Add(entry);
                    } while (Win32.Kernel32.Process32Next(handle, ref entry));
                }
            }
            catch
            {
            }
            Win32.Kernel32.CloseHandle(handle);
            return processes;
        }

        private static float getSumOfChildProcess(uint pid, List<Win32.PROCESSENTRY32> processes)
        {
            float sum = 0;
            foreach (var process in processes)
            {

                if(process.th32ParentProcessID == pid)
                {
                    sum += getSumOfChildProcess(process.th32ProcessID, processes);
                    ++NumChild;
                }
            }
            try
            {
                sum += System.Diagnostics.Process.GetProcessById((int)pid).PrivateMemorySize64;
            }
            catch { }
            return sum;
        }

        public static float ChildProcessUsage(SizeUnit unit = SizeUnit.GB)
        {
            var allProcess = getAllProcesses();
            float sum = 0;
            NumChild = 0;
            var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            foreach(var process in allProcess)
            {
                if (process.th32ParentProcessID == currentPid)
                {
                    sum += getSumOfChildProcess(process.th32ProcessID, allProcess);
                    ++NumChild;
                }
            }

            return ConvertUnit(sum, unit);
        }
    }
}

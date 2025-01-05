using System;
using System.Collections.Generic;
using System.Text;
using Win32;

namespace VS2022
{
    static class ChildProcess
    {
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

        private static void getChildProcessRecursive(uint pid, List<Win32.PROCESSENTRY32> allProcesses)
        {
            foreach (var process in allProcesses)
            {
                if (process.th32ParentProcessID == pid)
                {
                    s_allChildProcesses.Add(process.th32ProcessID);
                    getChildProcessRecursive(process.th32ProcessID, allProcesses);
                }
            }
        }

        private static HashSet<uint> s_allChildProcesses = new HashSet<uint>();

        public static HashSet<uint> AllChildProcess => s_allChildProcesses;

        public static void Update()
        {
            List<PROCESSENTRY32> childProcesses = new List<PROCESSENTRY32>();
            var allProcesses = getAllProcesses();
            getChildProcessRecursive((uint)System.Diagnostics.Process.GetCurrentProcess().Id, allProcesses);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using VS2022Support;
using Win32;

namespace VS2022
{
    static class ChildProcess
    {


        /**
         *  Build up a tree 
         *      [0] -> [1]
         *      [1] -> [2,3,4]
         *      [2] -> [5,6]
         *      
         */
        //static private List<Win32.PROCESSENTRY32> getAllProcesses()
        //{
        //    var handle = Win32.Kernel32.CreateToolhelp32Snapshot((uint)Win32.SnapshotFlags.Process, 0);
        //    var processes = new List<Win32.PROCESSENTRY32>();
        //    try
        //    {
        //        Win32.PROCESSENTRY32 entry = new Win32.PROCESSENTRY32()
        //        {
        //            dwSize = (UInt32)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.PROCESSENTRY32))
        //        };
        //        if (Win32.Kernel32.Process32First(handle, ref entry))
        //        {
        //            do
        //            {
        //                processes.Add(entry);
        //            } while (Win32.Kernel32.Process32Next(handle, ref entry));
        //        }
        //    }
        //    catch
        //    {
        //    }
        //    Win32.Kernel32.CloseHandle(handle);
        //    return processes;
        //}

        static private Dictionary<uint, List<uint>> getAllProcesses()
        {
            var handle = Win32.Kernel32.CreateToolhelp32Snapshot((uint)Win32.SnapshotFlags.Process, 0);
            Dictionary<uint, List<uint>> processes = new Dictionary<uint, List<uint>>();
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
                        if (processes.ContainsKey(entry.th32ParentProcessID))
                        {
                            processes[entry.th32ParentProcessID].Add(entry.th32ProcessID);
                        }
                        else
                        {
                            processes[entry.th32ParentProcessID] = new List<uint>() { entry.th32ProcessID };
                        }
                    } while (Win32.Kernel32.Process32Next(handle, ref entry));
                }

            }
            catch { }
            return processes;
        }

        //private static void getChildProcessRecursive(uint pid, List<Win32.PROCESSENTRY32> allProcesses)
        //{
        //    foreach (var process in allProcesses)
        //    {
        //        if (process.th32ParentProcessID == pid)
        //        {
        //            s_allChildProcesses.Add(process.th32ProcessID);
        //            getChildProcessRecursive(process.th32ProcessID, allProcesses);
        //        }
        //    }
        //}

        private static void getChildProcessRecursive(uint pid, Dictionary<uint, List<uint>> allProcesses)
        {
            if (!allProcesses.ContainsKey(pid))
                return;

            foreach (var process in allProcesses[pid])
            {
                s_allChildProcesses.Add(process);
                getChildProcessRecursive(process, allProcesses);
            }
        }
        static uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        private static HashSet<uint> s_allChildProcesses = new HashSet<uint>();

        public static HashSet<uint> AllChildProcess => s_allChildProcesses;
        public static void Update()
        {
            List<PROCESSENTRY32> childProcesses = new List<PROCESSENTRY32>();
            var allProcesses = getAllProcesses();
            s_allChildProcesses.Clear();
            getChildProcessRecursive((uint)System.Diagnostics.Process.GetCurrentProcess().Id, allProcesses);
        }

        public static void SyncWithObservableCollection(ObservableCollection<ProcessCPUUsageModel> collection)
        {
            foreach (var item in ChildProcess.AllChildProcess)
            {
                if (!collection.Contains(new ProcessCPUUsageModel(item)))
                    collection.Add(new ProcessCPUUsageModel(item));
            }

            List<ProcessCPUUsageModel> itemsToRemove = new List<ProcessCPUUsageModel>();
            foreach (var item in collection)
            {
                if (!ChildProcess.AllChildProcess.Contains((uint)item.Pid) && item.Pid != currentPid)
                    itemsToRemove.Add(item);
                else
                    item.Update();
            }
            foreach (var item in itemsToRemove)
                collection.Remove(item);
        }

        public static void SyncWithObservableCollection(ObservableCollection<ProcessRAMUsageModel> collection)
        {
            foreach (var item in ChildProcess.AllChildProcess)
            {
                if (!collection.Contains(new ProcessRAMUsageModel(item)))
                    collection.Add(new ProcessRAMUsageModel(item));
            }

            List<ProcessRAMUsageModel> itemsToRemove = new List<ProcessRAMUsageModel>();
            ProcessRAMUsageModel.MaxProcessMemory = 0;
            foreach (var item in collection)
            {
                if (!ChildProcess.AllChildProcess.Contains((uint)item.Pid) && item.Pid != currentPid)
                    itemsToRemove.Add(item);
                else
                    ProcessRAMUsageModel.MaxProcessMemory = Math.Max(item.Process.PrivateMemorySize64, ProcessRAMUsageModel.MaxProcessMemory);
            }
            foreach (var item in itemsToRemove)
                collection.Remove(item);
            foreach (var item in collection)
                item.Update();
        }
    }
}

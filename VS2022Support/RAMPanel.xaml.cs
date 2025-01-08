﻿using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// Interaction logic for RAMPanel.xaml
    /// </summary>
    public partial class RAMPanel : UserControl, INotifyPropertyChanged
    {
        public RAMPanel()
        {
            InitializeComponent();
            ProcessItems.Add(new ProcessRAMUsageModel((uint)System.Diagnostics.Process.GetCurrentProcess().Id));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public static event PropertyChangedEventHandler StaticPropertyChanged;
        private static bool m_isPinned = false;
        public ObservableCollection<ProcessRAMUsageModel> ProcessItems { get; set; } = new ObservableCollection<ProcessRAMUsageModel>();
        public static bool IsPinned
        {
            get => m_isPinned;
            set
            {
                m_isPinned = value;
                if (CPUToolWindowPackage.Instance == null)
                    return;
                var window = CPUToolWindowPackage.Instance.FindToolWindow(typeof(RAMToolWindow), 0, true);
                var frame = window.Frame as IVsWindowFrame;
                if (m_isPinned)
                {
                    frame.Show();
                }
                else
                {
                    frame.Hide();
                }
            }
        }
        public void Update()
        {
            ChildProcess.SyncWithObservableCollection(ProcessItems);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProcessItems"));
        }

        private void KillProcessButton_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}

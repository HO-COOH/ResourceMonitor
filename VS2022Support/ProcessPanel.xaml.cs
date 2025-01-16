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
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace VS2022Support
{
    public enum ProcessPanelKind
    {
        CPU,
        RAM
    }
    /// <summary>
    /// Interaction logic for CPUPanel.xaml
    /// </summary>
    public partial class ProcessPanel : UserControl, INotifyPropertyChanged
    {

        public ProcessPanel()
        {
            InitializeComponent();
            ProcessItems.Add(new ProcessCPUUsageModel((uint)Process.GetCurrentProcess().Id));
        }

        public ObservableCollection<ProcessCPUUsageModel> ProcessItems { get; set; } = new ObservableCollection<ProcessCPUUsageModel>();

        public event PropertyChangedEventHandler PropertyChanged;
        public static event PropertyChangedEventHandler StaticPropertyChanged;
        ChildProcess.CollectionChange<ProcessCPUUsageModel> m_collectionChange;
        public void Update()
        {
            m_collectionChange = ChildProcess.SyncWithObservableCollection(ProcessItems);
        }

        public void RaiseDataChange()
        {
            if (m_collectionChange.ItemsToAdd != null)
            {
                foreach (var addItem in m_collectionChange.ItemsToAdd)
                    ProcessItems.Add(addItem);
            }
            if (m_collectionChange.ItemsToRemove != null)
            {
                foreach (var removeItem in m_collectionChange.ItemsToRemove)
                    ProcessItems.Remove(removeItem);
            }

            foreach (var item in ProcessItems)
            {
                item.RaiseDataChange();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                (((Button)sender).DataContext as ProcessCPUUsageModel).Process.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to kill process");
            }
        }

        private static bool m_isPinned = false;
        public static bool IsPinned
        {
            get => m_isPinned;
            set
            {
                m_isPinned = value;
                if (CPUToolWindowPackage.Instance == null)
                    return;
                var window = CPUToolWindowPackage.Instance.FindToolWindow(typeof(CPUToolWindow), 0, true);
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
    }
}

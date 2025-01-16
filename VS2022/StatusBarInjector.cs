using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VS2022
{
    internal class StatusBarInjector
    {
        private readonly System.Windows.Window mainWindow;

        private System.Windows.FrameworkElement statusBar;

        private System.Windows.Controls.Panel panel;

        public static StatusBarInjector Instance;

        public StatusBarInjector(System.Windows.Window pMainWindow)
        {
            mainWindow = pMainWindow;

            FindStatusBar();
            Instance = this;
        }

        private static System.Windows.DependencyObject FindChild(System.Windows.DependencyObject parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.FrameworkElement frameworkElement && frameworkElement.Name == childName)
                {
                    return frameworkElement;
                }

                child = FindChild(child, childName);

                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private void FindStatusBar()
        {
            statusBar = FindChild(mainWindow, "StatusBarContainer") as System.Windows.FrameworkElement;
            var frameworkElement = statusBar;

            if (frameworkElement != null)
            {
                panel = frameworkElement.Parent as System.Windows.Controls.DockPanel;
            }
        }

        private void RefindStatusBar()
        {
            if (panel == null)
            {
                FindStatusBar();
            }
        }

        public Color? GetForegroundColor()
        {
            if (panel == null)
                return null;

            var background = panel.Background as SolidColorBrush;
            var backgroundColor = background.Color;
            if ((backgroundColor.R * 0.299 + backgroundColor.G * 0.587 + backgroundColor.B * 0.114) > 186)
            {
                return Colors.Black;
            }
            else
            {
                return Colors.White;
            }
        }

        public void InjectControl(System.Windows.Controls.Control pControl)
        {
            RefindStatusBar();

            panel?.Dispatcher.Invoke(() => {
                pControl.SetValue(System.Windows.Controls.DockPanel.DockProperty, System.Windows.Controls.Dock.Left);
                //panel.Children.Insert(panel.Children.Count, pControl);
                panel.Children.Add(pControl);
            });
        }

        public bool IsInjected(System.Windows.FrameworkElement pControl)
        {
            RefindStatusBar();

            var flag = false;

            panel?.Dispatcher.Invoke(() => {
                flag = panel.Children.Contains(pControl);
            });

            return flag;
        }

        public void UninjectControl(System.Windows.FrameworkElement pControl)
        {
            RefindStatusBar();

            panel?.Dispatcher.Invoke(() => panel.Children.Remove(pControl));
        }
    }
}

using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VS2022Support
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("a945d18d-7d03-458c-b9c0-1ef94cc7322b")]
    public class CPUToolWindow : ToolWindowPane
    {
        public static CPUToolWindow Instance { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CPUToolWindow"/> class.
        /// </summary>
        public CPUToolWindow() : base(null)
        {
            this.Caption = "CPUToolWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ProcessPanel();
            Instance = this;
        }

        public void Update()
        {
            try
            {
                (Content as ProcessPanel).Update();
            }
            catch { }
        }

        public void RaiseDataChange()
        {
            (Content as ProcessPanel).RaiseDataChange();
        }
    }
}

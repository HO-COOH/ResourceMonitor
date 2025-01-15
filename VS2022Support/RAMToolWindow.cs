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
    [Guid("C9ACDED9-E7E2-409A-8557-ABE4740FCCFB")]
    public class RAMToolWindow : ToolWindowPane
    {
        public static RAMToolWindow Instance { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CPUToolWindow"/> class.
        /// </summary>
        public RAMToolWindow() : base(null)
        {
            this.Caption = "RAMToolWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new RAMPanel();
            Instance = this;
        }

        public void Update()
        {
            try
            {
                (Content as RAMPanel).Update();
            }
            catch { }
        }

        public void RaiseDataChange()
        {
            (Content as RAMPanel).RaiseDataChange();
        }
    }
}

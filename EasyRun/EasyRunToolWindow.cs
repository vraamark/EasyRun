using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace EasyRun
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
    [Guid("6951b3e9-e47f-48f7-94ee-44da8b812030")]
    public class EasyRunToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EasyRunToolWindow"/> class.
        /// </summary>
        public EasyRunToolWindow() : base(null)
        {
            this.Caption = "EasyRun";
            this.BitmapImageMoniker = KnownMonikers.EasyRun;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new Views.EasyRunToolWindowView();
        }
    }
}

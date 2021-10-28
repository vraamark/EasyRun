using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace EasyRun.Logging
{
    public static class Logger
    {
        private static IVsOutputWindowPane pane;
        private static Window window;

        public static void Initialize(IVsOutputWindow outputWindow, Window outputWindow2)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            GetOrCreateOutputWindowPane(outputWindow);
            window = outputWindow2;
        }

        public static void LogActive(string message, params object[] parameters)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Log(string.Format(message, parameters), activate: true);
        }

        public static void Log(string message, params object[] parameters)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Log(string.Format(message, parameters), activate: false);
        }

        public static void LogException(Exception e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Log(FormatException(e), activate: true);
        }

        private static void Log(string message, bool activate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                if (activate)
                {
                    pane.Activate();
                    window.Activate();
                }
                pane.OutputStringThreadSafe(message + Environment.NewLine);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Write(string.Format("Could not write to output window pane: {0}", FormatException(e)));
            }
        }

        private static string FormatException(Exception e)
        {
            return e.Message + Environment.NewLine + e.StackTrace;
        }

        private static void GetOrCreateOutputWindowPane(IVsOutputWindow outputWindow)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Guid guid = GuidList.guidOutputWindowPane;
            if (outputWindow.GetPane(ref guid, out pane) == VSConstants.S_OK) return;

            if (outputWindow.CreatePane(ref guid, "EasyRun", Convert.ToInt32(true), Convert.ToInt32(false)) != VSConstants.S_OK)
            {
                System.Diagnostics.Debug.Write("Could not create output window pane");
                return;
            }
            outputWindow.GetPane(ref guid, out pane);
        }
    }
}

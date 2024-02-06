using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows;

namespace ClearClock
{
    public static class SystemWindows
    {
        #region Constants

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const UInt32 SWP_DRAWFRAME = 0x0020;
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        const UInt32 SWP_HIDEWINDOW = 0x0080;
        const UInt32 SWP_NOOWNERZORDER = 0x0200;
        const UInt32 SWP_ASYNCWINDOWPOS = 0x4000;

        #endregion

        /// <summary>
        /// Activate a window from anywhere by attaching to the foreground window.
        /// </summary>
        public static void GlobalActivate(this Window w)
        {
            try
            {
                //Get the process ID for this window's thread
                var interopHelper = new WindowInteropHelper(w);
                var thisWindowThreadId = GetWindowThreadProcessId(interopHelper.Handle, IntPtr.Zero);

                //Get the process ID for the foreground window's thread
                var currentForegroundWindow = GetForegroundWindow();
                var currentForegroundWindowThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);

                //Attach this window's thread to the current window's thread
                AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, true);

                //Set the window position
                SetWindowPos(interopHelper.Handle, new IntPtr(0), 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

                //Detach this window's thread from the current window's thread
                AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, false);

                //Show and activate the window
                if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
                w.Show();
                w.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GlobalActivate(ERROR): {ex.Message}");
            }
        }

        /// <summary>
        /// Activate a window by it's process name.
        /// </summary>
        public static void ActivateByProcess(string processName)
        {
            var allProcs = System.Diagnostics.Process.GetProcessesByName(processName);
            if (allProcs.Length > 0)
            {
                System.Diagnostics.Process proc = allProcs[0];
                int hWnd = FindWindow(null, proc.MainWindowTitle.ToString());
                // Change behavior by settings the wFlags params. See http://msdn.microsoft.com/en-us/library/ms633545(VS.85).aspx
                SetWindowPos(new IntPtr(hWnd), new IntPtr(0), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"{processName}'s window was not found.");
            }
        }
        #region Imports

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImportAttribute("User32.dll")]
        private static extern int FindWindow(String ClassName, String WindowName);

        #endregion
    }
}

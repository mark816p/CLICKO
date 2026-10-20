using System;
using System.Threading;
using System.Windows.Forms;

namespace CLICKO {
    static class Program {
        private const string MUTEX_NAME = "Local\\CLICKO_SingleInstance_mark816p";
        private static Mutex _mutex;

        [STAThread]
        static void Main(string[] args) {
            // Check for single instance
            bool createdNew;
            _mutex = new Mutex(true, MUTEX_NAME, out createdNew);

            if (!createdNew) {
                // Another instance is already running (e.g. from Windows Startup).
                // Wake up the running instance to open the dashboard smoothly.
                try {
                    uint wmShow = Win32.RegisterWindowMessage("CLICKO_SHOW_DASHBOARD_MSG");
                    if (wmShow != 0) {
                        Win32.PostMessage(Win32.HWND_BROADCAST, wmShow, IntPtr.Zero, IntPtr.Zero);
                    }
                } catch { }
                return;
            }

            try {
                // Inform Windows OS to notify CLICKO of system shutdown first
                try {
                    Win32.SetProcessShutdownParameters(0x3FF, 0);
                } catch { }

                // Ensure High-DPI crisp fonts on Windows 10/11
                try {
                    Win32.SetProcessDPIAware();
                } catch { }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool startMinimized = false;
                for (int i = 0; i < args.Length; i++) {
                    string arg = args[i].ToLowerInvariant();
                    if (arg == "--minimized" || arg == "-m" || arg == "--startup" || arg == "-s") {
                        startMinimized = true;
                    } else if (arg == "--reset") {
                        ClickStats.Instance.Reset();
                    }
                }

                // Register all system shutdown and process exit hooks for guaranteed persistence
                AppDomain.CurrentDomain.ProcessExit += (s, e) => ClickStats.Instance.Save();
                Application.ApplicationExit += (s, e) => ClickStats.Instance.Save();
                try {
                    Microsoft.Win32.SystemEvents.SessionEnding += (s, e) => ClickStats.Instance.Save();
                    Microsoft.Win32.SystemEvents.SessionEnded += (s, e) => ClickStats.Instance.Save();
                } catch { }

                Application.Run(new TrayContext(startMinimized));
            } finally {
                if (_mutex != null) {
                    try { _mutex.ReleaseMutex(); } catch { }
                    _mutex.Dispose();
                }
            }
        }
    }
}

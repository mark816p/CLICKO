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
                MessageBox.Show(
                    "CLICKO is already running in your system tray.\nLook for the CLICKO icon near your Windows taskbar clock.",
                    "CLICKO Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try {
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

                // Register session ending / app exit hooks for guaranteed persistence
                AppDomain.CurrentDomain.ProcessExit += (s, e) => ClickStats.Instance.SaveIfDirty();
                Microsoft.Win32.SystemEvents.SessionEnding += (s, e) => ClickStats.Instance.SaveIfDirty();

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

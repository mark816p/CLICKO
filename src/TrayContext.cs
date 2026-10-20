using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CLICKO {
    public class TrayContext : ApplicationContext {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly DashboardForm _dashboardForm;
        private readonly RawInputWindow _rawInputWindow;
        private readonly ClickStats _stats;
        private readonly Timer _tooltipTimer;

        // Dynamic menu items
        private ToolStripMenuItem _miUsbInfo;
        private ToolStripMenuItem _miTouchpadInfo;
        private ToolStripMenuItem _miStartup;

        public TrayContext(bool startMinimized) {
            _stats = ClickStats.Instance;
            _dashboardForm = new DashboardForm();

            // Set up Raw Input Message Sink
            _rawInputWindow = new RawInputWindow();

            // Load or generate Icon
            Icon appIcon = null;
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath)) {
                try { appIcon = new Icon(iconPath); } catch { }
            }
            if (appIcon == null) {
                using (Bitmap bmp = IconGenerator.CreateIconBitmap(32)) {
                    appIcon = Icon.FromHandle(bmp.GetHicon());
                }
            }

            // Build Context Menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Renderer = new DarkMenuRenderer();

            ToolStripMenuItem miOpen = new ToolStripMenuItem("📊 Open Dashboard", null, (s, e) => ShowDashboard());
            miOpen.Font = new Font(_contextMenu.Font, FontStyle.Bold);
            _contextMenu.Items.Add(miOpen);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _miUsbInfo = new ToolStripMenuItem("🖱️ USB: 0 clicks") { Enabled = false };
            _miTouchpadInfo = new ToolStripMenuItem("💻 Touchpad: 0 clicks") { Enabled = false };
            _contextMenu.Items.Add(_miUsbInfo);
            _contextMenu.Items.Add(_miTouchpadInfo);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _miStartup = new ToolStripMenuItem("⚙️ Start with Windows", null, (s, e) => ToggleStartup()) {
                Checked = StartupManager.IsStartupEnabled(),
                CheckOnClick = true
            };
            _contextMenu.Items.Add(_miStartup);

            _contextMenu.Items.Add(new ToolStripMenuItem("🔄 Reset Statistics", null, (s, e) => ResetStats()));
            _contextMenu.Items.Add(new ToolStripMenuItem("📁 Open Data Folder", null, (s, e) => OpenDataFolder()));
            _contextMenu.Items.Add(new ToolStripMenuItem("💾 Save Stats", null, (s, e) => {
                _stats.Save();
                _trayIcon.ShowBalloonTip(1500, "CLICKO", "Statistics saved to disk.", ToolTipIcon.Info);
            }));

            _contextMenu.Items.Add(new ToolStripMenuItem("ℹ️ About CLICKO", null, (s, e) => ShowAbout()));
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(new ToolStripMenuItem("❌ Exit CLICKO", null, (s, e) => ExitApplication()));

            _contextMenu.Opening += (s, e) => UpdateMenuItems();

            // Setup System Tray Icon
            _trayIcon = new NotifyIcon {
                Icon = appIcon,
                ContextMenuStrip = _contextMenu,
                Text = "CLICKO - Click Counter",
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ShowDashboard();

            // Periodically refresh tooltip and menu numbers
            _tooltipTimer = new Timer { Interval = 1000 };
            _tooltipTimer.Tick += (s, e) => UpdateTooltip();
            _tooltipTimer.Start();
            UpdateTooltip();

            if (!startMinimized) {
                ShowDashboard();
            } else {
                _trayIcon.ShowBalloonTip(2000, "CLICKO", "Running in background. Tracking clicks from USB & Touchpad.", ToolTipIcon.Info);
            }
        }

        private void ShowDashboard() {
            if (_dashboardForm.Visible) {
                _dashboardForm.WindowState = FormWindowState.Normal;
                _dashboardForm.Activate();
            } else {
                _dashboardForm.Show();
                _dashboardForm.WindowState = FormWindowState.Normal;
                _dashboardForm.Activate();
            }
        }

        private void UpdateTooltip() {
            long total = _stats.TotalClicks;
            string tip = string.Format("CLICKO: {0:N0} clicks\nL: {1:N0} | R: {2:N0} | M: {3:N0}\nUSB: {4:N0} | Pad: {5:N0}",
                total, _stats.TotalLeft, _stats.TotalRight, _stats.TotalMiddle,
                _stats.UsbTotal, _stats.TouchpadTotal);

            // Windows NotifyIcon.Text max length is 63 chars (or 127 in newer shell)
            if (tip.Length >= 64) {
                tip = string.Format("CLICKO: {0:N0} clicks (USB: {1:N0}, Pad: {2:N0})",
                    total, _stats.UsbTotal, _stats.TouchpadTotal);
            }
            if (tip.Length >= 64) {
                tip = string.Format("CLICKO: {0:N0} clicks", total);
            }
            try {
                _trayIcon.Text = tip;
            } catch { }
        }

        private void UpdateMenuItems() {
            _miUsbInfo.Text = string.Format("🖱️ USB: {0:N0} clicks ({1:0}%)",
                _stats.UsbTotal,
                _stats.TotalClicks > 0 ? (_stats.UsbTotal * 100.0 / _stats.TotalClicks) : 0);
            _miTouchpadInfo.Text = string.Format("💻 Touchpad: {0:N0} clicks ({1:0}%)",
                _stats.TouchpadTotal,
                _stats.TotalClicks > 0 ? (_stats.TouchpadTotal * 100.0 / _stats.TotalClicks) : 0);
            _miStartup.Checked = StartupManager.IsStartupEnabled();
        }

        private void ToggleStartup() {
            bool current = StartupManager.IsStartupEnabled();
            StartupManager.SetStartup(!current);
            _miStartup.Checked = !current;
        }

        private void ResetStats() {
            DialogResult res = MessageBox.Show(
                "Are you sure you want to reset all click counters to 0?\nThis action cannot be undone.",
                "Confirm Reset - CLICKO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (res == DialogResult.Yes) {
                _stats.Reset();
                UpdateTooltip();
            }
        }

        private void OpenDataFolder() {
            string dir = Path.GetDirectoryName(_stats.StoragePath);
            if (Directory.Exists(dir)) {
                Process.Start("explorer.exe", dir);
            }
        }

        private void ShowAbout() {
            MessageBox.Show(
                "CLICKO v1.0.0\n" +
                "Lightweight, Hardware-Aware Click Counter for Windows.\n\n" +
                "Features:\n" +
                "• Tracks Left, Right, and Middle clicks\n" +
                "• Distinguishes USB Mouse vs Built-in Touchpad via Win32 Raw Input\n" +
                "• Persistent local storage (%APPDATA%\\CLICKO\\stats.json)\n" +
                "• Windows Startup integration\n" +
                "• 0% Idle CPU and <15MB RAM\n" +
                "• 100% Local & Privacy-Friendly\n\n" +
                "Author: mark816p\nLicense: MIT",
                "About CLICKO",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExitApplication() {
            _tooltipTimer.Stop();
            _stats.Save();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _rawInputWindow.Dispose();
            _dashboardForm.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (_trayIcon != null) _trayIcon.Dispose();
                if (_contextMenu != null) _contextMenu.Dispose();
                if (_rawInputWindow != null) _rawInputWindow.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Hidden Native Window for Raw Input message sink
    public class RawInputWindow : NativeWindow, IDisposable {
        public RawInputWindow() {
            CreateParams cp = new CreateParams {
                Caption = "CLICKO_RawInputSink",
                Parent = IntPtr.Zero
            };
            CreateHandle(cp);

            // Register for Raw Input Mouse sink
            Win32.RAWINPUTDEVICE[] rid = new Win32.RAWINPUTDEVICE[1];
            rid[0].usUsagePage = Win32.USAGE_PAGE_GENERIC;
            rid[0].usUsage = Win32.USAGE_MOUSE;
            rid[0].dwFlags = Win32.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            Win32.RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(Win32.RAWINPUTDEVICE)));
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == Win32.WM_INPUT) {
                ProcessRawInput(m.LParam);
            }
            base.WndProc(ref m);
        }

        private void ProcessRawInput(IntPtr hRawInput) {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(Win32.RAWINPUTHEADER));
            Win32.GetRawInputData(hRawInput, Win32.RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
            if (dwSize <= 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try {
                if (Win32.GetRawInputData(hRawInput, Win32.RID_INPUT, buffer, ref dwSize, headerSize) == dwSize) {
                    Win32.RAWINPUT raw = (Win32.RAWINPUT)Marshal.PtrToStructure(buffer, typeof(Win32.RAWINPUT));
                    if (raw.header.dwType == Win32.RIM_TYPEMOUSE) {
                        ushort flags = raw.mouse.usButtonFlags;

                        // Check button down flags
                        bool isLeft = (flags & Win32.RI_MOUSE_LEFT_BUTTON_DOWN) != 0;
                        bool isRight = (flags & Win32.RI_MOUSE_RIGHT_BUTTON_DOWN) != 0;
                        bool isMiddle = (flags & Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0;

                        if (isLeft || isRight || isMiddle) {
                            DeviceClassification dev = DeviceDetector.Classify(raw.header.hDevice);

                            if (isLeft) ClickStats.Instance.RecordClick(ClickButton.Left, dev);
                            if (isRight) ClickStats.Instance.RecordClick(ClickButton.Right, dev);
                            if (isMiddle) ClickStats.Instance.RecordClick(ClickButton.Middle, dev);
                        }
                    }
                }
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose() {
            DestroyHandle();
        }
    }

    public class DarkMenuRenderer : ToolStripProfessionalRenderer {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }
    }

    public class DarkColorTable : ProfessionalColorTable {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(20, 23, 34); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(20, 23, 34); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(20, 23, 34); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(20, 23, 34); } }
        public override Color MenuBorder { get { return Color.FromArgb(45, 52, 75); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(59, 130, 246); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(35, 42, 65); } }
        public override Color MenuStripGradientBegin { get { return Color.FromArgb(20, 23, 34); } }
        public override Color MenuStripGradientEnd { get { return Color.FromArgb(20, 23, 34); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(35, 42, 65); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(35, 42, 65); } }
        public override Color MenuItemPressedGradientBegin { get { return Color.FromArgb(30, 36, 56); } }
        public override Color MenuItemPressedGradientEnd { get { return Color.FromArgb(30, 36, 56); } }
        public override Color SeparatorDark { get { return Color.FromArgb(42, 48, 70); } }
        public override Color SeparatorLight { get { return Color.FromArgb(20, 23, 34); } }
    }
}

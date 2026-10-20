using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace CLICKO {
    public class DashboardForm : Form {
        private readonly ClickStats _stats;
        private readonly Timer _refreshTimer;

        // Visual flash timers for real-time click feedback
        private DateTime _lastLeftFlash = DateTime.MinValue;
        private DateTime _lastRightFlash = DateTime.MinValue;
        private DateTime _lastMiddleFlash = DateTime.MinValue;

        // UI Controls
        private Label lblTotalNumber;
        private Label lblLeftNumber;
        private Label lblLeftPercent;
        private Label lblRightNumber;
        private Label lblRightPercent;
        private Label lblMiddleNumber;
        private Label lblMiddlePercent;

        private Label lblUsbTotal;
        private Label lblUsbDetails;
        private Label lblTouchpadTotal;
        private Label lblTouchpadDetails;

        private Label lblTodayNumber;
        private Label lblStoragePath;

        private Panel pnlLeftCard;
        private Panel pnlRightCard;
        private Panel pnlMiddleCard;
        private Panel pnlSplitBar;

        private CheckBox chkStartup;

        public DashboardForm() {
            _stats = ClickStats.Instance;
            InitializeComponent();

            _stats.ClickRegistered += OnClickRegistered;

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 80;
            _refreshTimer.Tick += (s, e) => UpdateStatsUI();
            _refreshTimer.Start();

            UpdateStatsUI();
        }

        private void InitializeComponent() {
            this.Text = "CLICKO - Hardware-Aware Click Counter";
            this.Size = new Size(620, 680);
            this.MinimumSize = new Size(620, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 17, 26);
            this.ForeColor = Color.FromArgb(243, 244, 246);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.DoubleBuffered = true;

            try {
                this.Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"));
            } catch { }

            // Main Layout Container (Scrollable)
            Panel root = new Panel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 20, 24, 20)
            };
            this.Controls.Add(root);

            int y = 10;

            // ================= HEADER =================
            Panel pnlHeader = new Panel {
                Location = new Point(20, y),
                Size = new Size(560, 64),
                BackColor = Color.Transparent
            };

            Label lblTitle = new Label {
                Text = "CLICKO",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label {
                Text = "LIGHTWEIGHT HARDWARE-AWARE CLICK TRACKER",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(3, 38),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Live status badge
            Label lblLiveBadge = new Label {
                Text = "● ACTIVE MONITORING",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(20, 38, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(390, 8),
                Size = new Size(165, 30)
            };
            lblLiveBadge.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(16, 185, 129), 1f)) {
                    e.Graphics.DrawRectangle(p, 0, 0, lblLiveBadge.Width - 1, lblLiveBadge.Height - 1);
                }
            };
            pnlHeader.Controls.Add(lblLiveBadge);

            root.Controls.Add(pnlHeader);
            y += 74;

            // ================= TOTAL HERO CARD =================
            Panel pnlHero = CreateCardPanel(20, y, 560, 186);

            Label lblTotalLabel = new Label {
                Text = "TOTAL CLICKS RECORDED",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 14),
                AutoSize = true
            };
            pnlHero.Controls.Add(lblTotalLabel);

            lblTotalNumber = new Label {
                Text = "0",
                Font = new Font("Segoe UI", 36f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                Location = new Point(16, 32),
                Size = new Size(520, 68),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHero.Controls.Add(lblTotalNumber);

            // 3 Columns: Left, Right, Middle clicks
            int colW = 168;
            int colY = 105;

            // Left Pill Card
            pnlLeftCard = CreatePillPanel(20, colY, colW, 64, Color.FromArgb(6, 182, 212));
            Label lblLeftTag = new Label {
                Text = "LEFT BUTTON",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 182, 212),
                Location = new Point(12, 8),
                AutoSize = true
            };
            lblLeftNumber = new Label {
                Text = "0",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 24),
                AutoSize = true
            };
            lblLeftPercent = new Label {
                Text = "0%",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(120, 32),
                AutoSize = true
            };
            pnlLeftCard.Controls.Add(lblLeftTag);
            pnlLeftCard.Controls.Add(lblLeftNumber);
            pnlLeftCard.Controls.Add(lblLeftPercent);
            pnlHero.Controls.Add(pnlLeftCard);

            // Right Pill Card
            pnlRightCard = CreatePillPanel(20 + colW + 8, colY, colW, 64, Color.FromArgb(168, 85, 247));
            Label lblRightTag = new Label {
                Text = "RIGHT BUTTON",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(168, 85, 247),
                Location = new Point(12, 8),
                AutoSize = true
            };
            lblRightNumber = new Label {
                Text = "0",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 24),
                AutoSize = true
            };
            lblRightPercent = new Label {
                Text = "0%",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(120, 32),
                AutoSize = true
            };
            pnlRightCard.Controls.Add(lblRightTag);
            pnlRightCard.Controls.Add(lblRightNumber);
            pnlRightCard.Controls.Add(lblRightPercent);
            pnlHero.Controls.Add(pnlRightCard);

            // Middle Pill Card
            pnlMiddleCard = CreatePillPanel(20 + (colW + 8) * 2, colY, colW, 64, Color.FromArgb(245, 158, 11));
            Label lblMiddleTag = new Label {
                Text = "MIDDLE WHEEL",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11),
                Location = new Point(12, 8),
                AutoSize = true
            };
            lblMiddleNumber = new Label {
                Text = "0",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 24),
                AutoSize = true
            };
            lblMiddlePercent = new Label {
                Text = "0%",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(120, 32),
                AutoSize = true
            };
            pnlMiddleCard.Controls.Add(lblMiddleTag);
            pnlMiddleCard.Controls.Add(lblMiddleNumber);
            pnlMiddleCard.Controls.Add(lblMiddlePercent);
            pnlHero.Controls.Add(pnlMiddleCard);

            root.Controls.Add(pnlHero);
            y += 198;

            // ================= HARDWARE DEVICE BREAKDOWN =================
            Panel pnlDevice = CreateCardPanel(20, y, 560, 180);

            Label lblDevTitle = new Label {
                Text = "HARDWARE SOURCE IDENTIFICATION",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 14),
                AutoSize = true
            };
            pnlDevice.Controls.Add(lblDevTitle);

            // USB Mouse Section
            Label lblUsbIcon = new Label {
                Text = "🖱️ USB MOUSE",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                Location = new Point(20, 42),
                AutoSize = true
            };
            lblUsbTotal = new Label {
                Text = "0 clicks (0%)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(340, 42),
                Size = new Size(200, 24),
                TextAlign = ContentAlignment.MiddleRight
            };
            lblUsbDetails = new Label {
                Text = "L: 0   |   R: 0   |   M: 0",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(22, 66),
                AutoSize = true
            };
            pnlDevice.Controls.Add(lblUsbIcon);
            pnlDevice.Controls.Add(lblUsbTotal);
            pnlDevice.Controls.Add(lblUsbDetails);

            // Touchpad Section
            Label lblTouchpadIcon = new Label {
                Text = "💻 BUILT-IN TOUCHPAD",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(20, 96),
                AutoSize = true
            };
            lblTouchpadTotal = new Label {
                Text = "0 clicks (0%)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(340, 96),
                Size = new Size(200, 24),
                TextAlign = ContentAlignment.MiddleRight
            };
            lblTouchpadDetails = new Label {
                Text = "L: 0   |   R: 0   |   M: 0",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(22, 120),
                AutoSize = true
            };
            pnlDevice.Controls.Add(lblTouchpadIcon);
            pnlDevice.Controls.Add(lblTouchpadTotal);
            pnlDevice.Controls.Add(lblTouchpadDetails);

            // Split Bar showing USB vs Touchpad
            pnlSplitBar = new Panel {
                Location = new Point(20, 150),
                Size = new Size(520, 14),
                BackColor = Color.FromArgb(30, 35, 50)
            };
            pnlSplitBar.Paint += PaintSplitBar;
            pnlDevice.Controls.Add(pnlSplitBar);

            root.Controls.Add(pnlDevice);
            y += 192;

            // ================= TODAY / PERSISTENCE INFO =================
            Panel pnlToday = CreateCardPanel(20, y, 560, 68);

            Label lblTodayLabel = new Label {
                Text = "TODAY'S CLICKS",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 14),
                AutoSize = true
            };
            lblTodayNumber = new Label {
                Text = "0",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(18, 32),
                AutoSize = true
            };

            lblStoragePath = new Label {
                Text = "Persistent Storage: " + _stats.StoragePath,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(170, 38),
                Size = new Size(370, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlToday.Controls.Add(lblTodayLabel);
            pnlToday.Controls.Add(lblTodayNumber);
            pnlToday.Controls.Add(lblStoragePath);
            root.Controls.Add(pnlToday);
            y += 80;

            // ================= OPTIONS & ACTIONS =================
            chkStartup = new CheckBox {
                Text = "Launch automatically on Windows Startup",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(24, y),
                AutoSize = true,
                Checked = StartupManager.IsStartupEnabled()
            };
            chkStartup.CheckedChanged += (s, e) => {
                StartupManager.SetStartup(chkStartup.Checked);
            };
            root.Controls.Add(chkStartup);
            y += 34;

            // Buttons
            Button btnReset = CreateStyledButton("🔄 Reset Stats", 20, y, 125, 34, Color.FromArgb(239, 68, 68));
            btnReset.Click += (s, e) => {
                DialogResult res = MessageBox.Show(
                    "Are you sure you want to reset all click counters to 0?\nThis action cannot be undone.",
                    "Confirm Reset - CLICKO",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (res == DialogResult.Yes) {
                    _stats.Reset();
                    UpdateStatsUI();
                }
            };
            root.Controls.Add(btnReset);

            Button btnDataFolder = CreateStyledButton("📁 Open Folder", 155, y, 125, 34, Color.FromArgb(71, 85, 105));
            btnDataFolder.Click += (s, e) => {
                string dir = Path.GetDirectoryName(_stats.StoragePath);
                if (Directory.Exists(dir)) {
                    Process.Start("explorer.exe", dir);
                }
            };
            root.Controls.Add(btnDataFolder);

            Button btnSave = CreateStyledButton("💾 Save Now", 290, y, 110, 34, Color.FromArgb(16, 185, 129));
            btnSave.Click += (s, e) => {
                _stats.Save();
                MessageBox.Show("Statistics saved successfully to disk.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            root.Controls.Add(btnSave);

            Button btnHide = CreateStyledButton("🔽 Minimize", 410, y, 170, 34, Color.FromArgb(59, 130, 246));
            btnHide.Click += (s, e) => {
                this.Hide();
            };
            root.Controls.Add(btnHide);
        }

        private void OnClickRegistered(ClickEventData e) {
            DateTime now = DateTime.Now;
            if (e.Button == ClickButton.Left) _lastLeftFlash = now;
            else if (e.Button == ClickButton.Right) _lastRightFlash = now;
            else if (e.Button == ClickButton.Middle) _lastMiddleFlash = now;
        }

        private void UpdateStatsUI() {
            long total = _stats.TotalClicks;
            lblTotalNumber.Text = string.Format("{0:N0}", total);

            long left = _stats.TotalLeft;
            long right = _stats.TotalRight;
            long middle = _stats.TotalMiddle;

            double leftPct = total > 0 ? (left * 100.0 / total) : 0;
            double rightPct = total > 0 ? (right * 100.0 / total) : 0;
            double middlePct = total > 0 ? (middle * 100.0 / total) : 0;

            lblLeftNumber.Text = string.Format("{0:N0}", left);
            lblLeftPercent.Text = string.Format("{0:0.0}%", leftPct);

            lblRightNumber.Text = string.Format("{0:N0}", right);
            lblRightPercent.Text = string.Format("{0:0.0}%", rightPct);

            lblMiddleNumber.Text = string.Format("{0:N0}", middle);
            lblMiddlePercent.Text = string.Format("{0:0.0}%", middlePct);

            // USB
            long usbTotal = _stats.UsbTotal;
            double usbPct = total > 0 ? (usbTotal * 100.0 / total) : 0;
            lblUsbTotal.Text = string.Format("{0:N0} clicks ({1:0.0}%)", usbTotal, usbPct);
            lblUsbDetails.Text = string.Format("Left: {0:N0}   |   Right: {1:N0}   |   Middle: {2:N0}",
                _stats.UsbLeft, _stats.UsbRight, _stats.UsbMiddle);

            // Touchpad
            long tpTotal = _stats.TouchpadTotal;
            double tpPct = total > 0 ? (tpTotal * 100.0 / total) : 0;
            lblTouchpadTotal.Text = string.Format("{0:N0} clicks ({1:0.0}%)", tpTotal, tpPct);
            lblTouchpadDetails.Text = string.Format("Left: {0:N0}   |   Right: {1:N0}   |   Middle: {2:N0}",
                _stats.TouchpadLeft, _stats.TouchpadRight, _stats.TouchpadMiddle);

            // Today
            lblTodayNumber.Text = string.Format("{0:N0}", _stats.TodayTotal);

            // Redraw split bar and flash cards
            pnlSplitBar.Invalidate();
            pnlLeftCard.Invalidate();
            pnlRightCard.Invalidate();
            pnlMiddleCard.Invalidate();
        }

        private void PaintSplitBar(object sender, PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            long total = _stats.TotalClicks;
            int w = pnlSplitBar.Width;
            int h = pnlSplitBar.Height;

            if (total == 0) {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(40, 45, 60))) {
                    g.FillRectangle(b, 0, 0, w, h);
                }
                return;
            }

            int usbWidth = (int)((_stats.UsbTotal / (double)total) * w);
            int tpWidth = w - usbWidth;

            if (usbWidth > 0) {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(59, 130, 246))) {
                    g.FillRectangle(b, 0, 0, usbWidth, h);
                }
            }
            if (tpWidth > 0) {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(16, 185, 129))) {
                    g.FillRectangle(b, usbWidth, 0, tpWidth, h);
                }
            }
        }

        private Panel CreateCardPanel(int x, int y, int w, int h) {
            Panel p = new Panel {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(26, 29, 43)
            };
            p.Paint += (s, e) => {
                using (Pen borderPen = new Pen(Color.FromArgb(42, 46, 68), 1f)) {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
                }
            };
            return p;
        }

        private Panel CreatePillPanel(int x, int y, int w, int h, Color accent) {
            Panel p = new Panel {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(19, 22, 34)
            };
            p.Paint += (s, e) => {
                bool flash = false;
                DateTime now = DateTime.Now;
                if (accent.R == 6 && (now - _lastLeftFlash).TotalMilliseconds < 150) flash = true;
                if (accent.R == 168 && (now - _lastRightFlash).TotalMilliseconds < 150) flash = true;
                if (accent.R == 245 && (now - _lastMiddleFlash).TotalMilliseconds < 150) flash = true;

                Color borderColor = flash ? Color.White : Color.FromArgb(45, 52, 75);
                using (Pen pen = new Pen(borderColor, flash ? 2f : 1f)) {
                    e.Graphics.DrawRectangle(pen, 0, 0, w - 1, h - 1);
                }
                if (flash) {
                    using (SolidBrush flashBrush = new SolidBrush(Color.FromArgb(40, accent.R, accent.G, accent.B))) {
                        e.Graphics.FillRectangle(flashBrush, 1, 1, w - 2, h - 2);
                    }
                }
            };
            return p;
        }

        private Button CreateStyledButton(string text, int x, int y, int w, int h, Color bg) {
            Button btn = new Button {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == Win32.WM_QUERYENDSESSION) {
                _stats.Save();
                m.Result = (IntPtr)1;
                return;
            }
            if (m.Msg == Win32.WM_ENDSESSION) {
                if (m.WParam != IntPtr.Zero) {
                    _stats.Save();
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            // When user clicks 'X', minimize to tray instead of quitting
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.Hide();
            } else {
                base.OnFormClosing(e);
            }
        }
    }
}

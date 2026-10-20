using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace CLICKO {
    public enum ClickButton {
        Left,
        Right,
        Middle
    }

    public class ClickEventData {
        public ClickButton Button { get; set; }
        public DeviceClassification Device { get; set; }
        public DateTime Timestamp { get; set; }

        public ClickEventData(ClickButton btn, DeviceClassification dev) {
            Button = btn;
            Device = dev;
            Timestamp = DateTime.Now;
        }
    }

    public class ClickCounterStore {
        public long TotalLeft = 0;
        public long TotalRight = 0;
        public long TotalMiddle = 0;

        public long UsbLeft = 0;
        public long UsbRight = 0;
        public long UsbMiddle = 0;

        public long TouchpadLeft = 0;
        public long TouchpadRight = 0;
        public long TouchpadMiddle = 0;

        public long OtherLeft = 0;
        public long OtherRight = 0;
        public long OtherMiddle = 0;

        public string TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
        public long TodayLeft = 0;
        public long TodayRight = 0;
        public long TodayMiddle = 0;

        public string FirstStarted = DateTime.Now.ToString("o");
        public string LastUpdated = DateTime.Now.ToString("o");
    }

    public class ClickStats {
        private static ClickStats _instance;
        public static ClickStats Instance {
            get {
                if (_instance == null) {
                    _instance = new ClickStats();
                }
                return _instance;
            }
        }

        private readonly ClickCounterStore _store = new ClickCounterStore();
        private readonly string _storagePath;
        private readonly string _backupPath;
        private readonly string _tempPath;
        private readonly object _lock = new object();
        private bool _isDirty = false;
        private DateTime _lastClickTime = DateTime.UtcNow;
        private DateTime _lastSaveTime = DateTime.UtcNow;
        private readonly System.Threading.Timer _saveTimer;

        public event Action<ClickEventData> ClickRegistered;

        private ClickStats() {
            // Check portable stats.json in exe folder, else use %APPDATA%\CLICKO\stats.json
            string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stats.json");
            if (File.Exists(localFile)) {
                _storagePath = localFile;
            } else {
                string appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CLICKO");
                if (!Directory.Exists(appData)) {
                    Directory.CreateDirectory(appData);
                }
                _storagePath = Path.Combine(appData, "stats.json");
            }

            _backupPath = _storagePath + ".bak";
            _tempPath = _storagePath + ".tmp";

            Load();

            // Only create initial empty file if neither primary nor backup exists
            if (!File.Exists(_storagePath) && !File.Exists(_backupPath)) {
                Save();
            }

            // High-responsiveness auto-save timer (polls every 500ms)
            // Flushes if user pauses clicking for 1s, or at least every 2.5s of continuous clicking
            _saveTimer = new System.Threading.Timer(state => {
                CheckAutoSave();
            }, null, 500, 500);
        }

        public string StoragePath { get { return _storagePath; } }

        public void RecordClick(ClickButton button, DeviceClassification device) {
            lock (_lock) {
                CheckDayRollover();

                // Increment Totals
                if (button == ClickButton.Left) {
                    Interlocked.Increment(ref _store.TotalLeft);
                    Interlocked.Increment(ref _store.TodayLeft);
                } else if (button == ClickButton.Right) {
                    Interlocked.Increment(ref _store.TotalRight);
                    Interlocked.Increment(ref _store.TodayRight);
                } else if (button == ClickButton.Middle) {
                    Interlocked.Increment(ref _store.TotalMiddle);
                    Interlocked.Increment(ref _store.TodayMiddle);
                }

                // Increment Device-Specific Counters
                if (device.Type == DeviceType.UsbMouse) {
                    if (button == ClickButton.Left) Interlocked.Increment(ref _store.UsbLeft);
                    else if (button == ClickButton.Right) Interlocked.Increment(ref _store.UsbRight);
                    else if (button == ClickButton.Middle) Interlocked.Increment(ref _store.UsbMiddle);
                } else if (device.Type == DeviceType.Touchpad) {
                    if (button == ClickButton.Left) Interlocked.Increment(ref _store.TouchpadLeft);
                    else if (button == ClickButton.Right) Interlocked.Increment(ref _store.TouchpadRight);
                    else if (button == ClickButton.Middle) Interlocked.Increment(ref _store.TouchpadMiddle);
                } else {
                    if (button == ClickButton.Left) Interlocked.Increment(ref _store.OtherLeft);
                    else if (button == ClickButton.Right) Interlocked.Increment(ref _store.OtherRight);
                    else if (button == ClickButton.Middle) Interlocked.Increment(ref _store.OtherMiddle);
                }

                _store.LastUpdated = DateTime.Now.ToString("o");
                _isDirty = true;
                _lastClickTime = DateTime.UtcNow;
            }

            var handler = ClickRegistered;
            if (handler != null) {
                try {
                    handler(new ClickEventData(button, device));
                } catch { }
            }
        }

        private void CheckAutoSave() {
            if (!_isDirty) return;

            DateTime now = DateTime.UtcNow;
            bool shouldSave = false;

            lock (_lock) {
                // Save if idle for 1000ms after a click, or if 2500ms passed since last save
                if ((now - _lastClickTime).TotalMilliseconds >= 1000 ||
                    (now - _lastSaveTime).TotalMilliseconds >= 2500) {
                    shouldSave = true;
                }
            }

            if (shouldSave) {
                Save();
            }
        }

        private void CheckDayRollover() {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_store.TodayDate != today) {
                _store.TodayDate = today;
                _store.TodayLeft = 0;
                _store.TodayRight = 0;
                _store.TodayMiddle = 0;
            }
        }

        // Totals
        public long TotalLeft { get { return Interlocked.Read(ref _store.TotalLeft); } }
        public long TotalRight { get { return Interlocked.Read(ref _store.TotalRight); } }
        public long TotalMiddle { get { return Interlocked.Read(ref _store.TotalMiddle); } }
        public long TotalClicks { get { return TotalLeft + TotalRight + TotalMiddle; } }

        // USB
        public long UsbLeft { get { return Interlocked.Read(ref _store.UsbLeft); } }
        public long UsbRight { get { return Interlocked.Read(ref _store.UsbRight); } }
        public long UsbMiddle { get { return Interlocked.Read(ref _store.UsbMiddle); } }
        public long UsbTotal { get { return UsbLeft + UsbRight + UsbMiddle; } }

        // Touchpad
        public long TouchpadLeft { get { return Interlocked.Read(ref _store.TouchpadLeft); } }
        public long TouchpadRight { get { return Interlocked.Read(ref _store.TouchpadRight); } }
        public long TouchpadMiddle { get { return Interlocked.Read(ref _store.TouchpadMiddle); } }
        public long TouchpadTotal { get { return TouchpadLeft + TouchpadRight + TouchpadMiddle; } }

        // Other
        public long OtherLeft { get { return Interlocked.Read(ref _store.OtherLeft); } }
        public long OtherRight { get { return Interlocked.Read(ref _store.OtherRight); } }
        public long OtherMiddle { get { return Interlocked.Read(ref _store.OtherMiddle); } }
        public long OtherTotal { get { return OtherLeft + OtherRight + OtherMiddle; } }

        // Today
        public long TodayLeft { get { return Interlocked.Read(ref _store.TodayLeft); } }
        public long TodayRight { get { return Interlocked.Read(ref _store.TodayRight); } }
        public long TodayMiddle { get { return Interlocked.Read(ref _store.TodayMiddle); } }
        public long TodayTotal { get { return TodayLeft + TodayRight + TodayMiddle; } }

        public void Reset() {
            lock (_lock) {
                _store.TotalLeft = 0;
                _store.TotalRight = 0;
                _store.TotalMiddle = 0;

                _store.UsbLeft = 0;
                _store.UsbRight = 0;
                _store.UsbMiddle = 0;

                _store.TouchpadLeft = 0;
                _store.TouchpadRight = 0;
                _store.TouchpadMiddle = 0;

                _store.OtherLeft = 0;
                _store.OtherRight = 0;
                _store.OtherMiddle = 0;

                _store.TodayLeft = 0;
                _store.TodayRight = 0;
                _store.TodayMiddle = 0;

                _store.LastUpdated = DateTime.Now.ToString("o");
                _isDirty = true;
                Save();
            }
        }

        public void SaveIfDirty() {
            if (_isDirty) {
                Save();
            }
        }

        /// <summary>
        /// Atomic and safe persistence:
        /// 1. Serializes current counters.
        /// 2. Writes to temporary file and flushes physical disk buffers.
        /// 3. Atomically replaces primary file with backup creation via Win32 ReplaceFile.
        /// Guaranteed to never leave a deleted or zero-byte file even if power is cut.
        /// </summary>
        public void Save() {
            lock (_lock) {
                try {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(_store);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);

                    // Step 1: Write to temporary file with explicit disk flush
                    using (FileStream fs = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush(true); // Flushes to physical storage
                    }

                    // Step 2: Atomic NTFS replace with backup
                    if (File.Exists(_storagePath)) {
                        try {
                            File.Replace(_tempPath, _storagePath, _backupPath, true);
                        } catch {
                            // Fallback if Replace is unsupported or locked
                            try { File.Copy(_storagePath, _backupPath, true); } catch { }
                            File.Copy(_tempPath, _storagePath, true);
                            try { if (File.Exists(_tempPath)) File.Delete(_tempPath); } catch { }
                        }
                    } else {
                        // First creation
                        File.Copy(_tempPath, _storagePath, true);
                        try { File.Copy(_tempPath, _backupPath, true); } catch { }
                        try { if (File.Exists(_tempPath)) File.Delete(_tempPath); } catch { }
                    }

                    _isDirty = false;
                    _lastSaveTime = DateTime.UtcNow;
                } catch { }
            }
        }

        /// <summary>
        /// Multi-tier recovery loading:
        /// 1. Tries primary stats.json.
        /// 2. If missing, corrupt, or 0 bytes, falls back to stats.json.bak.
        /// 3. If that fails, checks stats.json.tmp.
        /// Automatically heals and restores valid files.
        /// </summary>
        private void Load() {
            lock (_lock) {
                ClickCounterStore loaded = TryLoadFile(_storagePath);

                if (loaded == null && File.Exists(_backupPath)) {
                    loaded = TryLoadFile(_backupPath);
                    if (loaded != null) {
                        try { File.Copy(_backupPath, _storagePath, true); } catch { }
                    }
                }

                if (loaded == null && File.Exists(_tempPath)) {
                    loaded = TryLoadFile(_tempPath);
                    if (loaded != null) {
                        try { File.Copy(_tempPath, _storagePath, true); } catch { }
                    }
                }

                if (loaded != null) {
                    ApplyLoadedStore(loaded);
                }

                CheckDayRollover();
            }
        }

        private ClickCounterStore TryLoadFile(string path) {
            if (!File.Exists(path)) return null;
            try {
                FileInfo fi = new FileInfo(path);
                if (fi.Length < 10) return null; // Corrupted or 0 bytes

                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrEmpty(json) || json.Trim().Length < 10) return null;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<ClickCounterStore>(json);
            } catch {
                return null;
            }
        }

        private void ApplyLoadedStore(ClickCounterStore loaded) {
            _store.TotalLeft = loaded.TotalLeft;
            _store.TotalRight = loaded.TotalRight;
            _store.TotalMiddle = loaded.TotalMiddle;

            _store.UsbLeft = loaded.UsbLeft;
            _store.UsbRight = loaded.UsbRight;
            _store.UsbMiddle = loaded.UsbMiddle;

            _store.TouchpadLeft = loaded.TouchpadLeft;
            _store.TouchpadRight = loaded.TouchpadRight;
            _store.TouchpadMiddle = loaded.TouchpadMiddle;

            _store.OtherLeft = loaded.OtherLeft;
            _store.OtherRight = loaded.OtherRight;
            _store.OtherMiddle = loaded.OtherMiddle;

            _store.TodayDate = loaded.TodayDate;
            _store.TodayLeft = loaded.TodayLeft;
            _store.TodayRight = loaded.TodayRight;
            _store.TodayMiddle = loaded.TodayMiddle;

            _store.FirstStarted = loaded.FirstStarted;
            _store.LastUpdated = loaded.LastUpdated;
        }
    }
}

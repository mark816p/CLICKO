using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CLICKO {
    public static class StartupManager {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "CLICKO";

        public static bool IsStartupEnabled() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false)) {
                    if (key == null) return false;
                    object value = key.GetValue(APP_NAME);
                    if (value == null) return false;
                    string cmd = value.ToString();
                    return cmd.IndexOf(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            } catch {
                return false;
            }
        }

        public static bool SetStartup(bool enable) {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true)) {
                    if (key == null) return false;
                    if (enable) {
                        string runCommand = string.Format("\"{0}\" --minimized", Application.ExecutablePath);
                        key.SetValue(APP_NAME, runCommand, RegistryValueKind.String);
                    } else {
                        if (key.GetValue(APP_NAME) != null) {
                            key.DeleteValue(APP_NAME, false);
                        }
                    }
                    return true;
                }
            } catch {
                return false;
            }
        }
    }
}

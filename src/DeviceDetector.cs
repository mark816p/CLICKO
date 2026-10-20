using System;
using System.Collections.Generic;
using System.Text;

namespace CLICKO {
    public enum DeviceType {
        UsbMouse,
        Touchpad,
        Other
    }

    public class DeviceClassification {
        public DeviceType Type { get; set; }
        public string DisplayName { get; set; }
        public string RawPath { get; set; }
        public string ParentId { get; set; }

        public DeviceClassification(DeviceType type, string displayName, string rawPath, string parentId) {
            Type = type;
            DisplayName = displayName;
            RawPath = rawPath ?? "";
            ParentId = parentId ?? "";
        }
    }

    public static class DeviceDetector {
        private static readonly Dictionary<IntPtr, DeviceClassification> _cache =
            new Dictionary<IntPtr, DeviceClassification>();
        private static readonly object _lock = new object();

        public static DeviceClassification Classify(IntPtr hDevice) {
            lock (_lock) {
                DeviceClassification cached;
                if (_cache.TryGetValue(hDevice, out cached)) {
                    return cached;
                }

                DeviceClassification classified = DetectDevice(hDevice);
                _cache[hDevice] = classified;
                return classified;
            }
        }

        private static DeviceClassification DetectDevice(IntPtr hDevice) {
            if (hDevice == IntPtr.Zero) {
                // Synthetic / Touchpad Tap-to-click / Injected Pointer
                long extra = Win32.GetMessageExtraInfo().ToInt64();
                // Check MI_WP_SIGNATURE (0xFF515700) for touch/pen/precision gestures
                if ((extra & 0xFFFFFF00) == 0xFF515700) {
                    return new DeviceClassification(DeviceType.Touchpad, "Touchpad (Tap)", "Synthetic:Touch", "Touch/Gesture");
                }
                return new DeviceClassification(DeviceType.Touchpad, "Touchpad / Tap", "Synthetic:ZeroHandle", "Synthetic");
            }

            string rawPath = GetDevicePath(hDevice);
            string parentId = GetParentDeviceId(rawPath);
            string upperPath = (rawPath ?? "").ToUpperInvariant();
            string upperParent = (parentId ?? "").ToUpperInvariant();

            // 1. Hardware PnP Parent Device Analysis
            if (!string.IsNullOrEmpty(upperParent)) {
                if (upperParent.StartsWith("USB\\") || upperParent.Contains("\\USB\\")) {
                    return new DeviceClassification(DeviceType.UsbMouse, "USB Mouse", rawPath, parentId);
                }
                if (upperParent.StartsWith("ACPI\\") || upperParent.StartsWith("I2C\\") ||
                    upperParent.Contains("PNP0F") || upperParent.Contains("ELAN") ||
                    upperParent.Contains("SYN") || upperParent.Contains("ALPS")) {
                    return new DeviceClassification(DeviceType.Touchpad, "Touchpad", rawPath, parentId);
                }
                if (upperParent.StartsWith("BTH\\") || upperParent.StartsWith("BTHENUM\\")) {
                    return new DeviceClassification(DeviceType.UsbMouse, "Bluetooth/USB Mouse", rawPath, parentId);
                }
            }

            // 2. Interface Path Heuristics
            if (upperPath.Contains("ELAN") ||
                upperPath.Contains("SYN") ||
                upperPath.Contains("ALPS") ||
                upperPath.Contains("TOUCHPAD") ||
                upperPath.Contains("TRACKPAD") ||
                upperPath.Contains("MSFT0001") ||
                upperPath.Contains("ETD") ||
                upperPath.Contains("GLIDEPOINT") ||
                upperPath.Contains("ACPI") ||
                upperPath.Contains("I2C")) {
                return new DeviceClassification(DeviceType.Touchpad, "Touchpad", rawPath, parentId);
            }

            if (upperPath.Contains("USB") || (upperPath.Contains("VID_") && upperPath.Contains("PID_"))) {
                return new DeviceClassification(DeviceType.UsbMouse, "USB Mouse", rawPath, parentId);
            }

            return new DeviceClassification(DeviceType.Other, "Other Pointer", rawPath, parentId);
        }

        private static string GetDevicePath(IntPtr hDevice) {
            uint pcbSize = 0;
            Win32.GetRawInputDeviceInfo(hDevice, Win32.RIDI_DEVICENAME, null, ref pcbSize);
            if (pcbSize <= 0) return "";

            StringBuilder sb = new StringBuilder((int)pcbSize + 2);
            if (Win32.GetRawInputDeviceInfo(hDevice, Win32.RIDI_DEVICENAME, sb, ref pcbSize) > 0) {
                return sb.ToString();
            }
            return "";
        }

        private static string GetParentDeviceId(string rawPath) {
            if (string.IsNullOrEmpty(rawPath)) return "";

            try {
                // Device paths typically start with \\?\ or \\.\
                string clean = rawPath;
                if (clean.StartsWith(@"\\?\") || clean.StartsWith(@"\\.\")) {
                    clean = clean.Substring(4);
                }

                string[] parts = clean.Split('#');
                if (parts.Length < 3) return "";

                string instanceId = parts[0] + @"\" + parts[1] + @"\" + parts[2];

                uint devInst;
                int cr = Win32.CM_Locate_DevNode(out devInst, instanceId, 0);
                if (cr != 0) return "";

                uint parentInst;
                cr = Win32.CM_Get_Parent(out parentInst, devInst, 0);
                if (cr != 0) return "";

                StringBuilder sb = new StringBuilder(512);
                cr = Win32.CM_Get_Device_ID(parentInst, sb, sb.Capacity, 0);
                if (cr == 0) {
                    return sb.ToString();
                }
            } catch {
                // Fallback to empty if PnP query throws
            }
            return "";
        }
    }
}

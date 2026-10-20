using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CLICKO {
    public static class Win32 {
        public const int WM_INPUT = 0x00FF;

        public const ushort USAGE_PAGE_GENERIC = 0x0001;
        public const ushort USAGE_MOUSE = 0x0002;
        public const uint RIDEV_INPUTSINK = 0x00000100;

        public const uint RID_INPUT = 0x10000003;
        public const uint RID_HEADER = 0x10000005;
        public const uint RIDI_DEVICENAME = 0x20000007;
        public const uint RIDI_DEVICEINFO = 0x2000000b;

        public const uint RIM_TYPEMOUSE = 0;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;

        public const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
        public const ushort RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
        public const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
        public const ushort RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
        public const ushort RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
        public const ushort RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020;
        public const ushort RI_MOUSE_BUTTON_4_DOWN = 0x0040;
        public const ushort RI_MOUSE_BUTTON_5_DOWN = 0x0080;
        public const ushort RI_MOUSE_WHEEL = 0x0400;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RAWMOUSE {
            [FieldOffset(0)] public ushort usFlags;
            [FieldOffset(4)] public uint ulButtons;
            [FieldOffset(4)] public ushort usButtonFlags;
            [FieldOffset(6)] public ushort usButtonData;
            [FieldOffset(8)] public uint ulRawButtons;
            [FieldOffset(12)] public int lLastX;
            [FieldOffset(16)] public int lLastY;
            [FieldOffset(20)] public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice,
            uint uiCommand,
            StringBuilder pData,
            ref uint pcbSize);

        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        // cfgmgr32.dll PnP device tree navigation
        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, int ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern int CM_Get_Parent(out uint pdnDevInst, uint dnDevInst, int ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CM_Get_Device_ID(uint dnDevInst, StringBuilder Buffer, int BufferLen, int ulFlags);
    }
}

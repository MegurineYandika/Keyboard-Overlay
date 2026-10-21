using System;
using System.Runtime.InteropServices;

namespace KeysPerSecond.Native
{
    public static unsafe class NativeMethods
    {
        // Window styles & flags
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        // Window Messages
        public const int WM_INPUT = 0x00FF;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        // Raw Input Constants
        public const ushort HID_USAGE_PAGE_GENERIC = 0x01;
        public const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;

        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RIDEV_REMOVE = 0x00000001;

        public const uint RID_INPUT = 0x10000003;
        public const uint RID_HEADER = 0x10000005;

        public const uint RIM_TYPEMOUSE = 0;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;

        public const ushort RI_KEY_MAKE = 0;
        public const ushort RI_KEY_BREAK = 1;
        public const ushort RI_KEY_E0 = 2;
        public const ushort RI_KEY_E1 = 4;

        // Hotkey Modifiers
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // P/Invoke: Raw Input
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE* pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            void* pData,
            uint* pcbSize,
            uint cbSizeHeader);

        // P/Invoke: Hotkeys
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // P/Invoke: Get/SetWindowLongPtr (32-bit & 64-bit safe)
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTKEYBOARD
    {
        public RAWINPUTHEADER Header;
        public RAWKEYBOARD Keyboard;
    }
}

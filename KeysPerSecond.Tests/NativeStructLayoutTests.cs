using System;
using System.Runtime.InteropServices;
using KeysPerSecond.Native;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class NativeStructLayoutTests
    {
        [Fact]
        public void RAWINPUTDEVICE_Size_MatchesWin32()
        {
            int expected = IntPtr.Size == 8 ? 16 : 12; // ushort(2) + ushort(2) + uint(4) + IntPtr(8 or 4)
            int actual = Marshal.SizeOf<RAWINPUTDEVICE>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RAWINPUTHEADER_Size_MatchesWin32()
        {
            // dwType(4) + dwSize(4) + hDevice(8 or 4) + wParam(8 or 4)
            int expected = IntPtr.Size == 8 ? 24 : 16;
            int actual = Marshal.SizeOf<RAWINPUTHEADER>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RAWKEYBOARD_Size_MatchesWin32()
        {
            // MakeCode(2) + Flags(2) + Reserved(2) + VKey(2) + Message(4) + ExtraInformation(4) = 16
            int expected = 16;
            int actual = Marshal.SizeOf<RAWKEYBOARD>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RAWINPUTKEYBOARD_Size_MatchesWin32()
        {
            // Header (24 or 16) + Keyboard (16) = 40 or 32
            int expected = IntPtr.Size == 8 ? 40 : 32;
            int actual = Marshal.SizeOf<RAWINPUTKEYBOARD>();
            Assert.Equal(expected, actual);
        }
    }
}

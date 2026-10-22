using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Interop;
using KeysPerSecond.Models;
using KeysPerSecond.Native;
using KeysPerSecond.ViewModels;

namespace KeysPerSecond.Services
{
    public sealed unsafe class RawInputService : IDisposable
    {
        public const int HOTKEY_ID_EDIT = 9001;
        public const int HOTKEY_ID_RESET = 9002;
        public const int HOTKEY_ID_EDIT_ALT = 9003;

        private readonly MainViewModel _mainViewModel;
        private readonly Action _onToggleEditMode;
        private readonly Action _onResetStats;
        private readonly IntPtr _ownerHwnd;

        private HwndSource? _hwndSource;
        private IntPtr _hwnd;
        private bool _isDisposed;

        // Lookup table: index is VKey (0-255), value is array of KeyItemViewModel
        private readonly KeyItemViewModel[]?[] _lookupTable = new KeyItemViewModel[]?[256];

        // Track physical key state to ignore typematic repeat on MAKE
        private readonly bool[] _keyPhysicalDown = new bool[256];

        // Modifier tracking for custom shortcuts in Raw Input
        private bool _isCtrlDown;
        private bool _isShiftDown;
        private bool _isAltDown;

        // Parsed hotkey configurations
        private byte _editModeVKey = 0x79; // VK_F10
        private bool _editModeRequireCtrl = true;
        private bool _editModeRequireShift = true;
        private bool _editModeRequireAlt = false;

        private byte _resetStatsVKey = 0x52; // 'R'
        private bool _resetStatsRequireCtrl = true;
        private bool _resetStatsRequireShift = true;
        private bool _resetStatsRequireAlt = false;

        public RawInputService(
            MainViewModel mainViewModel,
            Action onToggleEditMode,
            Action onResetStats,
            IntPtr ownerHwnd = default)
        {
            _mainViewModel = mainViewModel;
            _onToggleEditMode = onToggleEditMode;
            _onResetStats = onResetStats;
            _ownerHwnd = ownerHwnd;

            ParseHotkeys(mainViewModel.Config);
            RebuildLookupTable();
        }

        public IntPtr WindowHandle => _hwnd;

        /// <summary>
        /// Registers for Win32 Raw Input and hooks window messages.
        /// If ownerHwnd was passed, hooks the owner window. Otherwise, creates a dedicated hidden HwndSource.
        /// </summary>
        public void Start()
        {
            if (_hwnd != IntPtr.Zero)
                return;

            if (_ownerHwnd != IntPtr.Zero)
            {
                _hwnd = _ownerHwnd;
                var source = HwndSource.FromHwnd(_ownerHwnd);
                source?.AddHook(HwndMessageHook);
            }
            else
            {
                // Fallback for headless testing or standalone operation
                var parameters = new HwndSourceParameters("KeysPerSecond_RawInputListener")
                {
                    WindowStyle = 0,
                    ExtendedWindowStyle = NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE,
                    Width = 0,
                    Height = 0,
                    PositionX = -32000,
                    PositionY = -32000,
                    ParentWindow = IntPtr.Zero
                };

                _hwndSource = new HwndSource(parameters);
                _hwnd = _hwndSource.Handle;
                _hwndSource.AddHook(HwndMessageHook);
            }

            // Register Raw Input Keyboard device with RIDEV_INPUTSINK
            RAWINPUTDEVICE rid = new RAWINPUTDEVICE
            {
                usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                usUsage = NativeMethods.HID_USAGE_GENERIC_KEYBOARD,
                dwFlags = NativeMethods.RIDEV_INPUTSINK,
                hwndTarget = _hwnd
            };

            bool success = NativeMethods.RegisterRawInputDevices(&rid, 1, (uint)sizeof(RAWINPUTDEVICE));
            if (!success)
            {
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Debug.WriteLine($"[RawInputService] RegisterRawInputDevices failed with error: {error}");
            }

            // Also register global hotkeys as secondary trigger
            RegisterGlobalHotkeys();
        }

        public void RebuildLookupTable()
        {
            Array.Clear(_lookupTable, 0, _lookupTable.Length);
            Array.Clear(_keyPhysicalDown, 0, _keyPhysicalDown.Length);

            var grouped = new Dictionary<byte, List<KeyItemViewModel>>();

            foreach (var keyVm in _mainViewModel.TrackedKeys)
            {
                byte vk = keyVm.VirtualKey;
                if (!grouped.TryGetValue(vk, out var list))
                {
                    list = new List<KeyItemViewModel>();
                    grouped[vk] = list;
                }
                list.Add(keyVm);
            }

            foreach (var kvp in grouped)
            {
                _lookupTable[kvp.Key] = kvp.Value.ToArray();
            }
        }

        public void UpdateHotkeys(AppConfig config)
        {
            UnregisterGlobalHotkeys();
            ParseHotkeys(config);
            RegisterGlobalHotkeys();
        }

        private void ParseHotkeys(AppConfig config)
        {
            ParseHotkeyString(config.ToggleEditModeKey, ref _editModeVKey, ref _editModeRequireCtrl, ref _editModeRequireShift, ref _editModeRequireAlt, 0x79);
            ParseHotkeyString(config.ResetStatsKey, ref _resetStatsVKey, ref _resetStatsRequireCtrl, ref _resetStatsRequireShift, ref _resetStatsRequireAlt, 0x52);
        }

        private static void ParseHotkeyString(
            string hotkeyStr,
            ref byte targetVk,
            ref bool reqCtrl,
            ref bool reqShift,
            ref bool reqAlt,
            byte fallbackVk)
        {
            if (string.IsNullOrWhiteSpace(hotkeyStr))
            {
                targetVk = fallbackVk;
                reqCtrl = true;
                reqShift = true;
                reqAlt = false;
                return;
            }

            reqCtrl = false;
            reqShift = false;
            reqAlt = false;
            targetVk = 0;

            string[] parts = hotkeyStr.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    reqCtrl = true;
                }
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    reqShift = true;
                }
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    reqAlt = true;
                }
                else
                {
                    targetVk = KeyToVkHelper.ResolveVirtualKey(part);
                }
            }

            if (targetVk == 0)
            {
                targetVk = fallbackVk;
            }
        }

        private static bool SafeRegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk)
        {
            if (hwnd == IntPtr.Zero || vk == 0)
                return false;

            // Try with MOD_NOREPEAT first
            bool ok = NativeMethods.RegisterHotKey(hwnd, id, modifiers | NativeMethods.MOD_NOREPEAT, vk);
            if (!ok)
            {
                // Retry without MOD_NOREPEAT (compatibility fallback for older/quirky drivers)
                ok = NativeMethods.RegisterHotKey(hwnd, id, modifiers, vk);
            }
            return ok;
        }

        private void RegisterGlobalHotkeys()
        {
            if (_hwnd == IntPtr.Zero) return;

            // 1. Primary Edit Mode Hotkey (e.g. Ctrl + Shift + F10 or user configured)
            uint editMods = 0;
            if (_editModeRequireCtrl) editMods |= NativeMethods.MOD_CONTROL;
            if (_editModeRequireShift) editMods |= NativeMethods.MOD_SHIFT;
            if (_editModeRequireAlt) editMods |= NativeMethods.MOD_ALT;
            SafeRegisterHotKey(_hwnd, HOTKEY_ID_EDIT, editMods, _editModeVKey);

            // 2. Secondary Ergonomic Edit Mode Hotkey: Ctrl + Shift + E (0x45)
            // Ensures users on 60%/65% or laptop keyboards where F10 requires an Fn key can always toggle edit mode
            if (_editModeVKey != 0x45)
            {
                SafeRegisterHotKey(_hwnd, HOTKEY_ID_EDIT_ALT, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x45);
            }

            // 3. Reset Stats Hotkey (e.g. Ctrl + Shift + R)
            uint resetMods = 0;
            if (_resetStatsRequireCtrl) resetMods |= NativeMethods.MOD_CONTROL;
            if (_resetStatsRequireShift) resetMods |= NativeMethods.MOD_SHIFT;
            if (_resetStatsRequireAlt) resetMods |= NativeMethods.MOD_ALT;
            SafeRegisterHotKey(_hwnd, HOTKEY_ID_RESET, resetMods, _resetStatsVKey);
        }

        private void UnregisterGlobalHotkeys()
        {
            if (_hwnd == IntPtr.Zero) return;
            NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_EDIT);
            NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_EDIT_ALT);
            NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_RESET);
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_INPUT)
            {
                ProcessRawInput(lParam);
                handled = true;
                return IntPtr.Zero;
            }
            else if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_EDIT || id == HOTKEY_ID_EDIT_ALT)
                {
                    _onToggleEditMode.Invoke();
                    handled = true;
                    return IntPtr.Zero;
                }
                else if (id == HOTKEY_ID_RESET)
                {
                    _onResetStats.Invoke();
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Zero-allocation Raw Input processor in the hot path.
        /// </summary>
        private void ProcessRawInput(IntPtr hRawInput)
        {
            // Stackalloc buffer for RAWINPUT. Keyboard RAWINPUT is 40 bytes on x64, 32 bytes on x86.
            Span<byte> buffer = stackalloc byte[128];
            uint size = (uint)buffer.Length;

            fixed (byte* pBuffer = buffer)
            {
                uint bytesRead = NativeMethods.GetRawInputData(
                    hRawInput,
                    NativeMethods.RID_INPUT,
                    pBuffer,
                    &size,
                    (uint)sizeof(RAWINPUTHEADER));

                if (bytesRead == uint.MaxValue || bytesRead == 0)
                    return;

                RAWINPUTKEYBOARD* raw = (RAWINPUTKEYBOARD*)pBuffer;
                if (raw->Header.dwType != NativeMethods.RIM_TYPEKEYBOARD)
                    return;

                ushort rawVk = raw->Keyboard.VKey;
                ushort makeCode = raw->Keyboard.MakeCode;

                // In raw input, F10 with modifiers may arrive with VKey = 0 but MakeCode = 0x44 (68)
                if (rawVk == 0 && makeCode == 0x44)
                {
                    rawVk = 0x79; // VK_F10
                }

                if (rawVk == 0 || rawVk > 255)
                    return;

                byte vkey = (byte)rawVk;
                ushort flags = raw->Keyboard.Flags;
                bool isUp = (flags & NativeMethods.RI_KEY_BREAK) != 0;
                bool isDown = !isUp;

                // Update internal modifier tracking
                UpdateModifiers(vkey, flags, isDown);

                // Check for Raw Input hotkey triggers
                if (CheckHotkeys(vkey, makeCode, isDown))
                    return;

                // Dispatch to tracked keys lookup table
                DispatchKey(vkey, flags, isDown);
            }
        }

        private static bool IsAsyncKeyDown(int vKey)
        {
            return (NativeMethods.GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        private bool CheckHotkeys(byte vkey, ushort makeCode, bool isDown)
        {
            if (!isDown) return false;

            // Direct hardware-level state check via GetAsyncKeyState as fallback
            // This handles cases where modifiers were pressed before window creation or missed by buffer
            bool isCtrlHeld = _isCtrlDown || IsAsyncKeyDown(0x11);
            bool isShiftHeld = _isShiftDown || IsAsyncKeyDown(0x10);
            bool isAltHeld = _isAltDown || IsAsyncKeyDown(0x12);

            // Primary Edit Mode Check (Configured key or F10 scan code 0x44)
            bool isEditKey = (vkey == _editModeVKey) || (_editModeVKey == 0x79 && makeCode == 0x44);
            if (isEditKey &&
                (!_editModeRequireCtrl || isCtrlHeld) &&
                (!_editModeRequireShift || isShiftHeld) &&
                (!_editModeRequireAlt || isAltHeld))
            {
                _onToggleEditMode.Invoke();
                return true;
            }

            // Secondary Ergonomic Edit Mode Check: Ctrl + Shift + E ('E' = 0x45, makeCode = 0x12)
            if ((vkey == 0x45 || makeCode == 0x12) && isCtrlHeld && isShiftHeld)
            {
                _onToggleEditMode.Invoke();
                return true;
            }

            // Reset Stats Check
            if (vkey == _resetStatsVKey &&
                (!_resetStatsRequireCtrl || isCtrlHeld) &&
                (!_resetStatsRequireShift || isShiftHeld) &&
                (!_resetStatsRequireAlt || isAltHeld))
            {
                _onResetStats.Invoke();
                return true;
            }

            return false;
        }

        private void UpdateModifiers(byte vkey, ushort flags, bool isDown)
        {
            if (vkey == 0x11 || vkey == 0xA2 || vkey == 0xA3) // VK_CONTROL, VK_LCONTROL, VK_RCONTROL
            {
                _isCtrlDown = isDown;
            }
            else if (vkey == 0x10 || vkey == 0xA0 || vkey == 0xA1) // VK_SHIFT, VK_LSHIFT, VK_RSHIFT
            {
                _isShiftDown = isDown;
            }
            else if (vkey == 0x12 || vkey == 0xA4 || vkey == 0xA5) // VK_MENU, VK_LMENU, VK_RMENU
            {
                _isAltDown = isDown;
            }
        }

        internal void SimulateRawKeyEvent(byte vkey, bool isDown, ushort flags = 0, ushort makeCode = 0)
        {
            UpdateModifiers(vkey, flags, isDown);
            if (CheckHotkeys(vkey, makeCode, isDown))
                return;
            DispatchKey(vkey, flags, isDown);
        }

        internal void DispatchKey(byte vkey, ushort flags, bool isDown)
        {
            // Primary lookup
            KeyItemViewModel[]? items = _lookupTable[vkey];

            // Also check generic vs specific Shift/Ctrl/Alt bindings
            if (items == null)
            {
                if (vkey == 0x10) // VK_SHIFT
                {
                    // Check if user bound LShift (0xA0) or RShift (0xA1)
                    byte specific = (flags & NativeMethods.RI_KEY_E0) != 0 ? (byte)0xA1 : (byte)0xA0;
                    items = _lookupTable[specific];
                }
                else if (vkey == 0x11) // VK_CONTROL
                {
                    byte specific = (flags & NativeMethods.RI_KEY_E0) != 0 ? (byte)0xA3 : (byte)0xA2;
                    items = _lookupTable[specific];
                }
                else if (vkey == 0x12) // VK_MENU
                {
                    byte specific = (flags & NativeMethods.RI_KEY_E0) != 0 ? (byte)0xA5 : (byte)0xA4;
                    items = _lookupTable[specific];
                }
            }

            if (items == null)
                return;

            if (isDown)
            {
                // Ignore typematic auto-repeats; only trigger on initial UP -> DOWN edge
                if (!_keyPhysicalDown[vkey])
                {
                    _keyPhysicalDown[vkey] = true;

                    long timestamp = Stopwatch.GetTimestamp();
                    _mainViewModel.OnKeyStrokeDown(timestamp);

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i].OnKeyDown(timestamp);
                    }
                }
            }
            else
            {
                if (_keyPhysicalDown[vkey])
                {
                    _keyPhysicalDown[vkey] = false;
                    long timestamp = Stopwatch.GetTimestamp();

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i].OnKeyUp(timestamp);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            UnregisterGlobalHotkeys();

            if (_hwnd != IntPtr.Zero)
            {
                // Unregister Raw Input
                RAWINPUTDEVICE rid = new RAWINPUTDEVICE
                {
                    usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    usUsage = NativeMethods.HID_USAGE_GENERIC_KEYBOARD,
                    dwFlags = NativeMethods.RIDEV_REMOVE,
                    hwndTarget = IntPtr.Zero
                };
                NativeMethods.RegisterRawInputDevices(&rid, 1, (uint)sizeof(RAWINPUTDEVICE));
            }

            if (_ownerHwnd != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(_ownerHwnd);
                source?.RemoveHook(HwndMessageHook);
            }

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(HwndMessageHook);
                _hwndSource.Dispose();
                _hwndSource = null;
            }

            _hwnd = IntPtr.Zero;
        }
    }
}

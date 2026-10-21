using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;

namespace KeysPerSecond.Services
{
    public static class KeyToVkHelper
    {
        private static readonly Dictionary<string, byte> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Backspace", 0x08 },
            { "Tab", 0x09 },
            { "Enter", 0x0D },
            { "Return", 0x0D },
            { "Shift", 0x10 },
            { "Ctrl", 0x11 },
            { "Control", 0x11 },
            { "Alt", 0x12 },
            { "Menu", 0x12 },
            { "Pause", 0x13 },
            { "CapsLock", 0x14 },
            { "Escape", 0x1B },
            { "Esc", 0x1B },
            { "Space", 0x20 },
            { "Spacebar", 0x20 },
            { "PageUp", 0x21 },
            { "PageDown", 0x22 },
            { "End", 0x23 },
            { "Home", 0x24 },
            { "Left", 0x25 },
            { "Up", 0x26 },
            { "Right", 0x27 },
            { "Down", 0x28 },
            { "Insert", 0x2D },
            { "Delete", 0x2E },
            { "LShift", 0xA0 },
            { "LeftShift", 0xA0 },
            { "RShift", 0xA1 },
            { "RightShift", 0xA1 },
            { "LCtrl", 0xA2 },
            { "LeftCtrl", 0xA2 },
            { "LeftControl", 0xA2 },
            { "RCtrl", 0xA3 },
            { "RightCtrl", 0xA3 },
            { "RightControl", 0xA3 },
            { "LAlt", 0xA4 },
            { "LeftAlt", 0xA4 },
            { "RAlt", 0xA5 },
            { "RightAlt", 0xA5 },
            { "Semicolon", 0xBA },
            { ";", 0xBA },
            { "Plus", 0xBB },
            { "=", 0xBB },
            { "Comma", 0xBC },
            { ",", 0xBC },
            { "Minus", 0xBD },
            { "-", 0xBD },
            { "Period", 0xBE },
            { ".", 0xBE },
            { "Slash", 0xBF },
            { "/", 0xBF },
            { "Tilde", 0xC0 },
            { "`", 0xC0 },
            { "~", 0xC0 },
            { "OpenBracket", 0xDB },
            { "[", 0xDB },
            { "Backslash", 0xDC },
            { "\\", 0xDC },
            { "CloseBracket", 0xDD },
            { "]", 0xDD },
            { "Quote", 0xDE },
            { "'", 0xDE },
        };

        /// <summary>
        /// Resolves a Virtual Key code from a string representation
        /// (e.g. "Z", "Space", "F10", "0x5A", or "90").
        /// </summary>
        public static byte ResolveVirtualKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return 0;

            string trimmed = keyString.Trim();

            // 1. Try Hex number (e.g. "0x5A")
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (byte.TryParse(trimmed.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexVal))
                    return hexVal;
            }

            // 2. Single character (A-Z, 0-9, or single punctuation)
            if (trimmed.Length == 1)
            {
                char c = char.ToUpperInvariant(trimmed[0]);
                if (c >= 'A' && c <= 'Z')
                    return (byte)c;
                if (c >= '0' && c <= '9')
                    return (byte)c;
                if (NamedKeys.TryGetValue(trimmed, out byte singlePunctVk))
                    return singlePunctVk;
            }

            // 3. Try Decimal number for VK codes with 2 or more digits (e.g. "90", "112")
            if (trimmed.Length > 1 && byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte decVal))
            {
                return decVal;
            }

            // 4. Function keys (F1 - F24)
            if (trimmed.Length >= 2 && (trimmed[0] == 'F' || trimmed[0] == 'f'))
            {
                if (int.TryParse(trimmed.Substring(1), out int fNum) && fNum >= 1 && fNum <= 24)
                {
                    return (byte)(0x70 + (fNum - 1)); // VK_F1 is 0x70
                }
            }

            // 5. Numpad keys (NumPad0 - NumPad9)
            if (trimmed.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Num", StringComparison.OrdinalIgnoreCase))
            {
                string numPart = trimmed.Substring(trimmed.Length - 1);
                if (int.TryParse(numPart, out int numVal) && numVal >= 0 && numVal <= 9)
                {
                    return (byte)(0x60 + numVal); // VK_NUMPAD0 is 0x60
                }
            }

            // 6. Named keys dictionary
            if (NamedKeys.TryGetValue(trimmed, out byte vk))
            {
                return vk;
            }

            // 7. Fallback to WPF KeyInterop if possible
            if (Enum.TryParse<Key>(trimmed, true, out Key wpfKey))
            {
                int wpfVk = KeyInterop.VirtualKeyFromKey(wpfKey);
                if (wpfVk > 0 && wpfVk <= 255)
                    return (byte)wpfVk;
            }

            return 0;
        }

        /// <summary>
        /// Converts a Virtual Key code to a friendly display string if none provided.
        /// </summary>
        public static string GetDefaultLabel(byte vk)
        {
            if (vk >= 'A' && vk <= 'Z')
                return ((char)vk).ToString();
            if (vk >= '0' && vk <= '9')
                return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x87)
                return $"F{vk - 0x70 + 1}";
            if (vk >= 0x60 && vk <= 0x69)
                return $"Num{vk - 0x60}";

            return vk switch
            {
                0x20 => "SPC",
                0x10 => "SHF",
                0xA0 => "LSHF",
                0xA1 => "RSHF",
                0x11 => "CTRL",
                0xA2 => "LCTL",
                0xA3 => "RCTL",
                0x12 => "ALT",
                0xA4 => "LALT",
                0xA5 => "RALT",
                0x0D => "RET",
                0x08 => "BSP",
                0x09 => "TAB",
                0x1B => "ESC",
                0x25 => "LEFT",
                0x26 => "UP",
                0x27 => "RGHT",
                0x28 => "DOWN",
                0xBA => ";",
                0xBB => "=",
                0xBC => ",",
                0xBD => "-",
                0xBE => ".",
                0xBF => "/",
                0xC0 => "`",
                0xDB => "[",
                0xDC => "\\",
                0xDD => "]",
                0xDE => "'",
                _ => $"0x{vk:X2}"
            };
        }
    }
}

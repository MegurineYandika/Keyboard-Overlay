using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KeysPerSecond.Models;
using KeysPerSecond.Services;

namespace KeysPerSecond.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly AppConfig _originalConfig;

        // Keys Collection
        public ObservableCollection<KeyBindingConfig> Keys { get; }

        private KeyBindingConfig? _selectedKey;
        public KeyBindingConfig? SelectedKey
        {
            get => _selectedKey;
            set => SetProperty(ref _selectedKey, value);
        }

        // New Key Input state
        private string _inputKey = "Space";
        private string _inputLabel = "SPC";
        private byte _inputVirtualKey = 0x20;
        private string _inputAccentColor = "#00E5FF";
        private bool _isCapturingKey;
        private string _captureStatus = "Click 'Detect Key' and press any key";

        public string InputKey
        {
            get => _inputKey;
            set
            {
                if (SetProperty(ref _inputKey, value))
                {
                    byte resolved = KeyToVkHelper.ResolveVirtualKey(value);
                    if (resolved != 0)
                    {
                        InputVirtualKey = resolved;
                        if (string.IsNullOrWhiteSpace(_inputLabel) || _inputLabel == KeyToVkHelper.GetDefaultLabel(_inputVirtualKey))
                        {
                            InputLabel = KeyToVkHelper.GetDefaultLabel(resolved);
                        }
                    }
                }
            }
        }

        public string InputLabel
        {
            get => _inputLabel;
            set => SetProperty(ref _inputLabel, value);
        }

        public byte InputVirtualKey
        {
            get => _inputVirtualKey;
            set => SetProperty(ref _inputVirtualKey, value);
        }

        public string InputAccentColor
        {
            get => _inputAccentColor;
            set => SetProperty(ref _inputAccentColor, value);
        }

        public bool IsCapturingKey
        {
            get => _isCapturingKey;
            set
            {
                if (SetProperty(ref _isCapturingKey, value))
                {
                    CaptureStatus = value ? "PRESS ANY KEY NOW..." : "Click 'Detect Key' and press any key";
                }
            }
        }

        public string CaptureStatus
        {
            get => _captureStatus;
            set => SetProperty(ref _captureStatus, value);
        }

        // Appearance & Style
        private string _accentColor;
        private string _pressedColor;
        private double _backgroundOpacity;
        private double _keyWidth;
        private double _keyHeight;
        private double _keySpacing;
        private double _cornerRadius;
        private string _orientation;
        private bool _showKpsCounter;
        private bool _showMaxKps;
        private bool _showTotalCounter;
        private bool _showIndividualCounters;

        public string AccentColor
        {
            get => _accentColor;
            set => SetProperty(ref _accentColor, value);
        }

        public string PressedColor
        {
            get => _pressedColor;
            set => SetProperty(ref _pressedColor, value);
        }

        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set => SetProperty(ref _backgroundOpacity, Math.Round(value, 2));
        }

        public double KeyWidth
        {
            get => _keyWidth;
            set => SetProperty(ref _keyWidth, Math.Round(value, 0));
        }

        public double KeyHeight
        {
            get => _keyHeight;
            set => SetProperty(ref _keyHeight, Math.Round(value, 0));
        }

        public double KeySpacing
        {
            get => _keySpacing;
            set => SetProperty(ref _keySpacing, Math.Round(value, 0));
        }

        public double CornerRadius
        {
            get => _cornerRadius;
            set => SetProperty(ref _cornerRadius, Math.Round(value, 0));
        }

        public string Orientation
        {
            get => _orientation;
            set => SetProperty(ref _orientation, value);
        }

        public bool IsHorizontal
        {
            get => Orientation.Equals("Horizontal", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                {
                    Orientation = "Horizontal";
                    OnPropertyChanged(nameof(IsHorizontal));
                    OnPropertyChanged(nameof(IsVertical));
                }
            }
        }

        public bool IsVertical
        {
            get => Orientation.Equals("Vertical", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                {
                    Orientation = "Vertical";
                    OnPropertyChanged(nameof(IsHorizontal));
                    OnPropertyChanged(nameof(IsVertical));
                }
            }
        }

        public bool ShowKpsCounter
        {
            get => _showKpsCounter;
            set => SetProperty(ref _showKpsCounter, value);
        }

        public bool ShowMaxKps
        {
            get => _showMaxKps;
            set => SetProperty(ref _showMaxKps, value);
        }

        public bool ShowTotalCounter
        {
            get => _showTotalCounter;
            set => SetProperty(ref _showTotalCounter, value);
        }

        public bool ShowIndividualCounters
        {
            get => _showIndividualCounters;
            set => SetProperty(ref _showIndividualCounters, value);
        }

        // Hotkeys
        private string _toggleEditModeKey;
        private string _resetStatsKey;

        public string ToggleEditModeKey
        {
            get => _toggleEditModeKey;
            set => SetProperty(ref _toggleEditModeKey, value);
        }

        public string ResetStatsKey
        {
            get => _resetStatsKey;
            set => SetProperty(ref _resetStatsKey, value);
        }

        public SettingsViewModel(AppConfig config)
        {
            _originalConfig = config;

            Keys = new ObservableCollection<KeyBindingConfig>(
                config.TrackedKeys.Select(k => k.Clone()));

            _accentColor = config.Style.AccentColor;
            _pressedColor = config.Style.PressedColor;
            _backgroundOpacity = config.Style.BackgroundOpacity;
            _keyWidth = config.Style.KeyWidth;
            _keyHeight = config.Style.KeyHeight;
            _keySpacing = config.Style.KeySpacing;
            _cornerRadius = config.Style.CornerRadius;
            _orientation = config.Style.Orientation ?? "Horizontal";
            _showKpsCounter = config.Style.ShowKpsCounter;
            _showMaxKps = config.Style.ShowMaxKps;
            _showTotalCounter = config.Style.ShowTotalCounter;
            _showIndividualCounters = config.Style.ShowIndividualCounters;

            _toggleEditModeKey = config.ToggleEditModeKey;
            _resetStatsKey = config.ResetStatsKey;

            _inputAccentColor = config.Style.AccentColor;
        }

        public void SetCapturedKey(byte vk, string keyName)
        {
            InputVirtualKey = vk;
            InputKey = keyName;
            InputLabel = KeyToVkHelper.GetDefaultLabel(vk);
            IsCapturingKey = false;
        }

        public bool AddCurrentKey()
        {
            if (string.IsNullOrWhiteSpace(InputKey))
                return false;

            byte vk = InputVirtualKey;
            if (vk == 0)
            {
                vk = KeyToVkHelper.ResolveVirtualKey(InputKey);
            }

            if (vk == 0)
                return false;

            string label = string.IsNullOrWhiteSpace(InputLabel)
                ? KeyToVkHelper.GetDefaultLabel(vk)
                : InputLabel.Trim();

            var newKey = new KeyBindingConfig
            {
                Key = InputKey.Trim(),
                Label = label,
                VirtualKey = vk,
                AccentColor = !string.IsNullOrWhiteSpace(InputAccentColor) ? InputAccentColor : AccentColor
            };

            Keys.Add(newKey);
            SelectedKey = newKey;
            return true;
        }

        public void DeleteKey(KeyBindingConfig? key)
        {
            if (key == null) return;
            Keys.Remove(key);
            if (SelectedKey == key)
            {
                SelectedKey = Keys.LastOrDefault();
            }
        }

        public void MoveKeyUp(KeyBindingConfig? key)
        {
            if (key == null) return;
            int index = Keys.IndexOf(key);
            if (index > 0)
            {
                Keys.Move(index, index - 1);
                SelectedKey = key;
            }
        }

        public void MoveKeyDown(KeyBindingConfig? key)
        {
            if (key == null) return;
            int index = Keys.IndexOf(key);
            if (index >= 0 && index < Keys.Count - 1)
            {
                Keys.Move(index, index + 1);
                SelectedKey = key;
            }
        }

        public void ApplyPreset(string presetId)
        {
            Keys.Clear();

            switch (presetId.ToLowerInvariant())
            {
                case "osu_2k":
                    Keys.Add(new KeyBindingConfig { Key = "Z", Label = "Z", VirtualKey = 90, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "X", Label = "X", VirtualKey = 88, AccentColor = "#FF007F" });
                    break;

                case "osu_4k":
                    Keys.Add(new KeyBindingConfig { Key = "D", Label = "D", VirtualKey = 68, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "F", Label = "F", VirtualKey = 70, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "J", Label = "J", VirtualKey = 74, AccentColor = "#FF007F" });
                    Keys.Add(new KeyBindingConfig { Key = "K", Label = "K", VirtualKey = 75, AccentColor = "#FF007F" });
                    break;

                case "osu_7k":
                    Keys.Add(new KeyBindingConfig { Key = "S", Label = "S", VirtualKey = 83, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "D", Label = "D", VirtualKey = 68, AccentColor = "#38BDF8" });
                    Keys.Add(new KeyBindingConfig { Key = "F", Label = "F", VirtualKey = 70, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "Space", Label = "SPC", VirtualKey = 0x20, AccentColor = "#FACC15", Width = 84 });
                    Keys.Add(new KeyBindingConfig { Key = "J", Label = "J", VirtualKey = 74, AccentColor = "#FF007F" });
                    Keys.Add(new KeyBindingConfig { Key = "K", Label = "K", VirtualKey = 75, AccentColor = "#F43F5E" });
                    Keys.Add(new KeyBindingConfig { Key = "L", Label = "L", VirtualKey = 76, AccentColor = "#FF007F" });
                    break;

                case "wasd":
                    Keys.Add(new KeyBindingConfig { Key = "W", Label = "W", VirtualKey = 87, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "A", Label = "A", VirtualKey = 65, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "S", Label = "S", VirtualKey = 83, AccentColor = "#FF007F" });
                    Keys.Add(new KeyBindingConfig { Key = "D", Label = "D", VirtualKey = 68, AccentColor = "#FF007F" });
                    break;

                case "arrows":
                    Keys.Add(new KeyBindingConfig { Key = "Left", Label = "←", VirtualKey = 0x25, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "Down", Label = "↓", VirtualKey = 0x28, AccentColor = "#FF007F" });
                    Keys.Add(new KeyBindingConfig { Key = "Up", Label = "↑", VirtualKey = 0x26, AccentColor = "#00E5FF" });
                    Keys.Add(new KeyBindingConfig { Key = "Right", Label = "→", VirtualKey = 0x27, AccentColor = "#FF007F" });
                    break;
            }
        }

        /// <summary>
        /// Builds the updated AppConfig object containing all changes from the UI.
        /// </summary>
        public AppConfig BuildResultConfig()
        {
            var result = _originalConfig.Clone();

            result.TrackedKeys = Keys.Select(k => k.Clone()).ToList();

            result.Style.AccentColor = AccentColor;
            result.Style.PressedColor = PressedColor;
            result.Style.BackgroundOpacity = BackgroundOpacity;
            result.Style.KeyWidth = KeyWidth;
            result.Style.KeyHeight = KeyHeight;
            result.Style.KeySpacing = KeySpacing;
            result.Style.CornerRadius = CornerRadius;
            result.Style.Orientation = Orientation;
            result.Style.ShowKpsCounter = ShowKpsCounter;
            result.Style.ShowMaxKps = ShowMaxKps;
            result.Style.ShowTotalCounter = ShowTotalCounter;
            result.Style.ShowIndividualCounters = ShowIndividualCounters;

            result.ToggleEditModeKey = ToggleEditModeKey;
            result.ResetStatsKey = ResetStatsKey;

            return result;
        }
    }
}

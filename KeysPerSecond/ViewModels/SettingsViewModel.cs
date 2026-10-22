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

        // Window Dimensions & Scaling
        private double _windowScale;
        private double _customWidth;
        private double _customHeight;
        private bool _autoFit;

        public double WindowScale
        {
            get => _windowScale;
            set => SetProperty(ref _windowScale, Math.Round(value, 2));
        }

        public double CustomWidth
        {
            get => _customWidth;
            set => SetProperty(ref _customWidth, Math.Round(value, 0));
        }

        public double CustomHeight
        {
            get => _customHeight;
            set => SetProperty(ref _customHeight, Math.Round(value, 0));
        }

        public bool AutoFit
        {
            get => _autoFit;
            set => SetProperty(ref _autoFit, value);
        }

        public bool IsCustomSize
        {
            get => !_autoFit;
            set => AutoFit = !value;
        }

        public void SetScale(double scale)
        {
            WindowScale = Math.Clamp(scale, 0.4, 3.5);
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
            set
            {
                if (SetProperty(ref _keyWidth, Math.Round(value, 0)))
                {
                    OnPropertyChanged(nameof(CurrentKeysWidth));
                    OnPropertyChanged(nameof(CalculatedPatternHeight));
                    if (_matchPatternRatio)
                    {
                        WaterfallHeight = CalculatedPatternHeight;
                    }
                }
            }
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

        // Hold Duration & Hold Bar settings
        private bool _showHoldDuration;
        private bool _showHoldBar;
        private double _maxHoldDurationMs;
        private double _holdBarDecayMs;

        public bool ShowHoldDuration
        {
            get => _showHoldDuration;
            set => SetProperty(ref _showHoldDuration, value);
        }

        public bool ShowHoldBar
        {
            get => _showHoldBar;
            set => SetProperty(ref _showHoldBar, value);
        }

        public double MaxHoldDurationMs
        {
            get => _maxHoldDurationMs;
            set => SetProperty(ref _maxHoldDurationMs, Math.Round(value, 0));
        }

        public double HoldBarDecayMs
        {
            get => _holdBarDecayMs;
            set => SetProperty(ref _holdBarDecayMs, Math.Round(value, 0));
        }

        // Hotkeys
        private string _toggleEditModeKey;
        private string _resetStatsKey;

        // Waterfall / Piano Roll Bar Visualizer settings
        private bool _waterfallEnabled;
        private string _waterfallDirection = "Upscroll";
        private double _waterfallSpeed = 0.8;
        private double _waterfallMaxVisibleMs = 1500.0;
        private double _waterfallOpacity = 0.75;
        private double _waterfallCornerRadius = 4.0;
        private bool _waterfallUseKeyAccentColors = true;
        private double _waterfallHeight = 200.0;

        public bool WaterfallEnabled
        {
            get => _waterfallEnabled;
            set => SetProperty(ref _waterfallEnabled, value);
        }

        public string WaterfallDirection
        {
            get => _waterfallDirection;
            set
            {
                if (SetProperty(ref _waterfallDirection, value))
                {
                    OnPropertyChanged(nameof(IsUpscroll));
                    OnPropertyChanged(nameof(IsDownscroll));
                }
            }
        }

        public bool IsUpscroll
        {
            get => !string.Equals(WaterfallDirection, "Downscroll", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                {
                    WaterfallDirection = "Upscroll";
                }
            }
        }

        public bool IsDownscroll
        {
            get => string.Equals(WaterfallDirection, "Downscroll", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                {
                    WaterfallDirection = "Downscroll";
                }
            }
        }

        public double WaterfallSpeed
        {
            get => _waterfallSpeed;
            set => SetProperty(ref _waterfallSpeed, Math.Round(value, 2));
        }

        public double WaterfallMaxVisibleMs
        {
            get => _waterfallMaxVisibleMs;
            set => SetProperty(ref _waterfallMaxVisibleMs, Math.Round(value, 0));
        }

        public double WaterfallOpacity
        {
            get => _waterfallOpacity;
            set => SetProperty(ref _waterfallOpacity, Math.Round(value, 2));
        }

        public double WaterfallCornerRadius
        {
            get => _waterfallCornerRadius;
            set => SetProperty(ref _waterfallCornerRadius, Math.Round(value, 0));
        }

        public bool WaterfallUseKeyAccentColors
        {
            get => _waterfallUseKeyAccentColors;
            set => SetProperty(ref _waterfallUseKeyAccentColors, value);
        }

        public double WaterfallHeight
        {
            get => _waterfallHeight;
            set => SetProperty(ref _waterfallHeight, Math.Round(value, 0));
        }

        private bool _transparentMode;
        private bool _matchPatternRatio;

        public bool TransparentMode
        {
            get => _transparentMode;
            set => SetProperty(ref _transparentMode, value);
        }

        public bool MatchPatternRatio
        {
            get => _matchPatternRatio;
            set
            {
                if (SetProperty(ref _matchPatternRatio, value) && value)
                {
                    WaterfallHeight = CalculatedPatternHeight;
                }
            }
        }

        public double CurrentKeysWidth => CalculateTotalKeysWidth();

        public double CalculatedPatternHeight => Math.Round(CalculateTotalKeysWidth() * 2.0, 0);

        public double CalculateTotalKeysWidth()
        {
            double total = 0;
            if (Keys != null && Keys.Count > 0)
            {
                foreach (var k in Keys)
                {
                    double w = k.Width ?? KeyWidth;
                    total += w + 8.0;
                }
            }
            else
            {
                total = KeyWidth * 4.0 + 32.0;
            }
            return Math.Max(40.0, total);
        }

        public void ApplyPatternHeightPreset()
        {
            WaterfallHeight = CalculatedPatternHeight;
        }

        public void ApplyTransparentPatternPreset()
        {
            WaterfallEnabled = true;
            TransparentMode = true;
            WaterfallHeight = CalculatedPatternHeight;
        }

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

            _windowScale = config.Window.Scale > 0 ? config.Window.Scale : 1.0;
            _customWidth = config.Window.CustomWidth;
            _customHeight = config.Window.CustomHeight;
            _autoFit = config.Window.AutoFit;

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
            _showHoldDuration = config.Style.ShowHoldDuration;
            _showHoldBar = config.Style.ShowHoldBar;
            _maxHoldDurationMs = config.Style.MaxHoldDurationMs > 0 ? config.Style.MaxHoldDurationMs : 300.0;
            _holdBarDecayMs = config.Style.HoldBarDecayMs > 0 ? config.Style.HoldBarDecayMs : 600.0;

            _toggleEditModeKey = config.ToggleEditModeKey;
            _resetStatsKey = config.ResetStatsKey;

            var wf = config.Waterfall ?? new WaterfallConfig();
            _waterfallEnabled = wf.IsEnabled;
            _waterfallDirection = wf.ScrollDirection ?? "Upscroll";
            _waterfallSpeed = wf.ScrollSpeed > 0 ? wf.ScrollSpeed : 0.8;
            _waterfallMaxVisibleMs = wf.MaxVisibleDurationMs > 0 ? wf.MaxVisibleDurationMs : 1500.0;
            _waterfallOpacity = wf.BarOpacity > 0 ? wf.BarOpacity : 0.75;
            _waterfallCornerRadius = wf.BarCornerRadius >= 0 ? wf.BarCornerRadius : 4.0;
            _waterfallUseKeyAccentColors = wf.UseKeyAccentColors;
            _waterfallHeight = wf.Height > 0 ? wf.Height : 200.0;
            _transparentMode = wf.TransparentMode;
            _matchPatternRatio = wf.MatchPatternRatio;

            if (_matchPatternRatio)
            {
                _waterfallHeight = CalculatedPatternHeight;
            }

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
            OnKeysStructureChanged();
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
            OnKeysStructureChanged();
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

            OnKeysStructureChanged();
        }

        private void OnKeysStructureChanged()
        {
            OnPropertyChanged(nameof(CurrentKeysWidth));
            OnPropertyChanged(nameof(CalculatedPatternHeight));
            if (_matchPatternRatio)
            {
                WaterfallHeight = CalculatedPatternHeight;
            }
        }

        /// <summary>
        /// Builds the updated AppConfig object containing all changes from the UI.
        /// </summary>
        public AppConfig BuildResultConfig()
        {
            var result = _originalConfig.Clone();

            result.Window.Scale = WindowScale;
            result.Window.CustomWidth = CustomWidth;
            result.Window.CustomHeight = CustomHeight;
            result.Window.AutoFit = AutoFit;

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
            result.Style.ShowHoldDuration = ShowHoldDuration;
            result.Style.ShowHoldBar = ShowHoldBar;
            result.Style.MaxHoldDurationMs = MaxHoldDurationMs;
            result.Style.HoldBarDecayMs = HoldBarDecayMs;

            result.ToggleEditModeKey = ToggleEditModeKey;
            result.ResetStatsKey = ResetStatsKey;

            result.Waterfall.IsEnabled = WaterfallEnabled;
            result.Waterfall.ScrollDirection = WaterfallDirection;
            result.Waterfall.ScrollSpeed = WaterfallSpeed;
            result.Waterfall.MaxVisibleDurationMs = WaterfallMaxVisibleMs;
            result.Waterfall.BarOpacity = WaterfallOpacity;
            result.Waterfall.BarCornerRadius = WaterfallCornerRadius;
            result.Waterfall.UseKeyAccentColors = WaterfallUseKeyAccentColors;
            result.Waterfall.Height = WaterfallHeight;
            result.Waterfall.TransparentMode = TransparentMode;
            result.Waterfall.MatchPatternRatio = MatchPatternRatio;

            return result;
        }
    }
}


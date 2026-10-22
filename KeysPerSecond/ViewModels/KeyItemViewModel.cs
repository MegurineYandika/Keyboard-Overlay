using System;
using System.Diagnostics;
using System.Windows.Media;

namespace KeysPerSecond.ViewModels
{
    public class KeyItemViewModel : ObservableObject
    {
        private bool _isPressed;
        private long _totalPresses;
        private string _label = string.Empty;
        private string _accentColorHex = "#00E5FF";
        private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        private double _width = 64;
        private double _height = 76;

        // Keypress Hold Duration & Bar Animation
        private long _keyDownTimestamp;
        private long _keyReleaseTimestamp;
        private double _currentHoldMs;
        private double _lastHoldMs;
        private double _holdProgress;
        private double _holdBarWidth;
        private double _holdBarOpacity;
        private string _formattedDuration = "--";
        private int _lastDisplayedIntMs = -1;

        public KeyItemViewModel(byte virtualKey, string keyName, string label, string accentColorHex, double width = 64, double height = 76)
        {
            VirtualKey = virtualKey;
            KeyName = keyName;
            _label = label;
            _width = width;
            _height = height;
            AccentColorHex = accentColorHex;
        }

        public byte VirtualKey { get; }
        public string KeyName { get; }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public double Width
        {
            get => _width;
            set
            {
                if (SetProperty(ref _width, value))
                {
                    UpdateHoldBarWidth();
                }
            }
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public string AccentColorHex
        {
            get => _accentColorHex;
            set
            {
                if (SetProperty(ref _accentColorHex, value))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(value);
                        var brush = new SolidColorBrush(color);
                        brush.Freeze();
                        AccentBrush = brush;
                    }
                    catch
                    {
                        var brush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
                        brush.Freeze();
                        AccentBrush = brush;
                    }
                }
            }
        }

        public Brush AccentBrush
        {
            get => _accentBrush;
            private set => SetProperty(ref _accentBrush, value);
        }

        public bool IsPressed
        {
            get => _isPressed;
            set => SetProperty(ref _isPressed, value);
        }

        public long TotalPresses
        {
            get => _totalPresses;
            set => SetProperty(ref _totalPresses, value);
        }

        // Live Keypress Duration Metrics & Animation Properties

        public double CurrentHoldMs
        {
            get => _currentHoldMs;
            private set => SetProperty(ref _currentHoldMs, value);
        }

        public double LastHoldMs
        {
            get => _lastHoldMs;
            private set => SetProperty(ref _lastHoldMs, value);
        }

        public double HoldProgress
        {
            get => _holdProgress;
            private set
            {
                if (SetProperty(ref _holdProgress, value))
                {
                    UpdateHoldBarWidth();
                }
            }
        }

        public double HoldBarWidth
        {
            get => _holdBarWidth;
            private set => SetProperty(ref _holdBarWidth, value);
        }

        public double HoldBarOpacity
        {
            get => _holdBarOpacity;
            private set => SetProperty(ref _holdBarOpacity, value);
        }

        public string FormattedDuration
        {
            get => _formattedDuration;
            private set => SetProperty(ref _formattedDuration, value);
        }

        public long KeyDownTimestamp => _keyDownTimestamp;
        public long KeyReleaseTimestamp => _keyReleaseTimestamp;

        private void UpdateHoldBarWidth()
        {
            double trackWidth = Math.Max(10.0, _width - 16.0);
            HoldBarWidth = Math.Max(0.0, trackWidth * _holdProgress);
        }

        public void IncrementPresses()
        {
            TotalPresses++;
        }

        /// <summary>
        /// Called when the physical key goes down (initial stroke).
        /// </summary>
        public void OnKeyDown(long timestamp)
        {
            IsPressed = true;
            _keyDownTimestamp = timestamp;
            _currentHoldMs = 0;
            HoldProgress = 0;
            HoldBarOpacity = 1.0;
            FormattedDuration = "0ms";
            _lastDisplayedIntMs = 0;
            IncrementPresses();
        }

        /// <summary>
        /// Called when the physical key is released (key up).
        /// </summary>
        public void OnKeyUp(long timestamp)
        {
            IsPressed = false;
            _keyReleaseTimestamp = timestamp;

            if (_keyDownTimestamp > 0)
            {
                double elapsedMs = (timestamp - _keyDownTimestamp) * 1000.0 / Stopwatch.Frequency;
                LastHoldMs = Math.Max(1.0, Math.Round(elapsedMs, 0));
                CurrentHoldMs = LastHoldMs;
                FormattedDuration = $"{LastHoldMs:0}ms";
                _lastDisplayedIntMs = (int)LastHoldMs;
            }
        }

        /// <summary>
        /// Frame update for key hold duration and hold bar animation.
        /// Zero allocations in the animation loop.
        /// </summary>
        public void UpdateHoldAnimation(long currentTimestamp, double maxBenchmarkMs, double decayWindowMs)
        {
            if (maxBenchmarkMs <= 0) maxBenchmarkMs = 300.0;
            if (decayWindowMs <= 0) decayWindowMs = 600.0;

            if (IsPressed && _keyDownTimestamp > 0)
            {
                double elapsedMs = (currentTimestamp - _keyDownTimestamp) * 1000.0 / Stopwatch.Frequency;
                CurrentHoldMs = elapsedMs;

                double progress = Math.Clamp(elapsedMs / maxBenchmarkMs, 0.0, 1.0);
                HoldProgress = progress;
                HoldBarOpacity = 1.0;

                int intMs = (int)elapsedMs;
                if (intMs != _lastDisplayedIntMs)
                {
                    _lastDisplayedIntMs = intMs;
                    FormattedDuration = $"{intMs}ms";
                }
            }
            else if (_keyReleaseTimestamp > 0)
            {
                double sinceReleaseMs = (currentTimestamp - _keyReleaseTimestamp) * 1000.0 / Stopwatch.Frequency;
                if (sinceReleaseMs < decayWindowMs)
                {
                    double remaining = 1.0 - (sinceReleaseMs / decayWindowMs);
                    HoldBarOpacity = Math.Clamp(remaining, 0.0, 1.0);
                }
                else
                {
                    if (HoldBarOpacity > 0.001)
                    {
                        HoldBarOpacity = 0.0;
                        HoldProgress = 0.0;
                    }
                }
            }
        }

        public void Reset()
        {
            IsPressed = false;
            TotalPresses = 0;
            _keyDownTimestamp = 0;
            _keyReleaseTimestamp = 0;
            CurrentHoldMs = 0;
            LastHoldMs = 0;
            HoldProgress = 0;
            HoldBarOpacity = 0;
            FormattedDuration = "--";
            _lastDisplayedIntMs = -1;
        }
    }
}

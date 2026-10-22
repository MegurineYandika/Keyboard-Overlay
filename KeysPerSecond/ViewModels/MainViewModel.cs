using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using KeysPerSecond.Models;
using KeysPerSecond.Services;

namespace KeysPerSecond.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly KpsSlidingWindow _kpsWindow;
        private int _currentKps;
        private int _maxKps;
        private long _totalPresses;
        private bool _isEditMode;
        private AppConfig _config;

        public event Action? ConfigurationApplied;

        public MainViewModel(AppConfig config)
        {
            _config = config;
            _kpsWindow = new KpsSlidingWindow(2048);
            TrackedKeys = new ObservableCollection<KeyItemViewModel>();

            InitializeFromConfig(config);
        }

        public AppConfig Config
        {
            get => _config;
            set => SetProperty(ref _config, value);
        }

        public ObservableCollection<KeyItemViewModel> TrackedKeys { get; }

        public int CurrentKps
        {
            get => _currentKps;
            set => SetProperty(ref _currentKps, value);
        }

        public int MaxKps
        {
            get => _maxKps;
            set => SetProperty(ref _maxKps, value);
        }

        public long TotalPresses
        {
            get => _totalPresses;
            set => SetProperty(ref _totalPresses, value);
        }

        public double WindowScale
        {
            get => _config.Window.Scale;
            set
            {
                double clamped = Math.Clamp(Math.Round(value, 2), 0.4, 3.5);
                if (Math.Abs(_config.Window.Scale - clamped) > 0.001)
                {
                    _config.Window.Scale = clamped;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Config));
                }
            }
        }

        public double CustomWidth
        {
            get => _config.Window.CustomWidth;
            set
            {
                if (Math.Abs(_config.Window.CustomWidth - value) > 0.1)
                {
                    _config.Window.CustomWidth = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Config));
                }
            }
        }

        public double CustomHeight
        {
            get => _config.Window.CustomHeight;
            set
            {
                if (Math.Abs(_config.Window.CustomHeight - value) > 0.1)
                {
                    _config.Window.CustomHeight = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Config));
                }
            }
        }

        public bool AutoFit
        {
            get => _config.Window.AutoFit;
            set
            {
                if (_config.Window.AutoFit != value)
                {
                    _config.Window.AutoFit = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Config));
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public void InitializeFromConfig(AppConfig config)
        {
            _config = config;
            TrackedKeys.Clear();

            foreach (var k in config.TrackedKeys)
            {
                double width = k.Width ?? config.Style.KeyWidth;
                double height = k.Height ?? config.Style.KeyHeight;
                string accent = !string.IsNullOrEmpty(k.AccentColor) ? k.AccentColor : config.Style.AccentColor;

                var item = new KeyItemViewModel(
                    k.VirtualKey,
                    k.Key,
                    k.Label ?? KeyToVkHelper.GetDefaultLabel(k.VirtualKey),
                    accent,
                    width,
                    height);

                TrackedKeys.Add(item);
            }
        }

        /// <summary>
        /// Updates the entire active configuration and refreshes the overlay keys.
        /// </summary>
        public void UpdateConfig(AppConfig newConfig)
        {
            ConfigService.NormalizeConfig(newConfig);
            Config = newConfig;
            InitializeFromConfig(newConfig);
            ConfigurationApplied?.Invoke();
        }

        /// <summary>
        /// Adds a key binding dynamically to the tracked keys.
        /// </summary>
        public void AddTrackedKey(KeyBindingConfig keyConfig)
        {
            if (keyConfig.VirtualKey == 0)
            {
                keyConfig.VirtualKey = KeyToVkHelper.ResolveVirtualKey(keyConfig.Key);
            }
            if (string.IsNullOrWhiteSpace(keyConfig.Label))
            {
                keyConfig.Label = KeyToVkHelper.GetDefaultLabel(keyConfig.VirtualKey);
            }
            if (string.IsNullOrWhiteSpace(keyConfig.AccentColor))
            {
                keyConfig.AccentColor = _config.Style.AccentColor;
            }

            _config.TrackedKeys.Add(keyConfig);

            double width = keyConfig.Width ?? _config.Style.KeyWidth;
            double height = keyConfig.Height ?? _config.Style.KeyHeight;
            var item = new KeyItemViewModel(
                keyConfig.VirtualKey,
                keyConfig.Key,
                keyConfig.Label,
                keyConfig.AccentColor,
                width,
                height);

            TrackedKeys.Add(item);
            ConfigurationApplied?.Invoke();
        }

        /// <summary>
        /// Removes a key binding from tracking.
        /// </summary>
        public bool RemoveTrackedKey(KeyItemViewModel keyItem)
        {
            int index = TrackedKeys.IndexOf(keyItem);
            if (index >= 0)
            {
                TrackedKeys.RemoveAt(index);
                if (index < _config.TrackedKeys.Count)
                {
                    _config.TrackedKeys.RemoveAt(index);
                }
                ConfigurationApplied?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when a monitored key goes DOWN (initial stroke, not repeat).
        /// </summary>
        public void OnKeyStrokeDown(long timestamp)
        {
            TotalPresses++;
            _kpsWindow.RecordPress(timestamp);
        }

        /// <summary>
        /// Updates live KPS from the sliding window. Called on UI render tick.
        /// </summary>
        public void UpdateKps(long currentTimestamp)
        {
            int liveKps = _kpsWindow.UpdateAndGetCount(currentTimestamp, Stopwatch.Frequency);

            if (_currentKps != liveKps)
            {
                CurrentKps = liveKps;
            }

            if (liveKps > _maxKps)
            {
                MaxKps = liveKps;
            }
        }

        /// <summary>
        /// Updates the real-time hold durations and animated hold bars for all tracked keys.
        /// </summary>
        public void UpdateHoldAnimations(long currentTimestamp)
        {
            double maxBenchmark = _config.Style.MaxHoldDurationMs;
            double decayWindow = _config.Style.HoldBarDecayMs;

            for (int i = 0; i < TrackedKeys.Count; i++)
            {
                TrackedKeys[i].UpdateHoldAnimation(currentTimestamp, maxBenchmark, decayWindow);
            }
        }

        /// <summary>
        /// Resets all counters and KPS metrics.
        /// </summary>
        public void ResetStats()
        {
            _kpsWindow.Reset();
            CurrentKps = 0;
            MaxKps = 0;
            TotalPresses = 0;

            foreach (var key in TrackedKeys)
            {
                key.Reset();
            }
        }

        public void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }
    }
}

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

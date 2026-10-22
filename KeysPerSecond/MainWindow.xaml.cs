using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using KeysPerSecond.Models;
using KeysPerSecond.Native;
using KeysPerSecond.Services;
using KeysPerSecond.ViewModels;

namespace KeysPerSecond
{
    public partial class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private AppConfig _config;
        private readonly MainViewModel _viewModel;
        private RawInputService? _rawInputService;
        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();

            _configService = new ConfigService();
            _config = _configService.LoadConfig();

            // Set initial window position
            this.Left = _config.Window.Left;
            this.Top = _config.Window.Top;
            ApplyWindowDimensions(_config);

            _viewModel = new MainViewModel(_config);
            this.DataContext = _viewModel;
            _viewModel.ConfigurationApplied += OnConfigurationApplied;

            this.SourceInitialized += OnSourceInitialized;
            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
            this.LocationChanged += OnLocationChanged;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // Apply initial Win32 styles
            ApplyClickThroughStyles(!_viewModel.IsEditMode);

            // Hook up rendering loop for zero-allocation live KPS calculation
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            WaterfallControl.Configure(_config, _viewModel.WaterfallBuffer, _viewModel.TrackedKeys);
            ApplyWaterfallAlignment();

            if (_hwnd == IntPtr.Zero)
            {
                _hwnd = new WindowInteropHelper(this).Handle;
            }

            // Start the Raw Input service attached directly to MainWindow's HWND
            _rawInputService = new RawInputService(
                _viewModel,
                onToggleEditMode: () => Dispatcher.InvokeAsync(ToggleEditMode),
                onResetStats: () => Dispatcher.InvokeAsync(_viewModel.ResetStats),
                ownerHwnd: _hwnd);

            _rawInputService.Start();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            long now = Stopwatch.GetTimestamp();
            _viewModel.UpdateKps(now);
            _viewModel.UpdateHoldAnimations(now);

            if (_config.Waterfall.IsEnabled && !_viewModel.IsEditMode)
            {
                WaterfallControl.InvalidateVisual();
            }
        }

        /// <summary>
        /// Configures Win32 window styles:
        /// When clickThrough is true: WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE.
        /// When clickThrough is false (Edit Mode): removes WS_EX_TRANSPARENT so the window can be dragged and clicked.
        /// </summary>
        public void ApplyClickThroughStyles(bool clickThrough)
        {
            if (_hwnd == IntPtr.Zero)
                return;

            IntPtr currentExStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
            long style = currentExStyle.ToInt64();

            // Ensure LAYERED, TOOLWINDOW and NOACTIVATE
            style |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW;

            if (clickThrough)
            {
                style |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE;
            }
            else
            {
                style &= ~NativeMethods.WS_EX_TRANSPARENT;
                style &= ~NativeMethods.WS_EX_NOACTIVATE;
            }

            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
        }

        public void ToggleEditMode()
        {
            _viewModel.ToggleEditMode();
            WaterfallControl.SetEditMode(_viewModel.IsEditMode);
            ApplyClickThroughStyles(!_viewModel.IsEditMode);

            if (!_viewModel.IsEditMode)
            {
                // Locked back: save current position
                SaveCurrentWindowConfig();
            }
        }

        private void OnRootMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.IsEditMode && e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                    SaveCurrentWindowConfig();
                }
                catch (InvalidOperationException)
                {
                    // Ignore transient drag exceptions
                }
            }
        }

        private void OnRootMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_viewModel.IsEditMode)
            {
                double step = e.Delta > 0 ? 0.05 : -0.05;
                _viewModel.WindowScale = Math.Clamp(_viewModel.WindowScale + step, 0.4, 3.5);
                SaveCurrentWindowConfig();
            }
        }

        private void OnLocationChanged(object? sender, EventArgs e)
        {
            if (_viewModel.IsEditMode)
            {
                SaveCurrentWindowConfig();
            }
        }

        private void ApplyWindowDimensions(AppConfig config)
        {
            if (!config.Window.AutoFit && config.Window.CustomWidth > 60 && config.Window.CustomHeight > 60)
            {
                this.SizeToContent = SizeToContent.Manual;
                this.Width = config.Window.CustomWidth;
                this.Height = config.Window.CustomHeight;
            }
            else
            {
                this.SizeToContent = SizeToContent.WidthAndHeight;
            }
        }

        private void SaveCurrentWindowConfig()
        {
            _config.Window.Left = this.Left;
            _config.Window.Top = this.Top;
            _config.Window.Scale = _viewModel.WindowScale;
            _config.Window.IsLocked = !_viewModel.IsEditMode;

            if (!_config.Window.AutoFit && this.ActualWidth > 60 && this.ActualHeight > 60)
            {
                _config.Window.CustomWidth = this.ActualWidth;
                _config.Window.CustomHeight = this.ActualHeight;
            }

            _configService.SaveConfig(_config);
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow(_config, this);
            if (settingsWin.ShowDialog() == true && settingsWin.ResultConfig != null)
            {
                _config = settingsWin.ResultConfig;
                ApplyWindowDimensions(_config);
                _viewModel.UpdateConfig(_config);
                _rawInputService?.UpdateHotkeys(_config);
                _rawInputService?.RebuildLookupTable();
                WaterfallControl.Configure(_config, _viewModel.WaterfallBuffer, _viewModel.TrackedKeys);
                ApplyWaterfallAlignment();
                _configService.SaveConfig(_config);
            }
        }

        private void OnConfigurationApplied()
        {
            WaterfallControl.Configure(_config, _viewModel.WaterfallBuffer, _viewModel.TrackedKeys);
            ApplyWaterfallAlignment();
        }

        private void ApplyWaterfallAlignment()
        {
            bool isDownscroll = string.Equals(_config.Waterfall.ScrollDirection, "Downscroll", StringComparison.OrdinalIgnoreCase);
            var alignment = isDownscroll ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            WaterfallControl.VerticalAlignment = alignment;
            KeysItemsControl.VerticalAlignment = alignment;
        }

        private void OnExitAppClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnLockOverlayClick(object sender, RoutedEventArgs e)
        {
            ToggleEditMode();
        }

        private void OnResetStatsClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetStats();
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            _rawInputService?.Dispose();
            SaveCurrentWindowConfig();
        }
    }
}

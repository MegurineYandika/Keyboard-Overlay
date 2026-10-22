using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeysPerSecond.Models;
using KeysPerSecond.Services;
using KeysPerSecond.ViewModels;

namespace KeysPerSecond
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public AppConfig? ResultConfig { get; private set; }

        public SettingsWindow(AppConfig currentConfig, Window? owner = null)
        {
            InitializeComponent();

            if (owner != null)
            {
                this.Owner = owner;
            }

            _viewModel = new SettingsViewModel(currentConfig);
            this.DataContext = _viewModel;
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel.IsCapturingKey)
            {
                e.Handled = true;

                Key key = e.Key == Key.System ? e.SystemKey : e.Key;
                int vk = KeyInterop.VirtualKeyFromKey(key);

                if (vk > 0 && vk <= 255)
                {
                    string keyName = key.ToString();
                    _viewModel.SetCapturedKey((byte)vk, keyName);
                }
            }
        }

        private void OnDetectKeyClick(object sender, RoutedEventArgs e)
        {
            _viewModel.IsCapturingKey = true;
        }

        private void OnAddKeyClick(object sender, RoutedEventArgs e)
        {
            bool added = _viewModel.AddCurrentKey();
            if (!added)
            {
                MessageBox.Show(
                    "Please enter a valid key name or click 'Detect Key' and press a key.",
                    "Invalid Key",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void OnDeleteKeyClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is KeyBindingConfig key)
            {
                _viewModel.DeleteKey(key);
            }
        }

        private void OnMoveKeyUpClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is KeyBindingConfig key)
            {
                _viewModel.MoveKeyUp(key);
            }
        }

        private void OnMoveKeyDownClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is KeyBindingConfig key)
            {
                _viewModel.MoveKeyDown(key);
            }
        }

        private void OnPaletteColorClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _viewModel.InputAccentColor = hex;
            }
        }

        private void OnPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string presetId)
            {
                if (presetId == "mania_pattern")
                {
                    _viewModel.ApplyPreset("osu_4k");
                    _viewModel.ApplyTransparentPatternPreset();
                }
                else
                {
                    _viewModel.ApplyPreset(presetId);
                }
            }
        }

        private void OnApplyPatternPresetClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplyPatternHeightPreset();
        }

        private void OnApplyTransparentPatternComboClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplyTransparentPatternPreset();
        }

        private void OnScalePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr &&
                double.TryParse(tagStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double scale))
            {
                _viewModel.SetScale(scale);
            }
        }

        private void OnExitAppClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit KeysPerSecond?",
                "Exit Overlay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            ResultConfig = _viewModel.BuildResultConfig();
            this.DialogResult = true;
            this.Close();
        }
    }
}


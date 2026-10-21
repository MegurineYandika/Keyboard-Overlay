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
        private double _height = 64;

        public KeyItemViewModel(byte virtualKey, string keyName, string label, string accentColorHex, double width = 64, double height = 64)
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
            set => SetProperty(ref _width, value);
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
                        // Fallback to default cyan
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

        public void IncrementPresses()
        {
            TotalPresses++;
        }

        public void Reset()
        {
            IsPressed = false;
            TotalPresses = 0;
        }
    }
}

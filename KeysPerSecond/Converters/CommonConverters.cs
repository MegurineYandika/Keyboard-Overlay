using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace KeysPerSecond.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v != Visibility.Visible;
            }
            return false;
        }
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
                catch
                {
                    // Fallback
                }
            }
            return Brushes.Cyan;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && s.Equals("Vertical", StringComparison.OrdinalIgnoreCase))
            {
                return Orientation.Vertical;
            }
            return Orientation.Horizontal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Orientation o && o == Orientation.Vertical)
            {
                return "Vertical";
            }
            return "Horizontal";
        }
    }

    public class KpsToWidthMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3 &&
                values[0] is int currentKps &&
                values[1] is int maxKps &&
                values[2] is double containerWidth &&
                !double.IsNaN(containerWidth) && containerWidth > 0)
            {
                if (currentKps <= 0) return 0.0;
                double benchmark = Math.Max(maxKps, 15.0);
                double ratio = Math.Clamp((double)currentKps / benchmark, 0.0, 1.0);
                return containerWidth * ratio;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

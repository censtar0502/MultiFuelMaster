using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MultiFuelMaster.Converters
{
    public class HexToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return Brushes.Transparent;

            try
            {
                string hex = value.ToString() ?? "#000000";
                if (!hex.StartsWith("#"))
                    hex = "#" + hex;
                
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

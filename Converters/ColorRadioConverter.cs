using System;
using System.Globalization;
using System.Windows.Data;

namespace MultiFuelMaster.Converters
{
    public class ColorRadioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string colorValue = value.ToString() ?? "";
            string paramValue = parameter.ToString() ?? "";

            // Map common colors
            string normalizedColor = NormalizeColor(colorValue);
            string normalizedParam = NormalizeColor(paramValue);

            return normalizedColor == normalizedParam;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter != null)
            {
                string paramValue = parameter.ToString() ?? "";
                
                // Return the actual hex color based on selection
                return paramValue.ToLower() switch
                {
                    "green" => "#228B22",
                    "red" => "#FF0000",
                    "blue" => "#0000FF",
                    "yellow" => "#FFD700",
                    _ => "#808080" // other/gray
                };
            }
            return Binding.DoNothing;
        }

        private string NormalizeColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return "";

            color = color.ToLower().Trim();

            // Map hex to name
            if (color == "#228b22" || color == "#228B22" || color == "green" || color == "зеленый")
                return "green";
            if (color == "#ff0000" || color == "#FF0000" || color == "red" || color == "красный")
                return "red";
            if (color == "#0000ff" || color == "#0000FF" || color == "blue" || color == "синий")
                return "blue";
            if (color == "#ffd700" || color == "#FFD700" || color == "yellow" || color == "желтый")
                return "yellow";

            return "other";
        }
    }
}

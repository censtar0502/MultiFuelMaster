using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MultiFuelMaster.Converters
{
    public class BoolToAddEditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdding)
            {
                return isAdding ? "Добавить вид топлива" : "Редактировать вид топлива";
            }
            return "Редактировать вид топлива";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

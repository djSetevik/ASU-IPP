using System;
using System.Globalization;
using System.Windows.Data;

namespace ASUIPP.App.Converters
{
    /// <summary>
    /// Инвертирует bool. Нужен для RadioButton: один привязан к IsHead, другой к !IsHead.
    /// </summary>
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }
}
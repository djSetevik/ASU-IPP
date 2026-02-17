using ASUIPP.App;
using ASUIPP.App.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASUIPP.App.Converters
{
    /// <summary>
    /// Конвертирует количество дней до дедлайна в текст и цвет для напоминалки.
    /// </summary>
    public class DaysToReminderTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int days)
            {
                if (days < 0)
                    return $"ПРОСРОЧЕНО на {Math.Abs(days)} дн.";
                if (days == 0)
                    return "Сегодня!";
                if (days == 1)
                    return "Завтра";

                return $"осталось {days} дн.";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DaysToReminderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int days)
            {
                if (days < 0)
                    return new SolidColorBrush(Color.FromRgb(200, 0, 0));   // красный
                if (days <= 3)
                    return new SolidColorBrush(Color.FromRgb(200, 100, 0)); // оранжевый
                if (days <= 7)
                    return new SolidColorBrush(Color.FromRgb(180, 180, 0)); // жёлтый

                return new SolidColorBrush(Color.FromRgb(0, 0, 0));         // чёрный
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
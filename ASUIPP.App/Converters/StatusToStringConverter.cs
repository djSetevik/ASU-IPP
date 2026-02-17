using System;
using System.Globalization;
using System.Windows.Data;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Converters
{
    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkStatus status)
            {
                switch (status)
                {
                    case WorkStatus.Planned: return "Запланирована";
                    case WorkStatus.InProgress: return "Выполняется";
                    case WorkStatus.Done: return "Ожидает подтверждения";
                    case WorkStatus.Confirmed: return "Подтверждена";
                    case WorkStatus.Reported: return "Учтена в отчёте";
                }
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkStatus status)
            {
                switch (status)
                {
                    case WorkStatus.Planned: return new SolidColorBrush(Color.FromRgb(128, 128, 128)); // серый
                    case WorkStatus.InProgress: return new SolidColorBrush(Color.FromRgb(0, 100, 200)); // синий
                    case WorkStatus.Done: return new SolidColorBrush(Color.FromRgb(200, 150, 0)); // жёлтый
                    case WorkStatus.Confirmed: return new SolidColorBrush(Color.FromRgb(0, 150, 0)); // зелёный
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
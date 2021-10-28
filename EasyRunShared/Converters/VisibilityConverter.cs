using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyRun.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility onTrue = Visibility.Visible;
            Visibility onFalse = Visibility.Hidden;

            if (parameter is string str)
            {
                var trueFalse = str.Split(';');

                if (trueFalse.Length >= 1)
                {
                    onTrue = MapToVisibility(trueFalse[0]);
                }

                if (trueFalse.Length >= 2)
                {
                    onFalse = MapToVisibility(trueFalse[1]);
                }
            }

            bool flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                flag = nullable ?? false;
            }
            return flag ? onTrue : onFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Visibility MapToVisibility(string str)
        {
            if (str.Equals("Collapsed", StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }
            else if (str.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }
    }
}

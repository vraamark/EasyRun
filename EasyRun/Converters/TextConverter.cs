using System;
using System.Globalization;
using System.Windows.Data;

namespace EasyRun.Converters
{
    public class TextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string onTrue = "?";
            string onFalse = "?";

            if (parameter is string str)
            {
                var trueFalse = str.Split(';');

                if (trueFalse.Length >= 1)
                {
                    onTrue = trueFalse[0];
                }

                if (trueFalse.Length >= 2)
                {
                    onFalse = trueFalse[1];
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
    }
}

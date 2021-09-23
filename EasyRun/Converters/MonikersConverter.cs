using EasyRun.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace EasyRun.Converters
{
    public class MonikersConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServiceType serviceType)
            {
                switch (serviceType)
                {
                    case ServiceType.Executable:
                        return Microsoft.VisualStudio.Imaging.KnownMonikers.BinaryFile;

                    case ServiceType.Image:
                        return Microsoft.VisualStudio.Imaging.KnownMonikers.Docker;
                }
            }

            return Microsoft.VisualStudio.Imaging.KnownMonikers.CSProjectNode;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

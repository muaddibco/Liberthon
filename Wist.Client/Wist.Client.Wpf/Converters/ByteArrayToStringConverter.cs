using System;
using System.Globalization;
using System.Windows.Data;
using Wist.Core.ExtensionMethods;

namespace Wist.Client.Wpf.Converters
{
    
    public class ByteArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is byte[] arr))
            {
                return string.Empty;
            }
            return arr.ToHexString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

    }
}
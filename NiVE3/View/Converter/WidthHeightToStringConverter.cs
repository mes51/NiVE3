using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    class WidthHeightToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v is not int))
            {
                throw new ArgumentException(nameof(values));
            }
            else if (values.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            return $"{values[0]}x{values[1]}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

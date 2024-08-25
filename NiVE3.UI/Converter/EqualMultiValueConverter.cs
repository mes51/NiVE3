using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NiVE3.UI.Converter
{
    public class EqualMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return true;
            }

            if (values.All(v => v == null))
            {
                return true;
            }
            else if (values.Any(v => v == null))
            {
                return false;
            }

            return values.Skip(1).All(v => v.Equals(values[0]));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

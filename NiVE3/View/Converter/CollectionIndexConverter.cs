using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.Extension;

namespace NiVE3.View.Converter
{
    class CollectionIndexConverter : IMultiValueConverter
    {
        public int Offset { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1)
            {
                if (values[0] is IEnumerable collection1)
                {
                    return collection1.Cast<object>().IndexOf(v => (v == null && values[1] == null) || (v?.Equals(values[1]) ?? false)) + Offset;
                }
                else if (values[1] is IEnumerable collection2)
                {
                    return collection2.Cast<object>().IndexOf(v => (v == null && values[0] == null) || (v?.Equals(values[0]) ?? false)) + Offset;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

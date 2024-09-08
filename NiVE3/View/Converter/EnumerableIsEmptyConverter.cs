using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(IEnumerable), typeof(bool))]
    class EnumerableIsEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Array array => array.Length < 1,
                string str => string.IsNullOrEmpty(str),
                ICollection collection => collection.Count < 1,
                IEnumerable enumerable => enumerable.OfType<object?>().Any(),
                _ => DependencyProperty.UnsetValue
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

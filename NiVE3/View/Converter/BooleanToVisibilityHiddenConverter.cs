using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    class BooleanToVisibilityHiddenConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool v)
            {
                return v ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v == Visibility.Visible;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}

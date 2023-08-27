using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(Color), typeof(Brush))]
    class ColorToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                return new SolidColorBrush(c);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush sb)
            {
                return sb.Color;
            }
            else if (value is Brush)
            {
                return null;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}

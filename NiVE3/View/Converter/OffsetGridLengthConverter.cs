using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace NiVE3.View.Converter
{
    class OffsetGridLengthConverter : IValueConverter
    {
        public double SizeOffset { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v)
            {
                return new GridLength(Math.Max(v + SizeOffset, 0.0));
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength v)
            {
                return v.Value - SizeOffset;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}

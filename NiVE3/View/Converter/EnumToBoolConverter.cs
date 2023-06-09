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
    [ValueConversion(typeof(Enum), typeof(bool))]
    class EnumToBoolConverter : IValueConverter
    {
        public bool UseAndCompare { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null && value.GetType() == parameter.GetType())
            {
                if (UseAndCompare)
                {
                    return (System.Convert.ToUInt64(value) & System.Convert.ToUInt64(parameter)) != 0;
                }
                else
                {
                    return value.ToString() == parameter.ToString();
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null && (bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString() ?? "");
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

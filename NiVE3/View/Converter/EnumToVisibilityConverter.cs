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
    [ValueConversion(typeof(Enum), typeof(Visibility))]
    class EnumToVisibilityConverter : IValueConverter
    {
        public bool UseAndCompare
        {
            get => Converter.UseAndCompare;
            set => Converter.UseAndCompare = value;
        }

        public bool FalseIsHidden { get; set; }

        EnumToBoolConverter Converter { get; } = new EnumToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = (bool)Converter.Convert(value, targetType, parameter, culture);
            if (result)
            {
                return Visibility.Visible;
            }
            else if (FalseIsHidden)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

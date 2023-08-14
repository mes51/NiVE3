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
    class VisibilityToSwitchValueConverter : IValueConverter
    {
        public object? VisibleValue { get; set; }

        public object? HiddenValue { get; set; }

        public object? CollapsedValue { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                switch (v)
                {
                    case Visibility.Visible:
                        return VisibleValue;
                    case Visibility.Hidden:
                        return HiddenValue;
                    case Visibility.Collapsed:
                        return CollapsedValue;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

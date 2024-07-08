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
    class SwitchValueConverter : Freezable, IValueConverter, IMultiValueConverter
    {
        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(
            nameof(TrueValue),
            typeof(object),
            typeof(SwitchValueConverter),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(
            nameof(FalseValue),
            typeof(object),
            typeof(SwitchValueConverter),
            new PropertyMetadata(null)
        );

        public object? FalseValue
        {
            get { return (object?)GetValue(FalseValueProperty); }
            set { SetValue(FalseValueProperty, value); }
        }

        public object? TrueValue
        {
            get { return (object?)GetValue(TrueValueProperty); }
            set { SetValue(TrueValueProperty, value); }
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool v)
            {
                if (v)
                {
                    return TrueValue;
                }
                else
                {
                    return FalseValue;
                }
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is bool sw)
            {
                if (sw)
                {
                    return values.Length > 1 ? values[1] : TrueValue;
                }
                else
                {
                    return values.Length > 2 ? values[2] : FalseValue;
                }
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new SwitchValueConverter();
        }
    }
}

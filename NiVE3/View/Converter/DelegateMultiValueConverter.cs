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
    class DelegateMultiValueConverter<TFrom1, TFrom2, TTo> : IMultiValueConverter
    {
        Func<TFrom1, TFrom2, TTo> ConvertFunc { get; }

        public DelegateMultiValueConverter(Func<TFrom1, TFrom2, TTo> convertFunc)
        {
            ConvertFunc = convertFunc;
        }

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length > 1 && values[0] is TFrom1 from1 && values[1] is TFrom2 from2)
            {
                return ConvertFunc(from1, from2);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class DelegateMultiValueConverter<TFrom1, TFrom2, TTo, TParam> : IMultiValueConverter
    {
        Func<TFrom1, TFrom2, TParam, TTo> ConvertFunc { get; }

        public DelegateMultiValueConverter(Func<TFrom1, TFrom2, TParam, TTo> convertFunc)
        {
            ConvertFunc = convertFunc;
        }

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length > 1 && values[0] is TFrom1 from1 && values[1] is TFrom2 from2 && parameter is TParam param)
            {
                return ConvertFunc(from1, from2, param);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

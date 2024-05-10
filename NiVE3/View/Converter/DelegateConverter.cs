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
    class DelegateConverter<TFrom, TTo> : IValueConverter
    {
        Func<TFrom, TTo>? ConvertFunc { get; }

        Func<TTo, TFrom>? ConvertBackFunc { get; }

        public DelegateConverter(Func<TFrom, TTo>? convertFunc) : this(convertFunc, null) { }

        public DelegateConverter(Func<TFrom, TTo>? convertFunc, Func<TTo, TFrom>? convertBackFunc)
        {
            ConvertFunc = convertFunc;
            ConvertBackFunc = convertBackFunc;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TFrom v && ConvertFunc != null)
            {
                return ConvertFunc(v);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TTo v && ConvertBackFunc != null)
            {
                return ConvertBackFunc(v);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

    class DelegateConverter<TFrom, TTo, TParam> : IValueConverter
    {
        Func<TFrom, TParam, TTo>? ConvertFunc { get; }

        Func<TTo, TParam, TFrom>? ConvertBackFunc { get; }

        public DelegateConverter(Func<TFrom, TParam, TTo>? convertFunc) : this(convertFunc, null) { }

        public DelegateConverter(Func<TFrom, TParam, TTo>? convertFunc, Func<TTo, TParam, TFrom>? convertBackFunc)
        {
            ConvertFunc = convertFunc;
            ConvertBackFunc = convertBackFunc;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (ConvertFunc != null && value is TFrom v && parameter is TParam param)
            {
                return ConvertFunc(v, param);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (ConvertBackFunc != null && value is TTo v && parameter is TParam param)
            {
                return ConvertBackFunc(v, param);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

    class DelegateMultiConverter<TFrom1, TFrom2, TTo> : IMultiValueConverter
    {
        Func<TFrom1, TFrom2, TTo>? ConvertFunc { get; }

        Func<TTo, (TFrom1, TFrom2)>? ConvertBackFunc { get; }

        public DelegateMultiConverter(Func<TFrom1, TFrom2, TTo>? convertFunc) : this(convertFunc, null) { }

        public DelegateMultiConverter(Func<TFrom1, TFrom2, TTo>? convertFunc, Func<TTo, (TFrom1, TFrom2)>? convertBackFunc)
        {
            ConvertFunc = convertFunc;
            ConvertBackFunc = convertBackFunc;
        }

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (ConvertFunc != null && values.Length > 1 && values[0] is TFrom1 from1 && values[1] is TFrom2 from2)
            {
                return ConvertFunc(from1, from2);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            if (ConvertBackFunc != null && value is TTo to)
            {
                var (from1, from2) = ConvertBackFunc(to);
                return [from1, from2];
            }
            else
            {
                return [DependencyProperty.UnsetValue, DependencyProperty.UnsetValue];
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NiVE3.View.Converter
{
    [ContentProperty(nameof(Converters))]
    class ConverterCollection : IValueConverter
    {
        public List<ConverterItem> Converters { get; set; } = [];

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var converted = value;
            foreach (var converter in Converters)
            {
                converted = converter.Converter?.Convert(converted, converter.TargetType, parameter, culture);
            }
            return converted;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(Converters))]
    class MultiConverterCollection : IMultiValueConverter
    {
        public IMultiValueConverter? ToSingleConverter { get; set; }

        public List<ConverterItem> Converters { get; set; } = [];

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (ToSingleConverter == null)
            {
                return DependencyProperty.UnsetValue;
            }

            var converted = ToSingleConverter.Convert(values, targetType, parameter, culture);
            foreach (var converter in Converters)
            {
                converted = converter.Converter?.Convert(converted, converter.TargetType, parameter, culture);
            }
            return converted;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(Converter))]
    class ConverterItem
    {
        public IValueConverter? Converter { get; set; }

        public Type? TargetType { get; set; }
    }
}

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
    [ValueConversion(typeof(object), typeof(string))]
    class LocalizationConverter : Freezable, IValueConverter, IMultiValueConverter
    {
        public static readonly DependencyProperty LocalizeFormatProperty = DependencyProperty.Register(
            nameof(LocalizeFormat),
            typeof(string),
            typeof(LocalizationConverter),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(
            nameof(Converter),
            typeof(IValueConverter),
            typeof(LocalizationConverter),
            new PropertyMetadata(null, ConverterChanged)
        );

        public static readonly DependencyProperty MultiValueConverterProperty = DependencyProperty.Register(
            nameof(MultiValueConverter),
            typeof(IMultiValueConverter),
            typeof(LocalizationConverter),
            new PropertyMetadata(null, ConverterChanged)
        );

        public static readonly DependencyProperty TargetTypeProperty = DependencyProperty.Register(
            nameof(TargetType),
            typeof(Type),
            typeof(LocalizationConverter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public Type? TargetType
        {
            get { return (Type)GetValue(TargetTypeProperty); }
            set { SetValue(TargetTypeProperty, value); }
        }

        public IMultiValueConverter? MultiValueConverter
        {
            get { return (IMultiValueConverter)GetValue(MultiValueConverterProperty); }
            set { SetValue(MultiValueConverterProperty, value); }
        }

        public IValueConverter? Converter
        {
            get { return (IValueConverter)GetValue(ConverterProperty); }
            set { SetValue(ConverterProperty, value); }
        }

        public string? LocalizeFormat
        {
            get { return (string)GetValue(LocalizeFormatProperty); }
            set { SetValue(LocalizeFormatProperty, value); }
        }

        Type? DefaultTargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var converter = Converter;
            if (converter != null)
            {
                value = converter.Convert(value, TargetType ?? DefaultTargetType ?? targetType, parameter, culture);
            }

            var format = LocalizeFormat;
            if (format != null)
            {
                return string.Format(format, value);
            }
            else
            {
                return value?.ToString() ?? "";
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var converter = MultiValueConverter;
            var format = LocalizeFormat;
            if (converter != null)
            {
                var convertedValue = converter.Convert(values, TargetType ?? DefaultTargetType ?? targetType, parameter, culture);
                if (format != null)
                {
                    return string.Format(format, convertedValue);
                }
                else
                {
                    return convertedValue.ToString() ?? "";
                }
            }
            else
            {
                if (format != null)
                {
                    return string.Format(format, values);
                }
                else
                {
                    return values.ToString() ?? "";
                }
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
            return new LocalizationConverter();
        }

        static void ConverterChanged(DependencyObject sender,  DependencyPropertyChangedEventArgs e)
        {
            if (sender is  LocalizationConverter converter)
            {
                var conversion = e.NewValue?.GetType()?.GetCustomAttributes(typeof(ValueConversionAttribute), true)?.OfType<ValueConversionAttribute>()?.FirstOrDefault();
                if (conversion == null)
                {
                    converter.DefaultTargetType = null;
                    return;
                }

                converter.DefaultTargetType = conversion.TargetType;
            }
        }
    }
}

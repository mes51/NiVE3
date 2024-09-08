using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    class StringJoinConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty SeparatorProperty = DependencyProperty.Register(
            nameof(Separator),
            typeof(string),
            typeof(StringJoinConverter),
            new PropertyMetadata(", ")
        );

        public string Separator
        {
            get { return (string)GetValue(SeparatorProperty); }
            set { SetValue(SeparatorProperty, value); }
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable enumerable)
            {
                return string.Join(Separator, [..enumerable.OfType<object>()]);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new StringJoinConverter();
        }
    }
}

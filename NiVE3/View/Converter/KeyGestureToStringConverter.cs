using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using NiVE3.Wpf.Input;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(InputGesture), typeof(string))]
    partial class KeyGestureToStringConverter : IValueConverter
    {
        KeyGestureConverter KeyGestureConverter { get; } = new KeyGestureConverter();

        KeyConverter KeyConverter { get; } = new KeyConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null)
            {
                switch (value)
                {
                    case KeyGesture keyGesture:
                        return KeyGestureConverter.ConvertTo(keyGesture, typeof(string));
                    case SingleKeyGesture singleKeyGesture:
                        return (singleKeyGesture.IsUseShift ? "Shift + " : "") + KeyConverter.ConvertTo(singleKeyGesture.Key, typeof(string));
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType == typeof(KeyGesture) && value is string)
            {
                return KeyGestureConverter.ConvertFrom(value);
            }
            else if (targetType == typeof(SingleKeyGesture) && value is string str)
            {
                var hasShift = ShiftKeyRegex().IsMatch(str);
                return new SingleKeyGesture((Key?)KeyConverter.ConvertFrom(ShiftKeyRegex().Replace(str, "")) ?? Key.None, hasShift);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        [GeneratedRegex(@"shift\s*\+\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ja-JP")]
        private static partial Regex ShiftKeyRegex();
    }
}

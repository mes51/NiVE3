using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Resource;
using System.Windows.Data;
using System.Windows;

namespace NiVE3.PresetPlugin.Internal.View.Converter
{
    [ValueConversion(typeof(Enum), typeof(string))]
    class EnumToLocalizedNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter is Type enumType && enumType.IsEnum && value.GetType().IsAssignableTo(enumType) && Enum.GetName(enumType, value) is string enumName)
            {
                return LanguageResourceDictionary.Dictionary.GetText($"{enumType.Name}_{enumName}");
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
    }
}

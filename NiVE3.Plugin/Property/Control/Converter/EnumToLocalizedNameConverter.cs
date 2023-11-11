using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Control.Converter
{
    [ValueConversion(typeof(Enum), typeof(string))]
    class EnumToLocalizedNameConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty LanguageResourceDictionaryTypeProperty = DependencyProperty.Register(
            nameof(LanguageResourceDictionaryType),
            typeof(Type),
            typeof(EnumToLocalizedNameConverter),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty EnumTypeProperty = DependencyProperty.Register(
            nameof(EnumType),
            typeof(Type),
            typeof(EnumToLocalizedNameConverter),
            new PropertyMetadata(null)
        );

        public Type EnumType
        {
            get { return (Type)GetValue(EnumTypeProperty); }
            set { SetValue(EnumTypeProperty, value); }
        }

        public Type? LanguageResourceDictionaryType
        {
            get { return (Type)GetValue(LanguageResourceDictionaryTypeProperty); }
            set { SetValue(LanguageResourceDictionaryTypeProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = (parameter as Type) ?? EnumType;
            if (value != null && enumType.IsEnum && value.GetType().IsAssignableTo(enumType) && Enum.GetName(enumType, value) is string enumName)
            {
                return LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText($"{enumType.Name}_{enumName}") ?? "";
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

        protected override Freezable CreateInstanceCore()
        {
            return new EnumToLocalizedNameConverter();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.Plugin.Property.Control.Converter
{
    class EnumTypeToArrayConverter : IValueConverter
    {
        public bool FilterNeverEditorBrowsable { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type enumType && enumType.IsEnum)
            {
                var result = Enum.GetValues(enumType);
                if (FilterNeverEditorBrowsable)
                {
                    return result.OfType<Enum>().Where(e => enumType.GetField(e.ToString())?.GetCustomAttribute<EditorBrowsableAttribute>()?.State != EditorBrowsableState.Never).ToArray();
                }
                else
                {
                    return result;
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
    }
}

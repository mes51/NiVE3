using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.UI.Converter
{
    [ValueConversion(typeof(int), typeof(string))]
    public class CalcIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                try
                {
                    var result = new DataTable().Compute(str, null);
                    switch (result)
                    {
                        case byte v:
                            return (int)v;
                        case short v:
                            return (int)v;
                        case int v:
                            return v;
                        case long v:
                            return (int)v;
                        case sbyte v:
                            return (int)v;
                        case ushort v:
                            return (int)v;
                        case uint v:
                            return (int)v;
                        case ulong v:
                            return (int)v;
                        case float v:
                            return (int)MathF.Round(v);
                        case double v:
                            return (int)Math.Round(v);
                    }
                }
                catch { } // 例外発生時はすべてUnsetValue
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

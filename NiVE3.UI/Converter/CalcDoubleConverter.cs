using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.UI.Converter
{
    [ValueConversion(typeof(double), typeof(string))]
    public class CalcDoubleConverter : IValueConverter
    {
        public int Digit { get; set; } = -1;

        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double v)
            {
                return Digit > -1 ? v.ToString($"F{Digit}") : v.ToString();
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                try
                {
                    var result = new DataTable().Compute(str, null);
                    switch (result)
                    {
                        case byte v:
                            return (double)v;
                        case short v:
                            return (double)v;
                        case int v:
                            return (double)v;
                        case long v:
                            return (double)v;
                        case sbyte v:
                            return (double)v;
                        case ushort v:
                            return (double)v;
                        case uint v:
                            return (double)v;
                        case ulong v:
                            return (double)v;
                        case float v:
                            return (double)v;
                        case double v:
                            return v;
                    }
                }
                catch { } // 例外発生時はすべてUnsetValue
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

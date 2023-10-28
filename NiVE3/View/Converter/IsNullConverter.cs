using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    class IsNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var invert = false;
            switch (parameter)
            {
                case string s:
                    invert = s.ToLowerInvariant().Trim() == "true";
                    break;
                case bool v:
                    invert = v;
                    break;
            }
            return invert ? (value != null) : (value == null);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

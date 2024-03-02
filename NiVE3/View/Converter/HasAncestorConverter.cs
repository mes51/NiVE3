using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NiVE3.View.Converter
{
    class HasAncestorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DependencyObject targtChild || parameter is not Type ancestorType)
            {
                return false;
            }

            var parent = VisualTreeHelper.GetParent(targtChild);
            while (parent != null)
            {
                if (parent.GetType().IsAssignableTo(ancestorType))
                {
                    return true;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

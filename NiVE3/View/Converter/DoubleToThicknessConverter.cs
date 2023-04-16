using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(double), typeof(Thickness))]
    class DoubleToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double thickness && parameter is ThicknessConvertFace faces)
            {
                return new Thickness(
                    (faces & ThicknessConvertFace.Left) != ThicknessConvertFace.None ? thickness : 0.0,
                    (faces & ThicknessConvertFace.Top) != ThicknessConvertFace.None ? thickness : 0.0,
                    (faces & ThicknessConvertFace.Right) != ThicknessConvertFace.None ? thickness : 0.0,
                    (faces & ThicknessConvertFace.Bottom) != ThicknessConvertFace.None ? thickness : 0.0
                );
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

    [TypeConverter(typeof(EnumConverter))]
    [Flags]
    enum ThicknessConvertFace : int
    {
        None = 0b0000,
        Left = 0b0001,
        Top = 0b0010,
        Right = 0b0100,
        Bottom = 0b1000,
        All = 0b1111
    }
}

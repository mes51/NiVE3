using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NiVE3.Plugin.Property.Control.Converter
{
    [ValueConversion(typeof(Vector4), typeof(Brush))]
    class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case Vector4 v:
                    var clamped = Vector4.Max(Vector4.Min(v, Vector4.One), Vector4.Zero);
                    return new SolidColorBrush(Color.FromArgb((byte)(clamped.W * 255.0F), (byte)(clamped.Z * 255.0F), (byte)(clamped.Y * 255.0F), (byte)(clamped.X * 255.0F)));
                case Color c:
                    return new SolidColorBrush(c);
                default:
                    return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var c = brush.Color;
                return new Vector4(c.B / 255.0F, c.G / 255.0F, c.R / 255.0F, c.A / 255.0F);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}

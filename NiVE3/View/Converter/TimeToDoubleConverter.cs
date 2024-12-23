using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(Time), typeof(double))]
    class TimeToDoubleConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(TimeToDoubleConverter),
            new PropertyMetadata(30.0)
        );

        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Time time)
            {
                return (double)time;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v)
            {
                if (!double.IsNaN(FrameRate) && !double.IsInfinity(FrameRate))
                {
                    return Time.FromTime(v, FrameRate);
                }
                else
                {
                    return Time.FromTime(v);
                }
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TimeToDoubleConverter();
        }
    }
}

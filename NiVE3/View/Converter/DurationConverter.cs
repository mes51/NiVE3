using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.UI.Converter;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(double), typeof(string))]
    class DurationConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(DurationConverter),
            new PropertyMetadata(1.0)
        );

        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        CalcDoubleConverter Converter { get; } = new CalcDoubleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double time)
            {
                return DependencyProperty.UnsetValue;
            }

            var hour = (int)(time / 3600);
            var minute = (int)((time % 3600) / 60);
            var second = (int)(time % 60);
            var frame = (int)((time % 1) * FrameRate);

            return $"{hour}:{minute:D2}:{second:D2}:{frame:D2}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string timeText)
            {
                return DependencyProperty.UnsetValue;
            }

            var part = timeText.Split(":");
            Array.Reverse(part);
            var time = 0.0;
            if (part.Length > 0)
            {
                time += ParseValue(part[0]) / FrameRate;
            }
            if (part.Length > 1)
            {
                time += ParseValue(part[1]);
            }
            if (part.Length > 2)
            {
                time += ParseValue(part[2]) * 60.0;
            }
            if (part.Length > 3)
            {
                time += ParseValue(part[2]) * 3600.0;
            }

            if (double.IsNaN(time))
            {
                return DependencyProperty.UnsetValue;
            }
            else
            {
                return time;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new DurationConverter();
        }

        double ParseValue(string text)
        {
            if (Converter.ConvertBack(text, typeof(double), null, CultureInfo.InvariantCulture) is double v)
            {
                return v;
            }
            else
            {
                return double.NaN;
            }
        }
    }
}

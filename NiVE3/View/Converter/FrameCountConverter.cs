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
    class FrameCountConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(FrameCountConverter),
            new PropertyMetadata(0.0)
        );

        public static readonly DependencyProperty DigitProperty = DependencyProperty.Register(
            nameof(Digit),
            typeof(int),
            typeof(FrameCountConverter),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public int Digit
        {
            get { return (int)GetValue(DigitProperty); }
            set { SetValue(DigitProperty, value); }
        }

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

            var count = (int)(time * FrameRate);
            if (Digit > 0)
            {
                return count.ToString($"D0{Digit}");
            }
            else
            {
                return count.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string timeText)
            {
                return DependencyProperty.UnsetValue;
            }

            var time = ParseValue(timeText) * FrameRate;
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
            return new FrameCountConverter();
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

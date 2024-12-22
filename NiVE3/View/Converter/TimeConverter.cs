using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.Plugin.ValueObject;
using NiVE3.UI.Converter;
using NiVE3.Util;

namespace NiVE3.View.Converter
{
    [ValueConversion(typeof(double), typeof(string))]
    class TimeConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(TimeConverter),
            new PropertyMetadata(1.0)
        );

        public static readonly DependencyProperty IsTimeStructProperty = DependencyProperty.Register(
            nameof(IsTimeStruct),
            typeof(bool),
            typeof(TimeConverter),
            new PropertyMetadata(false)
        );

        public bool IsTimeStruct
        {
            get { return (bool)GetValue(IsTimeStructProperty); }
            set { SetValue(IsTimeStructProperty, value); }
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
                if (value is not Time structTime)
                {
                    return DependencyProperty.UnsetValue;
                }
                time = (double)structTime;
            }

            var sign = Math.Sign(time);
            time = Math.Abs(time);
            var hour = (int)(time / 3600);
            var minute = (int)((time % 3600) / 60);
            var second = (int)(time % 60);
            var frame = (int)Math.Round((time % 1) * FrameRate);

            return $"{(sign < 0.0 ? "-" : "")}{hour}:{minute:D2}:{second:D2}:{frame.ToString("D" + Math.Max((int)Math.Ceiling(Math.Log10(FrameRate)), 2))}";
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
            var sign = 1.0;
            if (part.Length > 0)
            {
                var parsedValue = ParseValue(part[0]);
                time += Math.Abs(parsedValue) / FrameRate;
                sign = Math.Sign(parsedValue);
            }
            if (part.Length > 1)
            {
                var parsedValue = ParseValue(part[1]);
                time += Math.Abs(parsedValue);
                sign = Math.Sign(parsedValue);
            }
            if (part.Length > 2)
            {
                var parsedValue = ParseValue(part[2]);
                time += Math.Abs(parsedValue) * 60.0;
                sign = Math.Sign(parsedValue);
            }
            if (part.Length > 3)
            {
                var parsedValue = ParseValue(part[3]);
                time += Math.Abs(parsedValue) * 3600.0;
                sign = Math.Sign(parsedValue);
            }

            if (sign == 0.0)
            {
                if (part[^1].StartsWith("-"))
                {
                    sign = -1.0;
                }
                else
                {
                    sign = 1.0;
                }
            }

            if (double.IsNaN(time))
            {
                return DependencyProperty.UnsetValue;
            }
            else
            {
                var result = TimeCalc.RoundTimeDigit(time) * sign;
                if (IsTimeStruct)
                {
                    return Time.FromTime(time, FrameRate);
                }
                else
                {
                    return result;
                }
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TimeConverter();
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

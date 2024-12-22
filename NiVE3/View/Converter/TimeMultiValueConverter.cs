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
    class TimeMultiValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// 長さとフレームレートから時間表記の文字列に変換する
        /// </summary>
        /// <param name="values">
        /// 0: duration
        /// 1: frame rate
        /// </param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }
            if (values[1] is not double frameRate)
            {
                return DependencyProperty.UnsetValue;
            }
            if (values[0] is not double time)
            {
                if (values[0] is not Time structTime)
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
            var frame = (int)((time % 1) * frameRate);

            return $"{(sign < 0.0 ? "-" : "")}{hour}:{minute:D2}:{second:D2}:{frame.ToString("D" + Math.Max((int)Math.Ceiling(Math.Log10(frameRate)), 2))}";
        }

        /// <summary>
        /// 非対応
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray();
        }
    }
}

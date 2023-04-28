using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    class TimeConverter : IMultiValueConverter
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
            if (values.Any(v => v is not double))
            {
                return DependencyProperty.UnsetValue;
            }
            else if (values.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            var time = (double)values[0];
            var frameRate = (double)values[1];
            var hour = (int)(time / 3600);
            var minute = (int)((time % 3600) / 60);
            var second = (int)(time % 60);
            var frame = (int)((time % 1) * frameRate);

            return $"{hour}:{minute:D2}:{second:D2}:{frame:D2}";
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

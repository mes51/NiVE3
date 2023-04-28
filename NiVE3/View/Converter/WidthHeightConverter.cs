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
    class WidthHeightConverter : IMultiValueConverter
    {
        /// <summary>
        /// 幅と高さを widthxheight の文字列に変換する
        /// </summary>
        /// <param name="values">
        /// 0: width
        /// 1: height
        /// </param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v is not int))
            {
                return DependencyProperty.UnsetValue;
            }
            else if (values.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            return $"{values[0]}x{values[1]}";
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

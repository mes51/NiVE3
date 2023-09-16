using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    /// <summary>
    /// 値の補間を行います
    /// </summary>
    public static class Interpolation
    {
        /// <summary>
        /// 値を線形補間します
        /// </summary>
        /// <param name="value1">1つ目の値</param>
        /// <param name="value2">2つ目の値</param>
        /// <param name="time1">1つ目の値の時間</param>
        /// <param name="time2">2つ目の値の時間</param>
        /// <param name="time">現在時刻</param>
        /// <returns>補間後の値</returns>
        public static double Linear(double value1, double value2, double time1, double time2, double time)
        {
            return (value1 - value2) / (time1 - time2) * (time - time1) + value1;
        }

        /// <summary>
        /// 値をCatmull-Rom補間します
        /// </summary>
        /// <param name="value0">1つ前の値</param>
        /// <param name="value1">1つ目の値</param>
        /// <param name="value2">2つ目の値</param>
        /// <param name="value3">3つ目の値</param>
        /// <param name="time1">1つ目の値の時間</param>
        /// <param name="time2">2つ目の値の時間</param>
        /// <param name="time">現在時刻</param>
        /// <returns>補間後の値</returns>
        public static double CatmullRom(double value0, double value1, double value2, double value3, double time1, double time2, double time)
        {
            double t = 1.0 / (time2 - time1) * (time - time1);
            double v0 = (value2 - value0) / 2.0;
            double v1 = (value3 - value1) / 2.0;
            double t2 = t * t;
            double t3 = t2 * t;
            return (2.0 * (value1 - value2) + v0 + v1) * t3 + (-3.0 * (value1 + value2) - 2.0 * v0 - v1) * t2 + v0 * t + value1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    static class MathUtil
    {
        /// <summary>
        /// minがmax以上になる可能性がある時用のClamp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MaxAndMin<T>(T value, T min, T max) where T : INumber<T>
        {
            if (min > max)
            {
                min = max;
            }
            return T.Min(T.Max(value, min), max);
        }
    }
}

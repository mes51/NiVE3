using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Expression.Utility
{
    static class ExpressionInterpolation
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public static double linear(double value1, double value2, double t)
        {
            return double.Lerp(value1, value2, Math.Clamp(t, 0.0, 1.0));
        }

        [ExpressionPublicMember]
        public static double[] linear(double[] value1, double[] value2, double t)
        {
            return [..value1.Zip(value2, (v1, v2) => double.Lerp(v1, v2, Math.Clamp(t, 0.0, 1.0)))];
        }

        [ExpressionPublicMember]
        public static double catmullRom(double value0, double value1, double value2, double value3, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            var t2 = t * t;
            var t3 = t2 * t;
            var a3 = (value2 - value0) * 0.5;
            var a1 = (value3 - value1) * 0.5 - value2 * 2.0 + a3 + value1 * 2.0;
            var a2 = value2 * 3.0 - (value3 - value1) * 0.5 - a3 * 2.0 - value1 * 3.0;
            return a1 * t3 + a2 * t2 + a3 * t + value1;
        }

        [ExpressionPublicMember]
        public static double[] catmullRom(double[] value0, double[] value1, double[] value2, double[] value3, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            var t2 = t * t;
            var t3 = t2 * t;
            return [..value0.Zip(value1, value2, value3, (v0, v1, v2, v3) =>
            {
                var a3 = (v2 - v0) * 0.5;
                var a1 = (v3 - v1) * 0.5 - v2 * 2.0 + a3 + v1 * 2.0;
                var a2 = v2 * 3.0 - (v3 - v1) * 0.5 - a3 * 2.0 - v1 * 3.0;
                return a1 * t3 + a2 * t2 + a3 * t + v1;
            })];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}

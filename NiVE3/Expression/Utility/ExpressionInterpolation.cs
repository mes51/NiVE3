using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}

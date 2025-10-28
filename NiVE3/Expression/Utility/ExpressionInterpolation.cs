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

        [ExpressionPublicMember]
        public static double bezier(double value1, double value2, double controlValue1, double controlValue2, double effectValue1, double effectValue2, double x)
        {
            x = Math.Clamp(x, 0.0, 1.0);
            effectValue1 = Math.Clamp(effectValue1, 0.0, 1.0);
            effectValue2 = 1.0 - Math.Clamp(effectValue2, 0.0, 1.0);
            if (x <= 0.0 || x >= 1.0 || (effectValue1 <= 0.0 && effectValue2 <= 0.0))
            {
                return double.Lerp(value1, value2, x);
            }

            var t = SolveT(x, effectValue1, effectValue2);

            var ay = 3.0 * controlValue1 - 3.0 * controlValue2 + 1.0;
            var by = controlValue2 - 2.0 * controlValue1;
            var cy = 3.0 * controlValue1;

            var v = ((ay * t + 3 * by) * t + cy) * t;
            return double.Lerp(value1, value2, v);
        }

        [ExpressionPublicMember]
        public static double[] bezier(double[] value1, double[] value2, double controlValue1, double controlValue2, double effectValue1, double effectValue2, double x)
        {
            var minLength = Math.Min(value1.Length, value2.Length);
            return bezier(value1, value2, [..Enumerable.Repeat(controlValue1, minLength)], [..Enumerable.Repeat(controlValue2, minLength)], effectValue1, effectValue2, x);
        }

        [ExpressionPublicMember]
        public static double[] bezier(double[] value1, double[] value2, double[] controlValue1, double[] controlValue2, double effectValue1, double effectValue2, double x)
        {
            x = Math.Clamp(x, 0.0, 1.0);
            effectValue1 = Math.Clamp(effectValue1, 0.0, 1.0);
            effectValue2 = 1.0 - Math.Clamp(effectValue2, 0.0, 1.0);
            if (x <= 0.0 || x >= 1.0 || (controlValue1.All(c => c == effectValue1) && controlValue2.All(c => c == effectValue2)))
            {
                return [..value1.Zip(value2, (v1, v2) =>
                {
                    return double.Lerp(v1, v2, x);
                })];
            }

            var t = SolveT(x, effectValue1, effectValue2);

            return [..value1.Zip(value2, controlValue1, controlValue2, (v1, v2, c1, c2) =>
            {
                var ay = 3.0 * c1 - 3.0 * c2 + 1.0;
                var by = c2 - 2.0 * c1;
                var cy = 3.0 * c1;

                var v = ((ay * t + 3 * by) * t + cy) * t;
                return double.Lerp(v1, v2, v);
            })];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members

        static double SolveT(double x, double x1, double x2)
        {
            // http://dmitry.baranovskiy.com/bezier-easing.html
            var alpha = 3.0 * (3.0 * x1 - 3.0 * x2 + 1.0);
            if (alpha == 0.0)
            {
                return x;
            }

            var beta = 6.0 * (x2 - 2.0 * x1);
            var gamma = 3.0 * x1;

            var a2 = alpha * alpha * 4.0;
            var b2 = beta * beta;
            var a3 = Math.Pow(alpha, 3.0) * 8.0;
            var b3 = Math.Pow(beta, 3.0);

            var q1 = (-b3 / a3) + (3.0 * beta * gamma / a2);
            var w = Math.Pow((gamma / alpha) - (b2 / a2), 3.0);
            var delta = beta / (2.0 * alpha);
            var qx = q1 + (3.0 * x / (2.0 * alpha));
            // NOTE: 元の記事にはないが、q(x) = 0の時、θがMath.PI / 2になるが、そのときの値が異常(1以上の値)になる
            //       おそらくx == tになるのが正しいので、そのまま値を返す
            if (qx == 0.0)
            {
                return x;
            }
            var sx = qx * qx + w;

            if (sx > 0.0)
            {
                var sxSqrt = Math.Sqrt(sx);
                return Math.Cbrt(qx + sxSqrt) + Math.Cbrt(qx - sxSqrt) - delta;
            }

            var a = qx;
            var r = Math.Pow(a * a - sx, 1.0 / 6.0);
            var theta = Math.Atan(Math.Sqrt(-sx) / a);

            var phyx = 0.0;
            if (3.0 / (2.0 * alpha) < 0.0)
            {
                // NOTE: 元記事ではxの比較だったが、おそらくq(x)が正しい
                if (qx > 0.0)
                {
                    phyx = Math.PI * 2.0 - theta;
                }
                else
                {
                    phyx = Math.PI - theta;
                }
            }
            else if (delta < 0.0)
            {
                if (qx > 0.0)
                {
                    phyx = Math.PI * 2.0 + theta;
                }
                else
                {
                    phyx = theta - Math.PI * 3.0;
                }
            }
            else
            {
                if (qx > 0.0)
                {
                    phyx = theta;
                }
                else
                {
                    phyx = Math.PI + theta;
                }
            }

            return 2.0 * r * Math.Cos(phyx / 3.0) - delta;
        }
    }
}

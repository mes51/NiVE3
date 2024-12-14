using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Expression.Utility
{
    static class ExpressionVectorMath
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public static double[] add(double[] a, double[] b)
        {
            return [..a.Zip(b, (av, bv) => av + bv)];
        }

        [ExpressionPublicMember]
        public static double[] sub(double[] a, double[] b)
        {
            return [..a.Zip(b, (av, bv) => av - bv)];
        }

        [ExpressionPublicMember]
        public static double[] mul(double[] a, double[] b)
        {
            return [..a.Zip(b, (av, bv) => av * bv)];
        }

        [ExpressionPublicMember]
        public static double[] mul(double[] a, double scalar)
        {
            return [..a.Select(av => av * scalar)];
        }

        [ExpressionPublicMember]
        public static double[] div(double[] a, double[] b)
        {
            return [..a.Zip(b, (av, bv) => av / bv)];
        }

        [ExpressionPublicMember]
        public static double[] div(double[] a, double scalar)
        {
            return [..a.Select(av => av / scalar)];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}

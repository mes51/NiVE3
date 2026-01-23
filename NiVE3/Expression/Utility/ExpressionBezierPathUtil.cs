using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using NiVE3.Extension;
using NiVE3.Numerics;
using NiVE3.Plugin.ValueObject;
using NiVE3.Property.Types;

namespace NiVE3.Expression.Utility
{
    static class ExpressionBezierPathUtil
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public static double[]? tracePath(object pathData, double t)
        {
            if (!BezierPathPropertyType.Instance.TryConvertFromExpressionValue(pathData, null, out var parsedValue) || parsedValue is not BezierPath path || path.IsInvalid())
            {
                return null;
            }

            var simplePath = path.BuildPath()?.Flatten()?.First();
            if (simplePath == null || simplePath.Points.Length < 1)
            {
                return null;
            }

            if (simplePath.Points.Length < 2 || t <= 0.0)
            {
                return [simplePath.Points.Span[0].X, simplePath.Points.Span[0].Y];
            }
            else if (t >= 1.0)
            {
                return [simplePath.Points.Span[^1].X, simplePath.Points.Span[^1].Y];
            }

            var points = simplePath.Points.ToArray();
            var nextPoints = points.Skip(1);
            if (path.IsClosed)
            {
                nextPoints = nextPoints.Append(points[0]);
            }

            var lines = points.Zip(nextPoints, (f, s) =>
            {
                var fv = (Vector2d)(Vector2)f;
                var sv = (Vector2d)(Vector2)s;
                var diff = sv - fv;
                return (fv, sv, (sv - fv).Length());
            }).ToArray();

            var currentPos = lines.Aggregate(0.0, (m, v) => v.Item3 + m) * t;
            foreach (var (fv, sv, length) in lines)
            {
                if (length < currentPos)
                {
                    currentPos -= length;
                }
                else
                {
                    var p = Vector2d.Lerp(fv, sv, currentPos / length);
                    return [p.X, p.Y];
                }
            }

            return null;
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}

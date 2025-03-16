using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Internal.Util;

namespace NiVE3.Plugin.ValueObject
{
    public class BezierPath
    {
        public static readonly BezierPath Empty = new BezierPath(Vector2d.Zero, [], false);

        public Vector2d BeginPoint { get; }

        public ImmutableArray<BeziePoint> Points { get; }

        public bool IsClosed { get; }

        public BezierPath(Vector2d beginPoint, IEnumerable<BeziePoint> points, bool isClosed)
        {
            BeginPoint = beginPoint;
            Points = [..points];
            IsClosed = isClosed;
        }

        public BezierPath(Vector2d beginPoint, BeziePoint[] points, bool isClosed)
        {
            BeginPoint = beginPoint;
            Points = ImmutableArray.Create(points);
            IsClosed = isClosed;
        }

        public bool IsEmpty()
        {
            return Points.Length < 1;
        }

        public object? Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(BeginPoint), VectorSerializer.Serialize(BeginPoint) },
                { nameof(Points), Points.Select(p => p.Serialize()).ToArray() },
                { nameof(IsClosed), IsClosed }
            };
        }

        public static BezierPath? Deserialize(object? serializedValue)
        {
            if (serializedValue is not IDictionary<string, object?> dictionary ||
                !dictionary.TryGetValue(nameof(BeginPoint), out var beginPointValue) ||
                !dictionary.TryGetValue(nameof(Points), out var pointsValue) ||
                !dictionary.TryGetValue(nameof(IsClosed), out var isClosedValue) ||
                pointsValue is not object[] pointsValueArray ||
                !VectorSerializer.TryDeserializeVector2d(beginPointValue, out var beginPoint) ||
                isClosedValue is not bool isClosed)
            {
                return null;
            }

            var points = new List<BeziePoint>();
            foreach (var p in pointsValueArray)
            {
                var bezierPoint = BeziePoint.Deserialize(p);
                if (bezierPoint == null)
                {
                    return null;
                }

                points.Add(bezierPoint);
            }

            return new BezierPath(beginPoint, points, isClosed);
        }

        public BezierPath Transform(in Matrix3x2 matrix)
        {
            var matrixD = new Matrix3x2d(matrix);

            var newPoints = new BeziePoint[Points.Length];
            for (var i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                if (p.IsLinear)
                {
                    newPoints[i] = new BeziePoint(Vector2d.Zero, Vector2d.Zero, matrixD.Transform(p.EndPoint), true);
                }
                else
                {
                    newPoints[i] = new BeziePoint(matrixD.Transform(p.ControlPoint1), matrixD.Transform(p.ControlPoint2), matrixD.Transform(p.EndPoint), false);
                }
            }
            return new BezierPath(matrixD.Transform(BeginPoint), newPoints, IsClosed);
        }
    }

    public record BeziePoint(Vector2d ControlPoint1, Vector2d ControlPoint2, Vector2d EndPoint, bool IsLinear)
    {
        public object? Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(ControlPoint1), VectorSerializer.Serialize(ControlPoint1) },
                { nameof(ControlPoint2), VectorSerializer.Serialize(ControlPoint2) },
                { nameof(EndPoint), VectorSerializer.Serialize(EndPoint) },
                { nameof(IsLinear), IsLinear }
            };
        }

        public static BeziePoint? Deserialize(object? serializedValue)
        {
            if (serializedValue is BeziePoint point)
            {
                return point;
            }
            else if (serializedValue is IDictionary<string, object?> dictionary &&
                     dictionary.TryGetValue(nameof(ControlPoint1), out var controlPoint1Value) &&
                     dictionary.TryGetValue(nameof(ControlPoint2), out var controlPoint2Value) &&
                     dictionary.TryGetValue(nameof(EndPoint), out var endPointValue) &&
                     dictionary.TryGetValue(nameof(IsLinear), out var isLinearValue) &&
                     VectorSerializer.TryDeserializeVector2d(controlPoint1Value, out var controlPoint1) &&
                     VectorSerializer.TryDeserializeVector2d(controlPoint2Value, out var controlPoint2) &&
                     VectorSerializer.TryDeserializeVector2d(endPointValue, out var endPoint) &&
                     isLinearValue is bool isLinear)
            {
                return new BeziePoint(controlPoint1, controlPoint2, endPoint, isLinear);
            }

            return null;
        }
    }

    file readonly ref struct Matrix3x2d
    {
        public readonly Vector128<double> RowX;

        public readonly Vector128<double> RowY;

        public readonly Vector128<double> RowZ;

        public Matrix3x2d(in Matrix3x2 matrix)
        {
            RowX = Vector128.Create(matrix.M11, matrix.M12);

            RowY = Vector128.Create(matrix.M21, matrix.M22);

            RowZ = Vector128.Create(matrix.M31, matrix.M32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2d Transform(in Vector2d v)
        {
            var result = RowX * v.X;
            if (Fma.IsSupported)
            {
                result = Fma.MultiplyAdd(RowY, Vector128.Create(v.Y), result);
            }
            else
            {
                result = RowY * v.Y + result;
            }

            return Unsafe.BitCast<Vector128<double>, Vector2d>(result + RowZ);
        }
    }
}

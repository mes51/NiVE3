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
        public static readonly BezierPath Empty = new BezierPath(new Vector2d(double.NaN), ImmutableArray<BezierPoint>.Empty, false);

        public BezierPoint BeginPoint { get; }

        public ImmutableArray<BezierPoint> Points { get; }

        public bool IsClosed { get; }

        public BezierPath(Vector2d beginPoint, IEnumerable<BezierPoint> points, bool isClosed) : this(new BezierPoint(Vector2d.Zero, Vector2d.Zero, beginPoint, true, false), ImmutableArray.Create([..points]), isClosed) { }

        public BezierPath(Vector2d beginPoint, BezierPoint[] points, bool isClosed) : this(new BezierPoint(Vector2d.Zero, Vector2d.Zero, beginPoint, true, false), ImmutableArray.Create(points), isClosed) { }

        public BezierPath(BezierPoint beginPoint, IEnumerable<BezierPoint> points, bool isClosed) : this(beginPoint, ImmutableArray.Create([..points]), isClosed) { }

        public BezierPath(BezierPoint beginPoint, BezierPoint[] points, bool isClosed) : this(beginPoint, ImmutableArray.Create(points), isClosed) { }

        private BezierPath(BezierPoint beginPoint, ImmutableArray<BezierPoint> points, bool isClosed)
        {
            BeginPoint = beginPoint;
            Points = points;
            IsClosed = isClosed;
        }

        public bool IsEmpty()
        {
            return Points.Length < 1;
        }

        public bool IsInvalid()
        {
            return BeginPoint.EndPoint.IsNaN();
        }

        public object? Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(BeginPoint), BeginPoint.Serialize() },
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

            var points = new List<BezierPoint>();
            foreach (var p in pointsValueArray)
            {
                var bezierPoint = BezierPoint.Deserialize(p);
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

            var newPoints = new BezierPoint[Points.Length];
            for (var i = 0; i < Points.Length; i++)
            {
                newPoints[i] = Points[i].Transform(matrixD);
            }
            return new BezierPath(BeginPoint.Transform(matrixD), newPoints, IsClosed);
        }

        public BezierPath ClosePath()
        {
            if (IsClosed)
            {
                return this;
            }
            else
            {
                return new BezierPath(BeginPoint, Points, true);
            }
        }
    }

    public record BezierPoint(Vector2d PrevControlPoint, Vector2d NextControlPoint, Vector2d EndPoint, bool IsLinear, bool IsFreeControlPoint)
    {
        public object Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(PrevControlPoint), VectorSerializer.Serialize(PrevControlPoint) },
                { nameof(NextControlPoint), VectorSerializer.Serialize(NextControlPoint) },
                { nameof(EndPoint), VectorSerializer.Serialize(EndPoint) },
                { nameof(IsLinear), IsLinear },
                { nameof(IsFreeControlPoint), IsFreeControlPoint }
            };
        }

        public BezierPoint Transform(in Matrix3x2 matrix)
        {
            return Transform(new Matrix3x2d(matrix));
        }

        internal BezierPoint Transform(in Matrix3x2d matrix)
        {
            if (IsLinear)
            {
                return new BezierPoint(Vector2d.Zero, Vector2d.Zero, matrix.Transform(EndPoint), true, false);
            }
            else
            {
                return new BezierPoint(matrix.Transform(PrevControlPoint), matrix.Transform(NextControlPoint), matrix.Transform(EndPoint), true, IsFreeControlPoint);
            }
        }

        public static BezierPoint? Deserialize(object? serializedValue)
        {
            if (serializedValue is BezierPoint point)
            {
                return point;
            }
            else if (serializedValue is IDictionary<string, object?> dictionary &&
                     dictionary.TryGetValue(nameof(PrevControlPoint), out var controlPoint1Value) &&
                     dictionary.TryGetValue(nameof(NextControlPoint), out var controlPoint2Value) &&
                     dictionary.TryGetValue(nameof(EndPoint), out var endPointValue) &&
                     dictionary.TryGetValue(nameof(IsLinear), out var isLinearValue) &&
                     dictionary.TryGetValue(nameof(IsFreeControlPoint), out var isFreeControlPointValue) &&
                     VectorSerializer.TryDeserializeVector2d(controlPoint1Value, out var controlPoint1) &&
                     VectorSerializer.TryDeserializeVector2d(controlPoint2Value, out var controlPoint2) &&
                     VectorSerializer.TryDeserializeVector2d(endPointValue, out var endPoint) &&
                     isLinearValue is bool isLinear &&
                     isFreeControlPointValue is bool isFreeControlPoint)
            {
                return new BezierPoint(controlPoint1, controlPoint2, endPoint, isLinear, isFreeControlPoint);
            }

            return null;
        }
    }

    readonly ref struct Matrix3x2d
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

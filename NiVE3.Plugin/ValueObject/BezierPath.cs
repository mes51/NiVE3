using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                { nameof(BeginPoint), VectorSerDe.Serialize(BeginPoint) },
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
                !VectorSerDe.TryDeserializeVector2d(beginPointValue, out var beginPoint) ||
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
    }

    public record BeziePoint(Vector2d ControlPoint1, Vector2d ControlPoint2, Vector2d EndPoint, bool IsLinear)
    {
        public object? Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(ControlPoint1), VectorSerDe.Serialize(ControlPoint1) },
                { nameof(ControlPoint2), VectorSerDe.Serialize(ControlPoint2) },
                { nameof(EndPoint), VectorSerDe.Serialize(EndPoint) },
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
                     VectorSerDe.TryDeserializeVector2d(controlPoint1Value, out var controlPoint1) &&
                     VectorSerDe.TryDeserializeVector2d(controlPoint2Value, out var controlPoint2) &&
                     VectorSerDe.TryDeserializeVector2d(endPointValue, out var endPoint) &&
                     isLinearValue is bool isLinear)
            {
                return new BeziePoint(controlPoint1, controlPoint2, endPoint, isLinear);
            }

            return null;
        }
    }
}

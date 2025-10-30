using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Property.Types
{
    class BezierPathPropertyType : IPropertyType
    {
        public static readonly BezierPathPropertyType Instance = new BezierPathPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear;

        public bool IsSupportedExpression => true;

        public bool IsSupportedGraphEditor => false;

        private BezierPathPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= time);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            else if (baseKeyFrameIndex >= keyFrames.Count - 1)
            {
                return keyFrames[baseKeyFrameIndex].Value;
            }
            var keyFrame1 = keyFrames[baseKeyFrameIndex];
            var keyFrame2 = keyFrames[baseKeyFrameIndex + 1];

            if (keyFrame1.Time == time)
            {
                return keyFrame1.Value;
            }
            else if (keyFrame2.Time == time)
            {
                return keyFrame2.Value;
            }

            switch (keyFrame1.InterpolationType)
            {
                case InterpolationType.Linear:
                    {
                        var prevValue = (keyFrame1.Value as BezierPath) ?? BezierPath.Empty;
                        var nextValue = (keyFrame2.Value as BezierPath) ?? BezierPath.Empty;
                        var tv = (float)(double)((time - keyFrame1.Time) / (keyFrame2.Time - keyFrame1.Time));

                        var beginPoint = Vector2d.Lerp(prevValue.BeginPoint, nextValue.BeginPoint, tv);
                        return new BezierPath(
                            beginPoint,
                            InterpolatePoints(beginPoint, prevValue.Points, nextValue.Points, tv),
                            prevValue.IsClosed & nextValue.IsClosed
                        );
                    }
                default:
                    return keyFrame1.Value;
            }
        }

        public object? SerializeValue(object? value)
        {
            return (value as BezierPath)?.Serialize();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return BezierPath.Deserialize(serializedValue);
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is not BezierPath path)
            {
                return [];
            }

            var vector2dSize = sizeof(double) * 2;
            var beziePointSize = 3 * vector2dSize + sizeof(bool);
            var hashBase = new List<byte>(vector2dSize + beziePointSize * path.Points.Length + sizeof(bool));

            hashBase.AddRange(BitConverter.GetBytes(path.BeginPoint.X));
            hashBase.AddRange(BitConverter.GetBytes(path.BeginPoint.Y));
            foreach (var p in path.Points)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.ControlPoint1.X));
                hashBase.AddRange(BitConverter.GetBytes(p.ControlPoint1.Y));
                hashBase.AddRange(BitConverter.GetBytes(p.ControlPoint2.X));
                hashBase.AddRange(BitConverter.GetBytes(p.ControlPoint2.Y));
                hashBase.AddRange(BitConverter.GetBytes(p.EndPoint.X));
                hashBase.AddRange(BitConverter.GetBytes(p.EndPoint.Y));
                hashBase.AddRange(BitConverter.GetBytes(p.IsLinear));
            }
            hashBase.AddRange(BitConverter.GetBytes(path.IsClosed));

            return hashBase.ToArray();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            if (value is not BezierPath path)
            {
                return null;
            }

            var points = path.Points;
            var convertedPoints = new Dictionary<string, object>[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var p = points[i];
                convertedPoints[i] = new Dictionary<string, object>
                {
                    { "controlPoint1", ConvertToExpressionValueVector2d(p.ControlPoint1) },
                    { "controlPoint2", ConvertToExpressionValueVector2d(p.ControlPoint2) },
                    { "endPoint", ConvertToExpressionValueVector2d(p.EndPoint) },
                    { "isLinear", p.IsLinear }
                };
            }
            return new Dictionary<string, object>
            {
                { "beginPoint", ConvertToExpressionValueVector2d(path.BeginPoint) },
                { "points", convertedPoints },
                { "isClosed", path.IsClosed }
            };
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            if (expressionValue is not IDictionary<string, object> dictionary ||
                !dictionary.TryGetValue("beginPoint", out var beginPointValue) ||
                !dictionary.TryGetValue("points", out var pointsValue) ||
                !dictionary.TryGetValue("isClosed", out var isClosedValue) ||
                !TryConvertFromExpressionValueVector2d(beginPointValue, out var beginPoint) ||
                pointsValue is not object[] pointsArrayValue ||
                isClosedValue is not bool isClosed)
            {
                value = null;
                return false;
            }

            var points = new List<BeziePoint>(pointsArrayValue.Length);
            foreach (var pv in pointsArrayValue)
            {
                if (pv is not IDictionary<string, object> pointDictionary ||
                    !pointDictionary.TryGetValue("endPoint", out var endPointValue) ||
                    !TryConvertFromExpressionValueVector2d(endPointValue, out var endPoint))
                {
                    value = null;
                    return false;
                }

                pointDictionary.TryGetValue("isLinear", out var isLinearValue);
                var isLinear = (bool)(isLinearValue ?? false);

                if (isLinear ||
                    !pointDictionary.TryGetValue("controlPoint1", out var controlPoint1Value) ||
                    !pointDictionary.TryGetValue("controlPoint2", out var controlPoint2Value) ||
                    !TryConvertFromExpressionValueVector2d(controlPoint1Value, out var controlPoint1) ||
                    !TryConvertFromExpressionValueVector2d(controlPoint2Value, out var controlPoint2))
                {
                    points.Add(new BeziePoint(Vector2d.Zero, Vector2d.Zero, endPoint, true));
                }
                else
                {
                    points.Add(new BeziePoint(controlPoint1, controlPoint2, endPoint, false));
                }
            }

            value = new BezierPath(beginPoint, points, isClosed);
            return true;
        }

        static BeziePoint[] InterpolatePoints(in Vector2d beginPoint, ImmutableArray<BeziePoint> prevPoints, ImmutableArray<BeziePoint> nextPoints, double t)
        {
            if (t <= 0.0)
            {
                return [..prevPoints];
            }
            else if (t >= 1.0F)
            {
                return [..nextPoints];
            }

            var currentPoints = new List<BeziePoint>();
            var minPointCount = Math.Min(prevPoints.Length, nextPoints.Length);

            var prevBeginPoint = beginPoint;
            var nextBeginPoint = beginPoint;
            for (var i = 0; i < minPointCount; i++)
            {
                var prevPoint = prevPoints[i];
                var nextPoint = nextPoints[i];
                var prevControlPoint1 = prevPoint.IsLinear ? prevBeginPoint : prevPoint.ControlPoint1;
                var prevControlPoint2 = prevPoint.IsLinear ? prevBeginPoint : prevPoint.ControlPoint2;
                var nextControlPoint1 = nextPoint.IsLinear ? nextBeginPoint : nextPoint.ControlPoint1;
                var nextControlPoint2 = nextPoint.IsLinear ? nextBeginPoint : nextPoint.ControlPoint2;
                currentPoints.Add(new BeziePoint(
                    Vector2d.Lerp(prevControlPoint1, nextControlPoint1, t),
                    Vector2d.Lerp(prevControlPoint2, nextControlPoint2, t),
                    Vector2d.Lerp(prevPoint.EndPoint, nextPoint.EndPoint, t),
                    prevPoint.IsLinear && nextPoint.IsLinear
                ));

                prevBeginPoint = prevPoint.EndPoint;
                nextBeginPoint = nextPoint.EndPoint;
            }
            if (prevPoints.Length > nextPoints.Length)
            {
                var lastNextPoint = nextPoints[^1];
                var lastNextControlPoint1 = lastNextPoint.IsLinear ? nextBeginPoint : lastNextPoint.ControlPoint1;
                var lastNextControlPoint2 = lastNextPoint.IsLinear ? nextBeginPoint : lastNextPoint.ControlPoint2;
                for (var i = minPointCount; i < prevPoints.Length; i++)
                {
                    var prevPoint = prevPoints[i];
                    var prevControlPoint1 = prevPoint.IsLinear ? prevBeginPoint : prevPoint.ControlPoint1;
                    var prevControlPoint2 = prevPoint.IsLinear ? prevBeginPoint : prevPoint.ControlPoint2;
                    currentPoints.Add(new BeziePoint(
                        Vector2d.Lerp(prevControlPoint1, lastNextControlPoint1, t),
                        Vector2d.Lerp(prevControlPoint2, lastNextControlPoint2, t),
                        Vector2d.Lerp(prevPoint.EndPoint, lastNextPoint.EndPoint, t),
                        prevPoint.IsLinear && lastNextPoint.IsLinear
                    ));

                    prevBeginPoint = prevPoint.EndPoint;
                }
            }
            else if (nextPoints.Length > prevPoints.Length)
            {
                var lastPrevPoint = prevPoints[^1];
                var lastPrevControlPoint1 = lastPrevPoint.ControlPoint1;
                var lastPrevControlPoint2 = lastPrevPoint.ControlPoint2;
                for (var i = minPointCount; i < nextPoints.Length; i++)
                {
                    var nextPoint = nextPoints[i];
                    var nextControlPoint1 = nextPoint.IsLinear ? nextBeginPoint : nextPoint.ControlPoint1;
                    var nextControlPoint2 = nextPoint.IsLinear ? nextBeginPoint : nextPoint.ControlPoint2;
                    currentPoints.Add(new BeziePoint(
                        Vector2d.Lerp(lastPrevControlPoint1, nextControlPoint1, t),
                        Vector2d.Lerp(lastPrevControlPoint2, nextControlPoint2, t),
                        Vector2d.Lerp(lastPrevPoint.EndPoint, nextPoint.EndPoint, t),
                        lastPrevPoint.IsLinear && nextPoint.IsLinear
                    ));

                    nextBeginPoint = nextPoint.EndPoint;
                }
            }

            return [..currentPoints];
        }

        static Dictionary<string, object> ConvertToExpressionValueVector2d(in Vector2d v)
        {
            return new Dictionary<string, object>
            {
                { "x", v.X },
                { "y", v.Y }
            };
        }

        static bool TryConvertFromExpressionValueVector2d(object? value, out Vector2d v)
        {
            if (value is IDictionary<string, object> dictionary &&
                dictionary.TryGetValue("x", out var xValue) &&
                dictionary.TryGetValue("y", out var yValue))
            {
                v = new Vector2d(Convert.ToDouble(xValue), Convert.ToDouble(yValue));
                return true;
            }
            else
            {
                v = Vector2d.Zero;
                return false;
            }
        }
    }
}

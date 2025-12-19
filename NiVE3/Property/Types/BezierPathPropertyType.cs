using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jint.Native;
using NiVE3.Numerics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Property.Types
{
    class BezierPathPropertyType : IPropertyType
    {
        const byte ValidPathHashBaseFlag = 1;

        static readonly byte[] InvalidHashBase = [0];

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

                        if (prevValue.IsInvalid() && nextValue.IsInvalid())
                        {
                            return BezierPath.Empty;
                        }

                        BezierPoint beginPoint;
                        if (prevValue.IsInvalid() || tv >= 1.0F)
                        {
                            beginPoint = nextValue.BeginPoint;
                        }
                        else if (nextValue.IsInvalid() || tv <= 0.0F)
                        {
                            beginPoint = prevValue.BeginPoint;
                        }
                        else
                        {
                            beginPoint = InterpolatePoint(prevValue.BeginPoint, nextValue.BeginPoint, tv);
                        }

                        return new BezierPath(
                            beginPoint,
                            InterpolatePoints(prevValue.Points, nextValue.Points, tv),
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

            if (path.IsInvalid())
            {
                return InvalidHashBase;
            }

            const int BeziePointSize = 3 * sizeof(double) * 2 + sizeof(bool);
            var hashBase = new List<byte>(1 + BeziePointSize * (path.Points.Length + 1) + sizeof(bool))
            {
                ValidPathHashBaseFlag
            };
            foreach (var p in path.Points.Prepend(path.BeginPoint))
            {
                hashBase.AddRange(BitConverter.GetBytes(p.PrevControlPoint.X));
                hashBase.AddRange(BitConverter.GetBytes(p.PrevControlPoint.Y));
                hashBase.AddRange(BitConverter.GetBytes(p.NextControlPoint.X));
                hashBase.AddRange(BitConverter.GetBytes(p.NextControlPoint.Y));
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
                    { "prevControlPoint", ConvertToExpressionValueVector2d(p.PrevControlPoint) },
                    { "nextControlPoint", ConvertToExpressionValueVector2d(p.NextControlPoint) },
                    { "endPoint", ConvertToExpressionValueVector2d(p.EndPoint) },
                    { "isLinear", p.IsLinear }
                };
            }
            return new Dictionary<string, object>
            {
                { "beginPoint", ConvertToExpressionPoint(path.BeginPoint) },
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
                beginPointValue is not IDictionary<string, object> beginPointDictionary ||
                pointsValue is not object[] pointsArrayValue ||
                isClosedValue is not bool isClosed)
            {
                value = null;
                return false;
            }

            var beginPoint = ConvertFromExpressionPoint(beginPointDictionary);
            if (beginPoint == null)
            {
                value = null;
                return false;
            }

            var points = new List<BezierPoint>(pointsArrayValue.Length);
            foreach (var pv in pointsArrayValue)
            {
                if (pv is not IDictionary<string, object> pointDictionary)
                {
                    value = null;
                    return false;
                }

                var bezierPoint = ConvertFromExpressionPoint(pointDictionary);
                if (bezierPoint == null)
                {
                    value = null;
                    return false;
                }

                points.Add(bezierPoint);
            }

            value = new BezierPath(beginPoint, points, isClosed);
            return true;
        }

        static Dictionary<string, object> ConvertToExpressionPoint(BezierPoint point)
        {
            return new Dictionary<string, object>
            {
                { "prevControlPoint", ConvertToExpressionValueVector2d(point.PrevControlPoint) },
                { "nextControlPoint", ConvertToExpressionValueVector2d(point.NextControlPoint) },
                { "endPoint", ConvertToExpressionValueVector2d(point.EndPoint) },
                { "isLinear", point.IsLinear }
            };
        }

        static BezierPoint? ConvertFromExpressionPoint(IDictionary<string, object> pointDictionary)
        {
            if (!pointDictionary.TryGetValue("endPoint", out var endPointValue) ||
                !TryConvertFromExpressionValueVector2d(endPointValue, out var endPoint))
            {
                return null;
            }

            pointDictionary.TryGetValue("isLinear", out var isLinearValue);
            var isLinear = (bool)(isLinearValue ?? false);

            if (isLinear ||
                !pointDictionary.TryGetValue("prevControlPoint", out var controlPoint1Value) ||
                !pointDictionary.TryGetValue("nextControlPoint", out var controlPoint2Value) ||
                !TryConvertFromExpressionValueVector2d(controlPoint1Value, out var controlPoint1) ||
                !TryConvertFromExpressionValueVector2d(controlPoint2Value, out var controlPoint2))
            {
                return new BezierPoint(Vector2d.Zero, Vector2d.Zero, endPoint, true, false);
            }
            else
            {
                return new BezierPoint(controlPoint1, controlPoint2, endPoint, false, false);
            }
        }

        static BezierPoint InterpolatePoint(BezierPoint prevPoint, BezierPoint nextPoint, double t)
        {
            if (t <= 0.0)
            {
                return prevPoint;
            }
            else if (t >= 1.0)
            {
                return nextPoint;
            }
            else
            {
                if (prevPoint.IsLinear && nextPoint.IsLinear)
                {
                    return new BezierPoint(Vector2d.Zero, Vector2d.Zero, Vector2d.Lerp(prevPoint.EndPoint, nextPoint.EndPoint, t), true, false);
                }
                else
                {
                    return new BezierPoint(
                        Vector2d.Lerp(prevPoint.PrevControlPoint, nextPoint.PrevControlPoint, t),
                        Vector2d.Lerp(prevPoint.NextControlPoint, nextPoint.NextControlPoint, t),
                        Vector2d.Lerp(prevPoint.EndPoint, nextPoint.EndPoint, t),
                        true,
                        false
                    );
                }
            }
        }

        static BezierPoint[] InterpolatePoints(ImmutableArray<BezierPoint> prevPoints, ImmutableArray<BezierPoint> nextPoints, double t)
        {
            if (t <= 0.0)
            {
                return [..prevPoints];
            }
            else if (t >= 1.0F)
            {
                return [..nextPoints];
            }

            var currentPoints = new List<BezierPoint>();
            var minPointCount = Math.Min(prevPoints.Length, nextPoints.Length);

            for (var i = 0; i < minPointCount; i++)
            {
                currentPoints.Add(InterpolatePoint(prevPoints[i], nextPoints[i], t));
            }
            if (prevPoints.Length > nextPoints.Length)
            {
                var lastNextPoint = nextPoints[^1];
                for (var i = minPointCount; i < prevPoints.Length; i++)
                {
                    currentPoints.Add(InterpolatePoint(prevPoints[i], lastNextPoint, t));
                }
            }
            else if (nextPoints.Length > prevPoints.Length)
            {
                var lastPrevPoint = prevPoints[^1];
                for (var i = minPointCount; i < nextPoints.Length; i++)
                {
                    currentPoints.Add(InterpolatePoint(lastPrevPoint, nextPoints[i], t));
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

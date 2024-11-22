using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Property.Types
{
    class ToneCurvePropertyType : IPropertyType
    {
        public static readonly ToneCurvePropertyType Instance = new ToneCurvePropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear;

        public bool IsSupportedExpression => false;

        private ToneCurvePropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= t);
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

            if (keyFrame1.Time == t)
            {
                return keyFrame1.Value;
            }
            else if (keyFrame2.Time == t)
            {
                return keyFrame2.Value;
            }

            switch(keyFrame1.InterpolationType)
            {
                case InterpolationType.Linear:
                    {
                        var prevValue = (keyFrame1.Value as ToneCurveParameters) ?? ToneCurveParameters.Empty;
                        var nextValue = (keyFrame2.Value as ToneCurveParameters) ?? ToneCurveParameters.Empty;
                        var tv = (float)((t - keyFrame1.Time) / (keyFrame2.Time - keyFrame1.Time));

                        return new ToneCurveParameters(
                            InterpolatePoints(prevValue.Rgb, nextValue.Rgb, tv),
                            InterpolatePoints(prevValue.R, nextValue.R, tv),
                            InterpolatePoints(prevValue.G, nextValue.G, tv),
                            InterpolatePoints(prevValue.B, nextValue.B, tv),
                            InterpolatePoints(prevValue.A, nextValue.A, tv)
                        );
                    }
                default:
                    return keyFrame1.Value;
            }

            throw new NotImplementedException();
        }

        public object? SerializeValue(object? value)
        {
            return (value as ToneCurveParameters)?.Serialize();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return ToneCurveParameters.Deserialize(serializedValue);
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is not ToneCurveParameters parameters)
            {
                return [];
            }

            var hashBase = new List<byte>((parameters.Rgb.Length + parameters.R.Length + parameters.G.Length + parameters.B.Length + parameters.A.Length) * sizeof(float) * 2);
            foreach (var p in parameters.Rgb)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.InValue));
                hashBase.AddRange(BitConverter.GetBytes(p.OutValue));
            }
            foreach (var p in parameters.R)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.InValue));
                hashBase.AddRange(BitConverter.GetBytes(p.OutValue));
            }
            foreach (var p in parameters.G)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.InValue));
                hashBase.AddRange(BitConverter.GetBytes(p.OutValue));
            }
            foreach (var p in parameters.B)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.InValue));
                hashBase.AddRange(BitConverter.GetBytes(p.OutValue));
            }
            foreach (var p in parameters.A)
            {
                hashBase.AddRange(BitConverter.GetBytes(p.InValue));
                hashBase.AddRange(BitConverter.GetBytes(p.OutValue));
            }

            return hashBase.ToArray();
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            if (value is not ToneCurveParameters parameters)
            {
                return null;
            }

            return new Dictionary<string, object?>
            {
                { "rgb", parameters.Rgb.Select(p => new Dictionary<string, object>{ { "inValue", p.InValue }, { "outValue", p.OutValue } }) },
                { "r", parameters.R.Select(p => new Dictionary<string, object>{ { "inValue", p.InValue }, { "outValue", p.OutValue } }) },
                { "g", parameters.G.Select(p => new Dictionary<string, object>{ { "inValue", p.InValue }, { "outValue", p.OutValue } }) },
                { "b", parameters.B.Select(p => new Dictionary<string, object>{ { "inValue", p.InValue }, { "outValue", p.OutValue } }) },
                { "a", parameters.A.Select(p => new Dictionary<string, object>{ { "inValue", p.InValue }, { "outValue", p.OutValue } }) },
            };
        }

        static IReadOnlyList<ToneCurvePoint> InterpolatePoints(IReadOnlyList<ToneCurvePoint> prevPoints, IReadOnlyList<ToneCurvePoint> nextPoints, float t)
        {
            var currentPoints = new List<ToneCurvePoint>();
            var minPointCount = Math.Min(prevPoints.Count, nextPoints.Count);

            for (var i = 0; i < minPointCount; i++)
            {
                currentPoints.Add(new ToneCurvePoint(float.Lerp(prevPoints[i].InValue, nextPoints[i].InValue, t), float.Lerp(prevPoints[i].OutValue, nextPoints[i].OutValue, t)));
            }
            if (prevPoints.Count > nextPoints.Count)
            {
                var lastPoint = nextPoints[^1];
                for (var i = minPointCount; i < prevPoints.Count; i++)
                {
                    currentPoints.Add(new ToneCurvePoint(float.Lerp(prevPoints[i].InValue, lastPoint.InValue, t), float.Lerp(prevPoints[i].OutValue, lastPoint.OutValue, t)));
                }
            }
            else if (nextPoints.Count > currentPoints.Count)
            {
                var lastPoint = prevPoints[^1];
                for (var i = minPointCount; i < nextPoints.Count; i++)
                {
                    currentPoints.Add(new ToneCurvePoint(float.Lerp(lastPoint.InValue, nextPoints[i].InValue, t), float.Lerp(lastPoint.OutValue, nextPoints[i].OutValue, t)));
                }
            }

            return currentPoints;
        }
    }
}

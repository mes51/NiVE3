using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class DoublePropertyType : IPropertyType
    {
        static readonly byte[] ZeroHashBase = [..Enumerable.Repeat((byte)0, sizeof(double))];

        public static readonly DoublePropertyType Instance = new DoublePropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear | InterpolationType.CatmullRom;

        public bool IsSupportedExpression => true;

        private DoublePropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.IndexOfLast(k => k.Time <= t);
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
            switch (keyFrames[baseKeyFrameIndex].InterpolationType)
            {
                case InterpolationType.Linear:
                    return Interpolation.Linear((double)keyFrame1.Value!, (double)keyFrame2.Value!, keyFrame1.Time, keyFrame2.Time, t);
                case InterpolationType.CatmullRom:
                    {
                        var keyFrame0 = baseKeyFrameIndex > 0 ? keyFrames[baseKeyFrameIndex - 1] : keyFrame1;
                        var keyFrame3 = baseKeyFrameIndex <= keyFrames.Count - 3 ? keyFrames[baseKeyFrameIndex + 2] : keyFrame2;
                        return Interpolation.CatmullRom((double)keyFrame0.Value!, (double)keyFrame1.Value!, (double)keyFrame2.Value!, (double)keyFrame3.Value!, keyFrame1.Time, keyFrame2.Time, t);
                    }
                default:
                    return keyFrame1.Value;
            }
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return Convert.ToDouble(serializedValue);
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is double v)
            {
                return v.ConvertToSpan();
            }
            else
            {
                return ZeroHashBase;
            }
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            switch (expressionValue)
            {
                case byte v:
                    value = (double)v;
                    return true;
                case sbyte v:
                    value = (double)v;
                    return true;
                case short v:
                    value = (double)v;
                    return true;
                case ushort v:
                    value = (double)v;
                    return true;
                case int v:
                    value = (double)v;
                    return true;
                case uint v:
                    value = (double)v;
                    return true;
                case long v:
                    value = (double)v;
                    return true;
                case ulong v:
                    value = (double)v;
                    return true;
                case float v:
                    value = (double)v;
                    return true;
                case double:
                    value = expressionValue;
                    return true;
                case decimal v:
                    value = (double)v;
                    return true;
                default:
                    value = 0.0;
                    return false;
            }
        }

        public object? ConvertToExpressionValue(object? value)
        {
            return value;
        }
    }
}

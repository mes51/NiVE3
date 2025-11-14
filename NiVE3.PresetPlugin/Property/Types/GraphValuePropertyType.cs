using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Property.Types
{
    class GraphValuePropertyType : IPropertyType
    {
        public static readonly GraphValuePropertyType Instance = new GraphValuePropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear;

        public bool IsSupportedExpression => false;

        private GraphValuePropertyType() { }

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
                        var prevValue = (keyFrame1.Value as GraphValueParameter) ?? GraphValueParameter.Identity;
                        var nextValue = (keyFrame2.Value as GraphValueParameter) ?? GraphValueParameter.Identity;
                        var tv = (float)(double)((time - keyFrame1.Time) / (keyFrame2.Time - keyFrame1.Time));

                        var interpolated = prevValue.Values.Zip(nextValue.Values, (p, n) => float.Lerp(p, n, tv)).ToArray();
                        return new GraphValueParameter(interpolated);
                    }
                default:
                    return keyFrame1.Value;
            }
        }

        public object? SerializeValue(object? value)
        {
            return (value as GraphValueParameter)?.Serialize();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return GraphValueParameter.Deserialize(serializedValue);
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is not GraphValueParameter parameter)
            {
                return [];
            }

            var result = new float[GraphValueParameter.ValueCount];
            parameter.Values.AsSpan().CopyTo(result);

            return MemoryMarshal.Cast<float, byte>(result.AsSpan());
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            if (value is not GraphValueParameter parameter)
            {
                return null;
            }

            var result = new float[GraphValueParameter.ValueCount];
            parameter.Values.AsSpan().CopyTo(result);

            return result;
        }
    }
}

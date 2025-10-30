using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class BooleanPropertyType : IPropertyType
    {
        static readonly byte[] TrueHashBase = [1];

        static readonly byte[] FalseHashBase = [0];

        public static readonly BooleanPropertyType Instance = new BooleanPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => true;

        public bool IsSupportedGraphEditor => false;

        private BooleanPropertyType() { }

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
            return keyFrames[baseKeyFrameIndex].Value;
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return serializedValue;
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            return value is bool b && b ? TrueHashBase : FalseHashBase;
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            if (expressionValue is bool b)
            {
                value = b;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public object? ConvertToExpressionValue(object? value)
        {
            return value;
        }
    }
}

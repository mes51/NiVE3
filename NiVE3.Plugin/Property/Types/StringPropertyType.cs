using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class StringPropertyType : IPropertyType
    {
        static readonly byte[] EmptyHashBase = [];

        public static readonly StringPropertyType Instance = new StringPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => true;

        public bool IsSupportedGraphEditor => false;

        private StringPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, Time time)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= time);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            return keyFrames[Math.Min(baseKeyFrameIndex, keyFrames.Count - 1)].Value;
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return serializedValue as string ?? serializedValue?.ToString() ?? "";
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            return value is string s && s.Length > 0 ? Encoding.UTF8.GetBytes(s) : EmptyHashBase;
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            if (expressionValue is string s)
            {
                value = s;
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

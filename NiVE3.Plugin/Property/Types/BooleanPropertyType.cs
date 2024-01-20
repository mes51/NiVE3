using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class BooleanPropertyType : IPropertyType
    {
        public static readonly BooleanPropertyType Instance = new BooleanPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        private BooleanPropertyType() { }

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
            return keyFrames[baseKeyFrameIndex].Value;
        }

        public bool TryConvertFrom(object otherValue, out object convertedValue)
        {
            if (otherValue is bool v)
            {
                convertedValue = v;
                return true;
            }
            else
            {
                convertedValue = false;
                return false;
            }
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            return serializedValue;
        }
    }
}

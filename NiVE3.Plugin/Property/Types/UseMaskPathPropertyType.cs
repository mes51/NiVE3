using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class UseMaskPathPropertyType : ILayerDependPropertyType
    {
        public static readonly UseMaskPathPropertyType Instance = new UseMaskPathPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => false;

        public bool IsSupportedGraphEditor => false;

        private UseMaskPathPropertyType() { }

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
            if (serializedValue is UseMaskPathTarget target)
            {
                return target;
            }
            else if (serializedValue is IDictionary<string, object> dictionary && dictionary.TryGetValue(nameof(UseMaskPathTarget.MaskId), out var value))
            {
                var maskId = value switch
                {
                    Guid guid => guid,
                    string str => Guid.Parse(str),
                    _ => Guid.Empty
                };

                return new UseMaskPathTarget(maskId);
            }
            else
            {
                return UseMaskPathTarget.Empty;
            }
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is UseMaskPathTarget target)
            {
                return target.MaskId.ConvertToSpan();
            }
            else if (value is Guid maskId)
            {
                return maskId.ConvertToSpan();
            }
            else
            {
                return [];
            }
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, ILayerObject layer, out object? value)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value, ILayerObject layer)
        {
            if (value is UseMaskPathTarget target)
            {
                return layer.GetMask(target.MaskId)?.Name ?? "";
            }
            else
            {
                return null;
            }
        }
    }
}

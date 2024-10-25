using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    internal class UseLayerImagePropertyType : ICompositionDependIPropertyType
    {
        public static readonly UseLayerImagePropertyType Instance = new UseLayerImagePropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => false;

        private UseLayerImagePropertyType() { }

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

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue is UseLayerImageTarget identifier)
            {
                return identifier;
            }
            else if (serializedValue is IDictionary<string, object> dictionary)
            {
                var layerId = dictionary[nameof(UseLayerImageTarget.LayerId)] switch
                {
                    Guid guid => guid,
                    string str => Guid.Parse(str),
                    _ => Guid.Empty
                };

                return new UseLayerImageTarget(layerId, (LayerImageProcessType)(int)dictionary[nameof(UseLayerImageTarget.ImageProcessType)]);
            }
            else
            {
                return UseLayerImageTarget.Empty;
            }
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is UseLayerImageTarget target)
            {
                return (target.LayerId, target.ImageProcessType).ConvertToSpan();
            }
            else if (value is Guid layerId)
            {
                return layerId.ConvertToSpan();
            }
            else
            {
                return [];
            }
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, ICompositionObject composition, out object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value)
        {
            throw new NotImplementedException();
        }

        public object? ConvertToExpressionValue(object? value, ICompositionObject composition)
        {
            if (value is UseLayerImageTarget target)
            {
                return composition.GetLayer(target.LayerId)?.Name ?? "";
            }
            else
            {
                return null;
            }
        }
    }
}

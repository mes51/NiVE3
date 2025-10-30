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
    public class UseLayerAudioPropertyType : IPropertyType
    {
        public static readonly UseLayerAudioPropertyType Instance = new UseLayerAudioPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => false;

        public bool IsSupportedGraphEditor => false;

        private UseLayerAudioPropertyType() { }

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
            if (serializedValue is UseLayerAudioTarget target)
            {
                return target;
            }
            else if (serializedValue is IDictionary<string, object> dictionary && dictionary.TryGetValue(nameof(UseLayerAudioTarget.LayerId), out var value))
            {
                var layerId = value switch
                {
                    Guid guid => guid,
                    string str => Guid.Parse(str),
                    _ => Guid.Empty
                };

                return new UseLayerAudioTarget(layerId, (LayerAudioProcessType)(int)dictionary[nameof(UseLayerAudioTarget.AudioProcessType)]);
            }
            else
            {
                return UseLayerAudioTarget.Empty;
            }
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is UseLayerAudioTarget target)
            {
                return (target.LayerId, target.AudioProcessType).ConvertToSpan();
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
            if (value is UseLayerAudioTarget target)
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

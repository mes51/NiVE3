using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Color;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Property.Types
{
    public class ColorGradientPropertyType : IPropertyType
    {
        public static readonly ColorGradientPropertyType Instance = new ColorGradientPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None | InterpolationType.Linear;

        private ColorGradientPropertyType() { }

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
                    {
                        var prevValue = (ColorGradient)(keyFrame1.Value ?? ColorGradient.Empty);
                        var nextValue = (ColorGradient)(keyFrame2.Value ?? ColorGradient.Empty);
                        var tv = (float)((t - keyFrame1.Time) / (keyFrame2.Time - keyFrame1.Time));

                        var prevColorStops = prevValue.ColorStops;
                        var nextColorStops = nextValue.ColorStops;
                        var currentColorStops = new List<ColorStop>();
                        var minColorStopCount = Math.Min(prevColorStops.Count, nextColorStops.Count);
                        for (var i = 0; i < minColorStopCount; i++)
                        {
                            currentColorStops.Add(
                                new ColorStop(
                                    Vector4.Lerp(prevColorStops[i].Color, nextColorStops[i].Color, tv),
                                    float.Lerp(prevColorStops[i].Position, nextColorStops[i].Position, tv)
                                )
                            );
                        }
                        if (prevColorStops.Count > nextColorStops.Count)
                        {
                            var lastColor = nextColorStops.Last().Color;
                            for (var i = minColorStopCount; i < prevColorStops.Count; i++)
                            {
                                currentColorStops.Add(
                                    new ColorStop(
                                        Vector4.Lerp(prevColorStops[i].Color, lastColor, tv),
                                        float.Lerp(prevColorStops[i].Position, 1.0F, tv)
                                    )
                                );
                            }
                        }
                        else if (nextColorStops.Count > prevColorStops.Count)
                        {
                            var lastColor = prevColorStops.Last().Color;
                            for (var i = minColorStopCount; i < nextColorStops.Count; i++)
                            {
                                currentColorStops.Add(
                                    new ColorStop(
                                        Vector4.Lerp(lastColor, nextColorStops[i].Color, tv),
                                        float.Lerp(1.0F, nextColorStops[i].Position, tv)
                                    )
                                );
                            }
                        }

                        var prevOpacityStops = prevValue.OpacityStops;
                        var nextOpacityStops = nextValue.OpacityStops;
                        var currentOpacityStops = new List<OpacityStop>();
                        var minOpacityStopCount = Math.Min(prevOpacityStops.Count, nextOpacityStops.Count);
                        for (var i = 0; i < minOpacityStopCount; i++)
                        {
                            currentOpacityStops.Add(
                                new OpacityStop(
                                    float.Lerp(prevOpacityStops[i].Opacity, nextOpacityStops[i].Opacity, tv),
                                    float.Lerp(prevOpacityStops[i].Position, nextOpacityStops[i].Position, tv)
                                )
                            );
                        }
                        if (prevOpacityStops.Count > nextOpacityStops.Count)
                        {
                            var lastOpacity = nextOpacityStops.Last().Opacity;
                            for (var i = minOpacityStopCount; i < prevOpacityStops.Count; i++)
                            {
                                currentOpacityStops.Add(
                                    new OpacityStop(
                                        float.Lerp(prevOpacityStops[i].Opacity, lastOpacity, tv),
                                        float.Lerp(prevOpacityStops[i].Position, 1.0F, tv)
                                    )
                                );
                            }
                        }
                        else if (nextOpacityStops.Count > prevOpacityStops.Count)
                        {
                            var lastOpacity = prevOpacityStops.Last().Opacity;
                            for (var i = minOpacityStopCount; i < nextOpacityStops.Count; i++)
                            {
                                currentOpacityStops.Add(
                                    new OpacityStop(
                                        float.Lerp(lastOpacity, nextOpacityStops[i].Opacity, tv),
                                        float.Lerp(1.0F, nextOpacityStops[i].Position, tv)
                                    )
                                );
                            }
                        }

                        return new ColorGradient(currentColorStops, currentOpacityStops);
                    }
                default:
                    return keyFrame1.Value;
            }
        }

        public bool TryConvertFrom(object otherValue, out object convertedValue)
        {
            // TODO: エクスプレッション用の型を用意する
            switch (otherValue)
            {
                case ColorGradient:
                    convertedValue = otherValue;
                    return true;
            }

            convertedValue = new ColorGradient([new ColorStop(Vector4.One, 0.0F)], [new OpacityStop(1.0F, 0.0F)]);
            return false;
        }

        public object? SerializeValue(object? value)
        {
            return (value as ColorGradient)?.Serialize();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue != null)
            {
                return ColorGradient.Deserialize(serializedValue);
            }
            else
            {
                return null;
            }
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is not ColorGradient gradient)
            {
                return [];
            }

            var hashBase = new List<byte>();
            foreach (var colorStop in gradient.ColorStops)
            {
                hashBase.AddRange(BitConverter.GetBytes(colorStop.Position));
                hashBase.AddRange(colorStop.Color.ConvertToSpan());
            }
            foreach (var opacityStop in gradient.OpacityStops)
            {
                hashBase.AddRange(BitConverter.GetBytes(opacityStop.Position));
                hashBase.AddRange(BitConverter.GetBytes(opacityStop.Opacity));
            }

            return hashBase.ToArray();
        }
    }
}

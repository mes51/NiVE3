using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image.Color;
using NiVE3.Shape;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Util
{
    static class GpuGradientColor
    {
        public static (ReadOnlyBuffer<Float3> colorValues, ReadOnlyBuffer<float> colorPositions, ReadOnlyBuffer<float> opacityValues, ReadOnlyBuffer<float> opacityPositions) CopyToGPUGradientBuffers(GraphicsDevice device, ColorGradient colorGradient, bool useOkLabInterpolation)
        {
            var colorStops = colorGradient.ColorStops;
            using var colorValuesUploadBuffer = device.AllocateUploadBuffer<Float3>(Math.Max(colorStops.Count, 1));
            using var colorPositionsUploadBuffer = device.AllocateUploadBuffer<float>(colorValuesUploadBuffer.Length);

            if (colorStops.Count < 1)
            {
                colorValuesUploadBuffer.Span[0] = Float3.One;
                colorPositionsUploadBuffer.Span[0] = 0.0F;
            }
            else
            {
                var colorValueSpan = colorValuesUploadBuffer.Span;
                if (useOkLabInterpolation)
                {
                    for (var i = 0; i < colorStops.Count; i++)
                    {
                        var oklab = OkLab.FromRgb(colorStops[i].Color);
                        colorValuesUploadBuffer.Span[i] = (Float3)Unsafe.As<OkLab, Vector4>(ref oklab).AsVector3();
                    }
                }
                else
                {
                    for (var i = 0; i < colorStops.Count; i++)
                    {
                        colorValueSpan[i] = (Float3)colorStops[i].Color.AsVector3();
                    }
                }

                var colorPositionSpan = colorPositionsUploadBuffer.Span;
                for (var i = 0; i < colorStops.Count; i++)
                {
                    colorPositionSpan[i] = colorStops[i].Position;
                }
            }

            var colorValues = device.AllocateReadOnlyBuffer<Float3>(colorValuesUploadBuffer.Length);
            var colorPositions = device.AllocateReadOnlyBuffer<float>(colorPositionsUploadBuffer.Length);
            colorValuesUploadBuffer.CopyTo(colorValues);
            colorPositionsUploadBuffer.CopyTo(colorPositions);

            var opacityStops = colorGradient.OpacityStops;
            var opacityValues = device.AllocateReadOnlyBuffer([.. opacityStops.Select(o => o.Opacity)]);
            var opacityPositions = device.AllocateReadOnlyBuffer([.. opacityStops.Select(o => o.Position)]);

            return (colorValues, colorPositions, opacityValues, opacityPositions);
        }
    }
}

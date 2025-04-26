using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;

namespace NiVE3.PresetPlugin.Effect.Util.Stylize
{
    static class GlowProcessor
    {
        public static void ThresholdCpu(NManagedImage image, ROI roi, float threshold)
        {
            var imageWidth = image.Width;
            var imageData = image.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    if (Vector4.Dot(imageDataSpan[x], Const.ConvertToGrayScale) < threshold)
                    {
                        imageDataSpan[x] = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                    }
                }
            });
        }

        public static void ThresholdGpu(GraphicsDevice device, NGPUImage image, ROI roi, float threshold)
        {
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new GlowThresholdProcess(image.Data, image.Width, threshold, roi.Left, roi.Top));
        }

        public static void CompositeCpu(NManagedImage baseImage, NManagedImage glowImage, ROI roi, float strength, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder)
        {
            var imageWidth = baseImage.Width;
            var imageData = baseImage.Data;
            var glowImageData = glowImage.Data;
            if (compositeOrder == CompositeOrder.Front)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var glowImageDataSpan = glowImageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], glowImageDataSpan[x] * strength * color);
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var glowImageDataSpan = glowImageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = Blend.Process(blendMode, glowImageDataSpan[x] * strength * color, imageDataSpan[x]);
                    }
                });
            }
        }

        public static void CompositeGpu(GraphicsDevice device, NGPUImage baseImage, NGPUImage glowImage, ROI roi, float strength, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder)
        {
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new GlowCompositeProcess(baseImage.Data, glowImage.Data, baseImage.Width, strength, color, (int)blendMode, (int)compositeOrder, roi.Left, roi.Top));
        }

        public static void TransferGlowCpu(NManagedImage targetImage, NManagedImage glowImage, ROI roi, float strength, Vector4 color)
        {
            var imageWidth = targetImage.Width;
            var imageData = targetImage.Data;
            var glowImageData = glowImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var glowImageDataSpan = glowImageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    imageDataSpan[x] = glowImageDataSpan[x] * strength * color;
                }
            });
        }

        public static void TransferGlowGpu(GraphicsDevice device, NGPUImage targetImage, NGPUImage glowImage, ROI roi, float strength, Vector4 color)
        {
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new GlowTransferProcess(targetImage.Data, glowImage.Data, targetImage.Width, strength, color, roi.Left, roi.Top));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlowThresholdProcess(ReadWriteBuffer<Float4> image, int width, float threshold, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (Hlsl.Dot(image[pos].XYZ, Const.ConvertToGrayScaleFloat3) < threshold)
            {
                image[pos] = Const.EmptyPixelFloat4;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlowCompositeProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> glowImage, int width, float strength, Float4 color, int blendMode, int compositeOrder, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (compositeOrder == 0) // CompositeOrder.Front
            {
                image[pos] = BlendMethods.Process(blendMode, image[pos], glowImage[pos] * strength * color);
            }
            else
            {
                image[pos] = BlendMethods.Process(blendMode, glowImage[pos] * strength * color, image[pos]);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlowTransferProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> glowImage, int width, float strength, Float4 color, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            image[pos] = glowImage[pos] * strength * color;
        }
    }
}

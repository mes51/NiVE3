using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Effect.Util.General
{
    static class ImageMaskProcessor
    {
        public static void FillAlphaZeroCpu(NManagedImage image, ROI roi)
        {
            var imageWidth = image.Width;
            var imageData = image.Data;

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    Unsafe.AsRef(ref imageDataSpan[x]).W = 0.0F;
                }
            });
        }

        public static void FillAlphaZeroGpu(GraphicsDevice device, NGPUImage image, ROI roi)
        {
            device.For(roi.Width, roi.Height, new FillAlphaZeroProcess(image.Data, image.Width, roi.Left, roi.Top));
        }

        public static void SameSizeMaskCpu(NManagedImage image, ManagedRasterizedMaskImage mask, ROI roi)
        {
            var imageWidth = image.Width;
            var imageData = image.Data;
            var maskData = mask.Data;

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    Unsafe.AsRef(ref imageDataSpan[x]).W = maskDataSpan[x];
                }
            });
        }

        public static void SameSizeMaskGpu(GraphicsDevice device, NGPUImage image, GPURasterizedMaskImage mask, ROI roi)
        {
            device.For(roi.Width, roi.Height, new MaskSameSizeApplyProcess(image.Data, image.Width, mask.Data, roi.Left, roi.Top));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FillAlphaZeroProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            image[(ThreadIds.Y + startY) * width + ThreadIds.X + startX].W = 0.0F;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskSameSizeApplyProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> mask, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            image[pos].W = mask[pos];
        }
    }
}

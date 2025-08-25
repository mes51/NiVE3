using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.ComputeShader;

namespace NiVE3.PresetPlugin.Effect.Util.General
{
    static class ImageBlendProcessor
    {
        public static void SameSizeCpu(NManagedImage backImage, NManagedImage frontImage, ROI roi, BlendMode blendMode)
        {
            var imageWidth = backImage.Width;
            var imageData = backImage.Data;
            var frontImageData = frontImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var backImageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var frontImageDataSpan = frontImageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    backImageDataSpan[x] = Blend.Process(blendMode, backImageDataSpan[x], frontImageDataSpan[x]);
                }
            });
        }

        public static void SameSizeGpu(GraphicsDevice device, NGPUImage backImage, NGPUImage frontImage, ROI roi, BlendMode blendMode)
        {
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new SameSizeBlendProcess(backImage.Data, frontImage.Data, backImage.Width, (int)blendMode, roi.Left, roi.Top));
        }

        public static void TransferImageCpu(NManagedImage backImage, NManagedImage frontImage, ROI roi)
        {
            var imageWidth = backImage.Width;
            var imageData = backImage.Data;
            var frontImageData = frontImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var backImageDataSpan = imageData.AsSpan(y * imageWidth + roi.Left, roi.Width);
                var frontImageDataSpan = frontImageData.AsSpan(y * imageWidth + roi.Left, roi.Width);

                frontImageDataSpan.CopyTo(backImageDataSpan);
            });
        }

        public static void TransferImageGpu(GraphicsDevice device, NGPUImage backImage, NGPUImage frontImage, ROI roi)
        {
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new TransferImageProcess(backImage.Data, frontImage.Data, backImage.Width, roi.Left, roi.Top));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SameSizeBlendProcess(ReadWriteBuffer<Float4> back, ReadWriteBuffer<Float4> front, int width, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            back[pos] = BlendMethods.Process(blendMode, back[pos], front[pos]);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct TransferImageProcess(ReadWriteBuffer<Float4> back, ReadWriteBuffer<Float4> front, int width, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            back[pos] = front[pos];
        }
    }
}

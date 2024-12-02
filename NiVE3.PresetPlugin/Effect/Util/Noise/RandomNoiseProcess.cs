using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;

namespace NiVE3.PresetPlugin.Effect.Util.Noise
{
    static class RandomNoiseProcess
    {
        public static void ProcessCpu(NManagedImage managedImage, ROI roi, float amount, bool isColorNoise, uint randomSeed, double time)
        {
            randomSeed += 201864043U;
            var imageOriginX = (float)(roi.OriginalImagePosition.X + managedImage.Origin.X);
            var imageOriginY = (float)(roi.OriginalImagePosition.Y + managedImage.Origin.Y);
            var uTime = unchecked((uint)time.GetHashCode());
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageSpan = managedImage.GetDataSpan().Slice((y * managedImage.Width), managedImage.Width);
                var uy = BitConverter.SingleToUInt32Bits(y - imageOriginY);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var ux = BitConverter.SingleToUInt32Bits(x - imageOriginX);
                    var noise = NoiseFunction.Pcg3DFloatCpu(ux, uy, uTime, randomSeed);
                    if (!isColorNoise)
                    {
                        noise = new Vector4(Vector4.Dot(noise, Const.ConvertToGrayScale));
                    }
                    noise.W = 1.0F;

                    imageSpan[x] = Vector4.Lerp(imageSpan[x], noise, amount);
                }
            });
        }

        public static NGPUImage ProcessGpu(GraphicsDevice device, NGPUImage gpuImage, ROI roi, float amount, bool isColorNoise, uint randomSeed, double time)
        {
            randomSeed += 201864043U;
            var imageOriginX = (float)(roi.OriginalImagePosition.X + gpuImage.Origin.X);
            var imageOriginY = (float)(roi.OriginalImagePosition.Y + gpuImage.Origin.Y);
            using var context = device.CreateComputeContext();
            if (isColorNoise)
            {
                context.For(roi.Width, roi.Height, new ColorRandomNoiseProcess(gpuImage.Data, gpuImage.Width, amount, (float)time, randomSeed, roi.Left, roi.Top, imageOriginX, imageOriginY));
            }
            else
            {
                context.For(roi.Width, roi.Height, new GrayScaleRandomNoiseProcess(gpuImage.Data, gpuImage.Width, amount, (float)time, randomSeed, roi.Left, roi.Top, imageOriginX, imageOriginY));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GrayScaleRandomNoiseProcess(ReadWriteBuffer<Float4> image, int width, float amount, float time, uint seed, int startX, int startY, float originX, float originY) : IComputeShader
    {
        public void Execute()
        {
            var v = Hlsl.AsUInt(new Float3(ThreadIds.X + startX - originX, ThreadIds.Y + startY - originY, time));
            var noise = Hlsl.Dot(NoiseFunction.Pcg3DFloatGpu(v, seed), Const.ConvertToGrayScaleFloat3);

            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[pos] = Hlsl.Lerp(image[pos], new Float4(noise, noise, noise, 1.0F), amount);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ColorRandomNoiseProcess(ReadWriteBuffer<Float4> image, int width, float amount, float time, uint seed, int startX, int startY, float originX, float originY) : IComputeShader
    {
        public void Execute()
        {
            var v = Hlsl.AsUInt(new Float3(ThreadIds.X + startX - originX, ThreadIds.Y + startY - originY, time));
            var noise = new Float4(NoiseFunction.Pcg3DFloatGpu(v, seed), 1.0F);

            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[pos] = Hlsl.Lerp(image[pos], noise, amount);
        }
    }
}

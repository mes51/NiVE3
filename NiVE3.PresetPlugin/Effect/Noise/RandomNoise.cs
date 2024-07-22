using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.Shared.Extension;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Noise_RandomNoise_Name, "mes51", "ノイズ", LanguageResourceDictionary.Noise_RandomNoise_Description, ID, IsSupportGpu = true, IsRenderEveryFrame = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class RandomNoise : IEffect
    {
        const string ID = "6D2B8747-EC1B-455B-8670-FAD8B9F79BE2";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyIsColorNoiseId = nameof(PropertyIsColorNoiseId);

        const string PropertySeedId = nameof(PropertySeedId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_Amount, 100.0, 0.0, 100.0, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new CheckBoxProperty(PropertyIsColorNoiseId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_IsColorNoise, false),
                new DoubleProperty(PropertySeedId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_Seed, 0, 0, uint.MaxValue, digit: 0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0) * 0.01F;
            var isColor = (bool)properties.GetValue(PropertyIsColorNoiseId, layerTime, false);
            var seed = (uint)properties.GetValue(PropertySeedId, layerTime, 0.0) + 201864043U;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, amount, isColor, layerTime, seed);
            }
            else
            {
                return ProcessCpu(image, roi, amount, isColor, layerTime, seed);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float amount, bool isColor, double time, uint seed)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var bx = roi.Left;
            var ex = roi.Right;
            var imageOriginX = (float)(roi.OriginalImagePosition.X + image.Origin.X);
            var imageOriginY = (float)(roi.OriginalImagePosition.Y + image.Origin.Y);
            var uTime = unchecked((uint)time.GetHashCode());
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageSpan = managedImage.GetDataSpan().Slice((y * image.Width), image.Width);
                var uy = BitConverter.SingleToUInt32Bits(y - imageOriginY);
                for (var x = bx; x < ex; x++)
                {
                    var ux = BitConverter.SingleToUInt32Bits(x - imageOriginX);
                    var noise = NoiseFunction.Pcg3DCpu(ux, uy, uTime, seed);
                    if (!isColor)
                    {
                        noise = new Vector4(Vector4.Dot(noise, Const.ConvertToGrayScale));
                        noise.W = 1.0F;
                    }

                    imageSpan[x] = Vector4.Lerp(imageSpan[x], noise, amount);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float amount, bool isColor, double time, uint seed)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            var imageOriginX = (float)(roi.OriginalImagePosition.X + image.Origin.X);
            var imageOriginY = (float)(roi.OriginalImagePosition.Y + image.Origin.Y);
            var uTime = unchecked((uint)time.GetHashCode());
            using var context = device.CreateComputeContext();
            if (isColor)
            {
                context.For(roi.Width, roi.Height, new ColorRandomNoiseProcess(gpuImage.Data, gpuImage.Width, amount, uTime, seed, roi.Left, roi.Top, imageOriginX, imageOriginY));
            }
            else
            {
                context.For(roi.Width, roi.Height, new GrayScaleRandomNoiseProcess(gpuImage.Data, gpuImage.Width, amount, uTime, seed, roi.Left, roi.Top, imageOriginX, imageOriginY));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GrayScaleRandomNoiseProcess(ReadWriteBuffer<Float4> image, int width, float amount, uint time, uint seed, int startX, int startY, float originX, float originY) : IComputeShader
    {
        public void Execute()
        {
            var noise = Hlsl.Dot(NoiseFunction.Pcg3DGpu(ThreadIds.X + startX - originX, ThreadIds.Y + startY - originY, time, seed), Const.ConvertToGrayScaleFloat3);

            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[pos] = Hlsl.Lerp(image[pos], new Float4(noise, noise, noise, 1.0F), amount);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ColorRandomNoiseProcess(ReadWriteBuffer<Float4> image, int width, float amount, uint time, uint seed, int startX, int startY, float originX, float originY) : IComputeShader
    {
        public void Execute()
        {
            var noise = new Float4(NoiseFunction.Pcg3DGpu(ThreadIds.X + startX - originX, ThreadIds.Y + startY - originY, time, seed), 1.0F);

            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[pos] = Hlsl.Lerp(image[pos], noise, amount);
        }
    }

    // from https://jcgt.org/published/0009/03/02/paper.pdf
    static class NoiseFunction
    {
        const uint Multiplyer = 1664525U;

        const uint Increment = 1013904223U;

        public static Vector4 Pcg3DCpu(uint x, uint y, uint time, uint seed)
        {
            const uint Multiplyer = 1664525U;
            const uint Increment = 1013904223U;

            var vx = (x + seed) * Multiplyer + Increment;
            var vy = (y + seed) * Multiplyer + Increment;
            var vz = (time + seed) * Multiplyer + Increment;

            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;
            vx ^= vx >> 16;
            vy ^= vy >> 16;
            vz ^= vz >> 16;
            vx += vy * vz;
            vy += vz * vx;
            vz += vx * vy;

            return new Vector4((vx / (float)uint.MaxValue), (vy / (float)uint.MaxValue), (vz / (float)uint.MaxValue), 1.0F);
        }

        public static Float3 Pcg3DGpu(float x, float y, uint time, uint seed)
        {
            var v = (new UInt3(Hlsl.AsUInt(x), Hlsl.AsUInt(y), time) + seed) * Multiplyer + Increment;
            v.X += v.Y * v.Z;
            v.Y += v.Z * v.X;
            v.Z += v.X * v.Y;
            v ^= v >> 16U;
            v.X += v.Y * v.Z;
            v.Y += v.Z * v.X;
            v.Z += v.X * v.Y;

            return new Float3(v.X / (float)uint.MaxValue, v.Y / (float)uint.MaxValue, v.Z / (float)uint.MaxValue);
        }
    }
}

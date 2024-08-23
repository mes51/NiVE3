using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_AutoContrastCorrection_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_AutoContrastCorrection_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class AutoContrastCorrection : IEffect
    {
        public const int BinSize = 1024;

        const string ID = "605E7C4D-C846-4A89-916B-BC46B4738047";

        const string PropertyShadowClipId = nameof(PropertyShadowClipId);

        const string PropertyHighlightClipId = nameof(PropertyHighlightClipId);

        const string PropertyBlendOriginalId = nameof(PropertyBlendOriginalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyShadowClipId, LanguageResourceDictionary.ResourceKeys.ColorCollection_AutoContrastCorrection_ShadowClip, 0.1, 0.0, 20.0, slideChangeValue: 0.1, digit: 2),
                new DoubleProperty(PropertyHighlightClipId, LanguageResourceDictionary.ResourceKeys.ColorCollection_AutoContrastCorrection_HighlightClip, 0.1, 0.0, 20.0, slideChangeValue: 0.1, digit: 2),
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.ColorCollection_AutoContrastCorrection_BlendOriginal, 0.0, 0.0, 100.0, slideChangeValue: 0.1, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var shadowClip = (float)properties.GetValue(PropertyShadowClipId, layerTime, 0.0) * 0.01F;
            var highlightClip = (float)properties.GetValue(PropertyHighlightClipId, layerTime, 0.0) * 0.01F;
            var newColorRate = 1.0F - (float)properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, shadowClip, highlightClip, newColorRate);
            }
            else
            {
                return ProcessCpu(image, roi, shadowClip, highlightClip, newColorRate);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float shadowClip, float highlightClip, float newColorRate)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageData = managedImage.Data;
            var ycbcrImage = ArrayPool<YCbCr>.Shared.Rent(managedImage.DataLength);
            var bin = ArrayPool<int>.Shared.Rent(BinSize);
            ycbcrImage.AsSpan().Clear();
            bin.AsSpan().Clear();

            if (image.Width > BinSize)
            {
                Parallel.For(0, managedImage.Height, y =>
                {
                    Span<int> tempBin = stackalloc int[BinSize];

                    var pos = y * managedImage.Width;

                    var imageDataSpan = imageData.AsSpan(pos, managedImage.Width);
                    var ycbcrSpan = ycbcrImage.AsSpan(pos, managedImage.Width);

                    for (var i = 0; i < imageDataSpan.Length; i++)
                    {
                        var ycbcr = YCbCr.FromRgb(imageDataSpan[i]);
                        ycbcrSpan[i] = ycbcr;
                        tempBin[(int)MathF.Round(ycbcr.Y * (bin.Length - 1))]++;
                    }

                    for (var i = 0; i < bin.Length; i++)
                    {
                        Interlocked.Add(ref bin[i], tempBin[i]);
                    }
                });
            }
            else
            {
                Parallel.For(0, managedImage.Height, y =>
                {
                    var pos = y * managedImage.Width;

                    var imageDataSpan = imageData.AsSpan(pos, managedImage.Width);
                    var ycbcrSpan = ycbcrImage.AsSpan(pos, managedImage.Width);

                    for (var i = 0; i < imageDataSpan.Length; i++)
                    {
                        var ycbcr = YCbCr.FromRgb(imageDataSpan[i]);
                        ycbcrSpan[i] = ycbcr;
                        Interlocked.Increment(ref bin[(int)MathF.Round(ycbcr.Y * (bin.Length - 1))]);
                    }
                });
            }

            var (lightnessRange, shadowValue) = CalcBin(bin, shadowClip, highlightClip, managedImage.DataLength);

            if (newColorRate < 1.0F)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var pos = y * managedImage.Width + roi.Left;

                    var imageDataSpan = imageData.AsSpan(pos, roi.Width);
                    var ycbcrSpan = ycbcrImage.AsSpan(pos, roi.Width);

                    for (var i = 0; i < imageDataSpan.Length; i++)
                    {
                        var ycbcr = ycbcrSpan[i];
                        ycbcr.Y = (ycbcr.Y - shadowValue) * lightnessRange;
                        var oldColor = imageDataSpan[i];
                        var newColor = ycbcr.ToRgb();
                        newColor.W = newColorRate;
                        newColor = Blend.Process(BlendMode.Normal, oldColor, newColor);
                        newColor.W = oldColor.W;
                        imageDataSpan[i] = newColor;
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var pos = y * managedImage.Width + roi.Left;

                    var imageDataSpan = imageData.AsSpan(pos, roi.Width);
                    var ycbcrSpan = ycbcrImage.AsSpan(pos, roi.Width);

                    for (var i = 0; i < imageDataSpan.Length; i++)
                    {
                        var ycbcr = ycbcrSpan[i];
                        ycbcr.Y = (ycbcr.Y - shadowValue) * lightnessRange;
                        var newColor = ycbcr.ToRgb();
                        newColor.W = imageDataSpan[i].W;
                        imageDataSpan[i] = newColor;
                    }
                });
            }

            ArrayPool<int>.Shared.Return(bin);
            ArrayPool<YCbCr>.Shared.Return(ycbcrImage);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float shadowClip, float highlightClip, float newColorRate)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            var binCpu = ArrayPool<int>.Shared.Rent(BinSize);
            using var bin = device.AllocateReadWriteBuffer<int>(BinSize);
            using (var context = device.CreateComputeContext())
            {
                context.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, bin, gpuImage.Width));
            }

            bin.CopyTo(binCpu.AsSpan(0, BinSize));

            var (lightnessRange, shadowValue) = CalcBin(binCpu, shadowClip, highlightClip, gpuImage.DataLength);

            using (var context = device.CreateComputeContext())
            {
                context.For(roi.Width, roi.Height, new AdjustContrastProcess(gpuImage.Data, gpuImage.Width, lightnessRange, shadowValue, newColorRate, roi.Left, roi.Top));
            }

            ArrayPool<int>.Shared.Return(binCpu);

            return gpuImage;
        }

        static (float lightnessRange, float shadowValue) CalcBin(int[] bin, float shadowClip, float highlightClip, int totalCount)
        {
            for (var i = 1; i < bin.Length; i++)
            {
                bin[i] = bin[i] + bin[i - 1];
            }
            var shadowClipCount = (int)MathF.Round(totalCount * shadowClip);
            var shadowValue = (bin.IndexOf(v => shadowClipCount <= v) + 1) / (float)BinSize;
            var highlightClipCount = (int)MathF.Round(totalCount * (1.0F - highlightClip));
            var highlightValue = (bin.IndexOfLast(v => highlightClipCount >= v) + 1) / (float)BinSize;
            var lightnessRange = 1.0F / Math.Max(highlightValue - shadowValue, 0.01F);

            return (lightnessRange, shadowValue);
        }
    }

    [ThreadGroupSize(ThreadGroupSize, ThreadGroupSize, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HistogramProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<int> bin, int width) : IComputeShader
    {
        const int ThreadGroupSize = 32;

        [GroupShared(AutoContrastCorrection.BinSize)]
        static readonly int[] TempBin = [];

        public void Execute()
        {
            if (GroupIds.Index == 0)
            {
                for (var i = 0; i < AutoContrastCorrection.BinSize; i++)
                {
                    TempBin[i] = 0;
                }
            }

            Hlsl.GroupMemoryBarrierWithGroupSync();

            var color = Hlsl.Saturate(image[ThreadIds.Y * width + ThreadIds.X]);
            var y = Hlsl.Dot(color, new Float4(0.299F, 0.587F, 0.114F, 0.0F));
            Hlsl.InterlockedAdd(ref TempBin[(int)Hlsl.Round(y * (AutoContrastCorrection.BinSize - 1))], 1);

            Hlsl.GroupMemoryBarrierWithGroupSync();

            if (GroupIds.Index == 0)
            {
                for (var i = 0; i < AutoContrastCorrection.BinSize; i++)
                {
                    Hlsl.InterlockedAdd(ref bin[i], TempBin[i]);
                }
            }
            //*/
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct AdjustContrastProcess(ReadWriteBuffer<Float4> image, int width, float lightnessRange, float shadowValue, float newColorRate, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var color = Hlsl.Saturate(image[pos]);
            var ycbcr = ColorSpaceConversion.RgbToYCbCr(color);

            ycbcr.X = (ycbcr.X - shadowValue) * lightnessRange;
            var newColor = ColorSpaceConversion.YCbCrToRgb(ycbcr);

            if (newColorRate < 1.0F)
            {
                var alpha = color.W;
                color.W = 1.0F;
                newColor.W = newColorRate;
                newColor = BlendMethods.Process(0, color, newColor);
                newColor.W = alpha;
            }

            image[pos] = newColor;
        }
    }
}

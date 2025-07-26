using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Utility
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Utility_Histogram_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Utility, LanguageResourceDictionary.Utility_Histogram_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    sealed public class Histogram : IEffect
    {
        public const int BinCount = 256;

        public const int HistogramMargin = 20;

        public const int HistogramImageSize = BinCount + HistogramMargin * 2;

        const string ID = "34165D55-AE01-4980-8FDE-2075B453CC43";

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyPositionId = nameof(PropertyPositionId);

        const string PropertyScaleId = nameof(PropertyScaleId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Utility_Histogram_Channel, typeof(ChannelType), typeof(LanguageResourceDictionary), ChannelType.RGB, selectBoxWidth: 90.0),
                new Vector3dProperty(PropertyPositionId, LanguageResourceDictionary.ResourceKeys.Utility_Histogram_Position, Vector3d.Zero, digit: 2, useInteraction: true),
                new Vector3dProperty(PropertyScaleId, LanguageResourceDictionary.ResourceKeys.Utility_Histogram_Scale, new Vector3d(100.0, 100.0, 0.0), Vector3d.Zero, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, useLinkRatio: true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var channel = properties.GetValue(PropertyChannelId, layerTime, ChannelType.RGB);
            var position = (Vector2)properties.GetValue(PropertyPositionId, layerTime, Vector3d.Zero);
            var scale = (Vector2)(properties.GetValue(PropertyScaleId, layerTime, Vector3d.Zero) * 0.01);

            if (scale.X <= 0.0F || scale.Y <= 0.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, channel, position, scale);
            }
            else
            {
                return ProcessCpu(image, roi, channel, position, scale);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, ChannelType channelType, Vector2 position, Vector2 scale)
        {
            var managedImage = image.ToManaged();

            var bin = new int[BinCount * 4];

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            switch (channelType)
            {
                case ChannelType.RGB:
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = 0; x < imageWidth; x++)
                        {
                            var quantizedColor = Vector4.Clamp(imageDataSpan[x], Vector4.Zero, Vector4.One) * (BinCount - 1);
                            Interlocked.Increment(ref bin[(int)MathF.Round(quantizedColor.X)]);
                            Interlocked.Increment(ref bin[BinCount + (int)MathF.Round(quantizedColor.Y)]);
                            Interlocked.Increment(ref bin[BinCount * 2 + (int)MathF.Round(quantizedColor.Z)]);
                        }
                    });
                    break;
                case ChannelType.R:
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = 0; x < imageWidth; x++)
                        {
                            var channel = Math.Clamp(imageDataSpan[x].Z, 0.0F, 1.0F) * (BinCount - 1);
                            Interlocked.Increment(ref bin[BinCount * 2 + (int)MathF.Round(channel)]);
                        }
                    });
                    break;
                case ChannelType.G:
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = 0; x < imageWidth; x++)
                        {
                            var channel = Math.Clamp(imageDataSpan[x].Y, 0.0F, 1.0F) * (BinCount - 1);
                            Interlocked.Increment(ref bin[BinCount + (int)MathF.Round(channel)]);
                        }
                    });
                    break;
                case ChannelType.B:
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = 0; x < imageWidth; x++)
                        {
                            var channel = Math.Clamp(imageDataSpan[x].X, 0.0F, 1.0F) * (BinCount - 1);
                            Interlocked.Increment(ref bin[(int)MathF.Round(channel)]);
                        }
                    });
                    break;
                case ChannelType.A:
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = 0; x < imageWidth; x++)
                        {
                            var channel = Math.Clamp(imageDataSpan[x].W, 0.0F, 1.0F) * (BinCount - 1);
                            Interlocked.Increment(ref bin[BinCount * 2 + (int)MathF.Round(channel)]);
                        }
                    });
                    break;
            }
            var maxCount = (float)bin.Max();

            using var histogramImage = new NManagedImage(HistogramImageSize, HistogramImageSize, Vector4.UnitW);

            var histogramImageData = histogramImage.Data;
            for (int pos = HistogramMargin - 1 + (HistogramMargin - 1) * HistogramImageSize, c = 0; c <= BinCount; pos += HistogramImageSize, c++)
            {
                histogramImageData[pos] = Vector4.One;
            }
            for (int pos = (HistogramMargin + BinCount - 1) * HistogramImageSize + HistogramMargin - 1, c = 0; c <= BinCount; pos++, c++)
            {
                histogramImageData[pos] = Vector4.One;
            }

            switch (channelType)
            {
                case ChannelType.RGB:
                    Parallel.For(0, BinCount, y =>
                    {
                        var histogramImageDataSpan = histogramImageData.AsSpan((y + HistogramMargin - 1) * HistogramImageSize + HistogramMargin, BinCount);

                        var binHeight = (BinCount - y) / (float)BinCount;
                        for (var x = 0; x < BinCount; x++)
                        {
                            histogramImageDataSpan[x] = new Vector4(
                                binHeight < bin[x] / maxCount ? 1.0F : 0.0F,
                                binHeight < bin[x + BinCount] / maxCount ? 1.0F : 0.0F,
                                binHeight < bin[x + BinCount * 2] / maxCount ? 1.0F : 0.0F,
                                1.0F
                            );
                        }
                    });
                    break;
                case ChannelType.R:
                    Parallel.For(0, BinCount, y =>
                    {
                        var histogramImageDataSpan = histogramImageData.AsSpan((y + HistogramMargin) * HistogramImageSize + HistogramMargin, BinCount);

                        var binHeight = (BinCount - y) / (float)BinCount;
                        for (var x = 0; x < BinCount; x++)
                        {
                            histogramImageDataSpan[x] = new Vector4(0.0F, 0.0F, binHeight < bin[x + BinCount * 2] / maxCount ? 1.0F : 0.0F, 1.0F);
                        }
                    });
                    break;
                case ChannelType.G:
                    Parallel.For(0, BinCount, y =>
                    {
                        var histogramImageDataSpan = histogramImageData.AsSpan((y + HistogramMargin) * HistogramImageSize + HistogramMargin, BinCount);

                        var binHeight = (BinCount - y) / (float)BinCount;
                        for (var x = 0; x < BinCount; x++)
                        {
                            histogramImageDataSpan[x] = new Vector4(0.0F, binHeight < bin[x + BinCount] / maxCount ? 1.0F : 0.0F, 0.0F, 1.0F);
                        }
                    });
                    break;
                case ChannelType.B:
                    Parallel.For(0, BinCount, y =>
                    {
                        var histogramImageDataSpan = histogramImageData.AsSpan((y + HistogramMargin) * HistogramImageSize + HistogramMargin, BinCount);

                        var binHeight = (BinCount - y) / (float)BinCount;
                        for (var x = 0; x < BinCount; x++)
                        {
                            histogramImageDataSpan[x] = new Vector4(binHeight < bin[x] / maxCount ? 1.0F : 0.0F, 0.0F, 0.0F, 1.0F);
                        }
                    });
                    break;
                case ChannelType.A:
                    Parallel.For(0, BinCount, y =>
                    {
                        var histogramImageDataSpan = histogramImageData.AsSpan((y + HistogramMargin) * HistogramImageSize + HistogramMargin, BinCount);

                        var binHeight = (BinCount - y) / (float)BinCount;
                        for (var x = 0; x < BinCount; x++)
                        {
                            var color = binHeight < bin[x + BinCount * 3] / maxCount ? 1.0F : 0.0F;
                            histogramImageDataSpan[x] = new Vector4(color, color, color, 1.0F);
                        }
                    });
                    break;
            }

            new CPURenderer2D(managedImage)
            {
                Clip = new Int32Rect(roi.Left, roi.Top, roi.Width, roi.Height)
            }.DrawSingleImage(Int32Point.Zero, histogramImage, 1.0F, Matrix3x3.CreateScale(scale.X, scale.Y).Translate(position.X, position.Y), ImageInterpolationQuality.Level1, BlendMode.Normal, null);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, ChannelType channelType, Vector2 position, Vector2 scale)
        {
            var gpuImage = image.ToGpu(device);

            using var bin = device.AllocateReadWriteBuffer<int>(BinCount * 4);

            switch (channelType)
            {
                case ChannelType.RGB:
                    {
                        using var context = device.CreateComputeContext();

                        context.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 0));
                        context.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 1));
                        context.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 2));
                    }
                    break;
                case ChannelType.R:
                    device.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 2));
                    break;
                case ChannelType.G:
                    device.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 1));
                    break;
                case ChannelType.B:
                    device.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 0));
                    break;
                case ChannelType.A:
                    device.For(gpuImage.Width, gpuImage.Height, new HistogramProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, bin, 3));
                    break;
            }

            var managedBin = new int[bin.Length];
            bin.CopyTo(managedBin);
            var maxCount = managedBin.Max();

            using var histogramImage = new NGPUImage(HistogramImageSize, HistogramImageSize, device, Vector4.UnitW);

            device.For(BinCount + 1, BinCount + 1, new HistogramDrawProcess(histogramImage.Data, bin, maxCount));

            new GPURenderer2D(gpuImage, device)
            {
                Clip = new Int32Rect(roi.Left, roi.Top, roi.Width, roi.Height)
            }.DrawSingleImage(Int32Point.Zero, histogramImage, 1.0F, Matrix3x3.CreateScale(scale.X, scale.Y).Translate(position.X, position.Y), ImageInterpolationQuality.Level1, BlendMode.Normal, null);

            return gpuImage;
        }
    }

    [ThreadGroupSize(16, 16, 1)] // NOTE: BinCountと合わせる
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HistogramProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<int> bin, int channel) : IComputeShader
    {
        [GroupShared(Histogram.BinCount)]
        static int[] LocalHistogram = [];

        public void Execute()
        {
            LocalHistogram[GroupIds.Index] = 0;
            if (ThreadIds.X == width - 1 &&  ThreadIds.Y == height - 1)
            {
                for (var i = GroupIds.Index; i < Histogram.BinCount; i++)
                {
                    LocalHistogram[i] = 0;
                }
            }

            Hlsl.GroupMemoryBarrierWithGroupSync();

            var binChannelPos = Histogram.BinCount * channel;
            var pos = ThreadIds.Y * width + ThreadIds.X;
            var binPos = (int)Hlsl.Round(Hlsl.Clamp(image[pos][channel], 0.0F, 1.0F) * (Histogram.BinCount - 1));
            Hlsl.InterlockedAdd(ref LocalHistogram[binPos], 1);

            Hlsl.GroupMemoryBarrierWithGroupSync();

            Hlsl.InterlockedAdd(ref bin[binChannelPos + GroupIds.Index], LocalHistogram[GroupIds.Index]);
            if (ThreadIds.X == width - 1 && ThreadIds.Y == height - 1)
            {
                for (var i = GroupIds.Index; i < Histogram.BinCount; i++)
                {
                    Hlsl.InterlockedAdd(ref bin[binChannelPos + i], LocalHistogram[i]);
                }
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HistogramDrawProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<int> bin, float maxCount) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + Histogram.HistogramMargin - 1;
            var y = ThreadIds.Y + Histogram.HistogramMargin - 1;
            var pos = y * Histogram.HistogramImageSize + x;

            if (ThreadIds.X == 0 || ThreadIds.Y == Histogram.BinCount)
            {
                image[pos] = Float4.One;
                return;
            }

            var binHeight = (Histogram.BinCount - ThreadIds.Y) / (float)Histogram.BinCount;
            var color = Float4.UnitW;

            var binPos = ThreadIds.X - 1;
            var bCount = bin[binPos] / maxCount;
            if (binHeight < bCount)
            {
                color.X = 1.0F;
            }

            var gCount = bin[Histogram.BinCount + binPos] / maxCount;
            if (binHeight < gCount)
            {
                color.Y = 1.0F;
            }

            var rCount = bin[Histogram.BinCount * 2 + binPos] / maxCount;
            if (binHeight < rCount)
            {
                color.Z = 1.0F;
            }

            var aCount = bin[Histogram.BinCount * 3 + binPos] / maxCount;
            if (binHeight < aCount)
            {
                color.XYZ = 1.0F;
            }

            image[pos] = color;
        }
    }
}

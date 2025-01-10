using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Mosaic_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Mosaic_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Mosaic : IEffect
    {
        const string ID = "CA1CAD7D-819C-49BE-9199-838F640EB110";

        const string PropertyHorizontalBlockId = nameof(PropertyHorizontalBlockId);

        const string PropertyVerticalBlockId = nameof(PropertyVerticalBlockId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyHorizontalBlockId, LanguageResourceDictionary.ResourceKeys.Stylize_Mosaic_HorizontalBlock, 10.0, 1.0, int.MaxValue, digit: 0),
                new DoubleProperty(PropertyVerticalBlockId, LanguageResourceDictionary.ResourceKeys.Stylize_Mosaic_VerticalBlock, 10.0, 1.0, int.MaxValue, digit: 0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var horizontalBlock = (int)(properties.GetValue(PropertyHorizontalBlockId, layerTime, 0.0) / downSamplingRateX);
            var verticalBlock = (int)(properties.GetValue(PropertyVerticalBlockId, layerTime, 0.0) / downSamplingRateY);

            if (horizontalBlock >= image.Width && verticalBlock >= image.Height)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, horizontalBlock, verticalBlock);
            }
            else
            {
                return ProcessCpu(image, roi, horizontalBlock, verticalBlock);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, int horizontalBlock, int verticalBlock)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var mosaicData = ArrayPool<Vector4>.Shared.Rent(horizontalBlock * verticalBlock);
            var mosaicDataCount = ArrayPool<int>.Shared.Rent(horizontalBlock * verticalBlock);
            mosaicData.AsSpan().Clear();
            mosaicDataCount.AsSpan().Clear();

            var hBlockWidth = MathF.Ceiling(managedImage.Width / (float)horizontalBlock);
            var vBlockHeight = MathF.Ceiling(managedImage.Height / (float)verticalBlock);

            var alignedTop = (int)Math.Floor(roi.Top / vBlockHeight) * (int)vBlockHeight;
            var alignedBottom = Math.Min((int)Math.Ceiling(roi.Bottom / vBlockHeight) * (int)vBlockHeight, managedImage.Height);
            var alignedLeft = (int)Math.Floor(roi.Left / hBlockWidth) * (int)hBlockWidth;
            var alignedRight = Math.Min((int)Math.Ceiling(roi.Right / hBlockWidth) * (int)hBlockWidth, managedImage.Width);
            Parallel.For(alignedTop, alignedBottom, y =>
            {
                var mosaicLine = ArrayPool<Vector4>.Shared.Rent(horizontalBlock);
                var mosaicLineCount = ArrayPool<int>.Shared.Rent(horizontalBlock);
                mosaicLine.AsSpan().Clear();
                mosaicLineCount.AsSpan().Clear();
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = alignedLeft; x < alignedRight; x++)
                {
                    var hBlockPos = (int)(x / hBlockWidth);
                    mosaicLine[hBlockPos] += imageDataSpan[x];
                    mosaicLineCount[hBlockPos]++;
                }

                var floatMosaicLine = MemoryMarshal.Cast<Vector4, float>(mosaicLine.AsSpan(0, horizontalBlock));
                var mosaicDataSpan = MemoryMarshal.Cast<Vector4, float>(mosaicData.AsSpan((int)(y / (float)vBlockHeight) * horizontalBlock, horizontalBlock));
                var mosaicDataCountSpan = mosaicDataCount.AsSpan((int)(y / (float)vBlockHeight) * horizontalBlock, horizontalBlock);
                for (var h = 0; h < floatMosaicLine.Length; h++)
                {
                    // SEE: https://stackoverflow.com/a/16893641
                    var newCurrentValue = mosaicDataSpan[h];
                    while (true)
                    {
                        var currentValue = newCurrentValue;
                        var newValue = currentValue + floatMosaicLine[h];
                        newCurrentValue = Interlocked.CompareExchange(ref mosaicDataSpan[h], newValue, currentValue);
                        if (newCurrentValue.Equals(currentValue))
                        {
                            break;
                        }
                    }
                }
                for (var h = 0; h < mosaicDataCountSpan.Length; h++)
                {
                    Interlocked.Add(ref mosaicDataCountSpan[h], mosaicLineCount[h]);
                }

                ArrayPool<Vector4>.Shared.Return(mosaicLine);
                ArrayPool<int>.Shared.Return(mosaicLineCount);
            });

            Parallel.For(0, verticalBlock, y =>
            {
                var mosaicDataSpan = mosaicData.AsSpan(y * horizontalBlock, horizontalBlock);
                var mosaicDataCountSpan = mosaicDataCount.AsSpan(y * horizontalBlock, horizontalBlock);
                for (var x = 0; x < horizontalBlock; x++)
                {
                    mosaicDataSpan[x] /= mosaicDataCountSpan[x];
                }
            });

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var mosaicDataSpan = mosaicData.AsSpan((int)(y / vBlockHeight) * horizontalBlock, horizontalBlock);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var mosaicX = (int)(x / hBlockWidth);
                    imageDataSpan[x] = mosaicDataSpan[mosaicX];
                }
            });

            ArrayPool<Vector4>.Shared.Return(mosaicData);
            ArrayPool<int>.Shared.Return(mosaicDataCount);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, int horizontalBlock, int verticalBlock)
        {
            var gpuImage = image.ToGpu(device);

            using var mosaicData = device.AllocateReadWriteBuffer<Float4>(horizontalBlock * verticalBlock);

            var hBlockWidth = MathF.Ceiling(gpuImage.Width / (float)horizontalBlock);
            var vBlockHeight = MathF.Ceiling(gpuImage.Height / (float)verticalBlock);

            var mosaicTop = (int)Math.Floor(roi.Top / vBlockHeight);
            var mosaicBottom = (int)Math.Ceiling(roi.Bottom / vBlockHeight);
            var mosaicLeft = (int)Math.Floor(roi.Left / hBlockWidth);
            var mosaicRight = (int)Math.Ceiling(roi.Right / hBlockWidth);

            using var context = device.CreateComputeContext();
            context.For(mosaicRight - mosaicLeft, mosaicBottom - mosaicTop, new MosaicSumProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, mosaicData, horizontalBlock, (int)hBlockWidth, (int)vBlockHeight, mosaicLeft, mosaicTop));
            context.Barrier(mosaicData);

            context.For(roi.Width, roi.Height, new MoasicCopyProcess(gpuImage.Data, gpuImage.Width, mosaicData, horizontalBlock, hBlockWidth, vBlockHeight, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MosaicSumProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> mosaicData, int horizontalBlock, int hBlockWidth, int vBlockHeight, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var mosaicBlockX = ThreadIds.X + startX;
            var mosaicBlockY = ThreadIds.Y + startY;
            var mosaicPos = mosaicBlockY * horizontalBlock + mosaicBlockX;

            var mosaicColor = Float4.Zero;
            var sumCount = 0;
            var imageY = mosaicBlockY * vBlockHeight;
            for (var y = 0; y < vBlockHeight && imageY < height; y++, imageY++)
            {
                var imageX = mosaicBlockX * hBlockWidth;
                var imagePos = imageY * width + imageX;
                for (var x = 0; x < hBlockWidth && imageX < width; x++, imageX++, imagePos++)
                {
                    mosaicColor += image[imagePos];
                    sumCount++;
                }
            }

            if (sumCount > 0)
            {
                mosaicData[mosaicPos] = mosaicColor / sumCount;
            }
            else
            {
                mosaicData[mosaicPos] = Const.EmptyPixelFloat4;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MoasicCopyProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<Float4> mosaicData, int horizontalBlock, float hBlockWidth, float vBlockHeight, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var mosaicPos = (int)(y / vBlockHeight) * horizontalBlock + (int)(x / hBlockWidth);

            image[pos] = mosaicData[mosaicPos];
        }
    }
}

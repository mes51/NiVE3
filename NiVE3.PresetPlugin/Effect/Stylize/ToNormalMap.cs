using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_ToNormalMap_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_ToNormalMap_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ToNormalMap : IEffect
    {
        const string ID = "625F6518-839F-4D75-A5C8-26F993B44390";

        const string PropertyStrengthId = nameof(PropertyStrengthId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyStrengthId, LanguageResourceDictionary.ResourceKeys.Stylize_ToNormalMap_Strength, 75.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            if (image.Width <= 1 && image.Height <= 1)
            {
                return image;
            }

            var strength = (float)(1.0 - properties.GetValue(PropertyStrengthId, layerTime, 0.0) * 0.01);
            if (AcceleratorObject != null && useGpu)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, strength);
            }
            else
            {
                return ProcessCpu(image, roi, strength);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float strength)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var sourceRoiLeft = Math.Max(roi.Left - 1, 0);
            var sourceRoiRight = Math.Min(roi.Right + 1, imageWidth);
            var imageData = managedImage.Data;
            var sourceData = ArrayPool<float>.Shared.Rent(managedImage.DataLength);
            Parallel.For(Math.Max(roi.Top - 1, 0), Math.Min(roi.Bottom + 1, managedImage.Height), y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceDataSpan = sourceData.AsSpan(y * imageWidth, imageWidth);

                for (var x = sourceRoiLeft; x < sourceRoiRight; x++)
                {
                    var color = imageDataSpan[x];
                    sourceDataSpan[x] = Vector4.Dot(color * color.W, Const.ConvertToGrayScale);
                }
            });

            var imageHeight = managedImage.Height;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var centerSourceLineSpan = sourceData.AsSpan(y * imageWidth, imageWidth);
                var topSourceLineSpan = sourceData.AsSpan((y > 0 ? (y - 1) : y) * imageWidth, imageWidth);
                var bottomSourceLineSpan = sourceData.AsSpan((y <= imageHeight - 2 ? y + 1 : imageHeight - 1) * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var leftX = CoordWrap.Wrap(x - 1, imageWidth);
                    var rightX = CoordWrap.Wrap(x + 1, imageWidth);

                    var sobelX = topSourceLineSpan[rightX] + centerSourceLineSpan[rightX] * 2.0F + bottomSourceLineSpan[rightX] - topSourceLineSpan[leftX] - centerSourceLineSpan[leftX] * 2.0F - bottomSourceLineSpan[leftX];
                    var sobelY = topSourceLineSpan[leftX] + topSourceLineSpan[x] * 2.0F + topSourceLineSpan[rightX] - bottomSourceLineSpan[leftX] - bottomSourceLineSpan[x] * 2.0F - bottomSourceLineSpan[rightX];

                    if (sobelX == 0.0F && sobelY == 0.0F)
                    {
                        imageDataSpan[x] = new Vector4(1.0F, 0.5F, 0.5F, 1.0F);
                    }
                    else
                    {
                        var normal = Vector3.Normalize(new Vector3(strength, -sobelY, sobelX)) + new Vector3(0.0F, 0.5F, 0.5F);
                        imageDataSpan[x] = new Vector4(normal, 1.0F);
                    }
                }
            });

            ArrayPool<float>.Shared.Return(sourceData);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float strength)
        {
            var gpuImage = image.ToGpu(device);
            using var sourceImage = device.AllocateReadWriteBuffer<float>(gpuImage.DataLength);

            using (var context = device.CreateComputeContext())
            {
                var sourceRoiLeft = Math.Max(roi.Left - 1, 0);
                var sourceRoiTop = Math.Max(roi.Top - 1, 0);
                var sourceRoiWidth = Math.Min(roi.Right + 1, gpuImage.Width) - sourceRoiLeft;
                var sourceRoiHeight = Math.Min(roi.Bottom + 1, gpuImage.Height) - sourceRoiTop;

                context.For(sourceRoiWidth, sourceRoiHeight, new ToNormalMapGrayScaleProcess(gpuImage.Data, gpuImage.Width, sourceImage, sourceRoiLeft, sourceRoiTop));
                context.Barrier(sourceImage);
                context.For(roi.Width, roi.Height, new ToNormalMapProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage, strength, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ToNormalMapGrayScaleProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> sourceImage, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            sourceImage[pos] = Hlsl.Dot(color.XYZ * color.W, Const.ConvertToGrayScaleFloat3);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ToNormalMapProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<float> sourceImage, float strength, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var topLine = CoordWrapGpu.Wrap(y - 1, height) * width;
            var centerLine = y * width;
            var bottomLine = CoordWrapGpu.Wrap(y + 1, height) * width;
            var left = CoordWrapGpu.Wrap(x - 1, width);
            var right = CoordWrapGpu.Wrap(x + 1, width);

            var sobelX = sourceImage[topLine + right] + sourceImage[centerLine + right] * 2.0F + sourceImage[bottomLine + right] - sourceImage[topLine + left] - sourceImage[centerLine + left] * 2.0F - sourceImage[bottomLine + left];
            var sobelY = sourceImage[topLine + left] + sourceImage[topLine + x] * 2.0F + sourceImage[topLine + right] - sourceImage[bottomLine + left] - sourceImage[bottomLine + x] * 2.0F - sourceImage[bottomLine + right];

            var pos = y * width + x;
            if (sobelX == 0.0F && sobelY == 0.0F)
            {
                image[pos] = new Float4(1.0F, 0.5F, 0.5F, 1.0F);
            }
            else
            {
                var normal = Hlsl.Normalize(new Float3(strength, -sobelY, sobelX)) + new Float3(0.0F, 0.5F, 0.5F);
                image[pos] = new Float4(normal, 1.0F);
            }
        }
    }
}

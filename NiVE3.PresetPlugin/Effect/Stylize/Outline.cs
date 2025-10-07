using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Outline_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Outline_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Outline : IEffect
    {
        const string ID = "FE055200-8709-40AB-8273-B87770AB6861";

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyWidthId = nameof(PropertyWidthId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyOnlyOutlineId = nameof(PropertyOnlyOutlineId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_Outline_Threshold, 0.0, 0.0, 1.0, slideChangeValue: 0.001, digit: 3),
                new DoubleProperty(PropertyWidthId, LanguageResourceDictionary.ResourceKeys.Stylize_Outline_Width, 5.0, double.MinValue, double.MaxValue, digit: 2),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Outline_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, new Vector4(0.0F, 0.0F, 1.0F, 1.0F)),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_Outline_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new CheckBoxProperty(PropertyOnlyOutlineId, LanguageResourceDictionary.ResourceKeys.Stylize_Outline_OutlineOnly, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var opacity = (float)(properties.GetValue(PropertyOpacityId, layerTime, 0.0) * 0.01);
            var outlineOnly = properties.GetValue(PropertyOnlyOutlineId, layerTime, false);

            if (opacity <= 0.0F)
            {
                if (!outlineOnly)
                {
                    return image;
                }
                else
                {
                    if (useGpu && AcceleratorObject != null)
                    {
                        var device = AcceleratorObject.CurrentDevice;
                        var gpuImage = image.ToGpu(device);
                        ImageClearProcessor.ClearGpu(device, gpuImage, roi);
                        return gpuImage;
                    }
                    else
                    {
                        var managedImage = image.ToManaged();
                        ImageClearProcessor.ClearCpu(managedImage, roi);
                        return managedImage;
                    }
                }
            }

            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.0);
            var outlineWidth = (Vector2)(new Vector2d(properties.GetValue(PropertyWidthId, layerTime, 0.0)) / new Vector2d(downSamplingRateX, downSamplingRateY));
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.Zero);
            color.W = opacity;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, threshold, outlineWidth, color, outlineOnly);
            }
            else
            {
                return ProcessCpu(image, roi, threshold, outlineWidth, color, outlineOnly);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float threshold, Vector2 outlineWidth, Vector4 color, bool outlineOnly)
        {
            var managedImage = image.ToManaged();
            var isInvert = outlineWidth.X < 0.0F;
            outlineWidth = Vector2.Abs(outlineWidth);

            var top = Math.Max((int)MathF.Floor(roi.Top - outlineWidth.Y), 0);
            var bottom = Math.Min((int)MathF.Ceiling(roi.Bottom + outlineWidth.Y), managedImage.Height);
            var left = Math.Max((int)MathF.Floor(roi.Left - outlineWidth.X), 0);
            var right = Math.Min((int)Math.Ceiling(roi.Right + outlineWidth.X), managedImage.Width);
            var blurROI = new ROI(new Int32Point(), new Int32Size(managedImage.Width, managedImage.Height), left, top, right, bottom);

            using var blurredMask = new ManagedRasterizedMaskImage(managedImage.Width, managedImage.Height);
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var maskData = blurredMask.Data;
            if (isInvert)
            {
                Parallel.For(blurROI.Top, blurROI.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = blurROI.Left; x < blurROI.Right; x++)
                    {
                        if (imageDataSpan[x].W <= threshold)
                        {
                            maskDataSpan[x] = 1.0F;
                        }
                    }
                });
            }
            else
            {
                Parallel.For(blurROI.Top, blurROI.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = blurROI.Left; x < blurROI.Right; x++)
                    {
                        if (imageDataSpan[x].W > threshold)
                        {
                            maskDataSpan[x] = 1.0F;
                        }
                    }
                });
            }

            if (Math.Abs(outlineWidth.X) > 0.0F)
            {
                MaskBoxBlurProcessor.ProcessCpu(blurredMask, blurROI, outlineWidth.X, outlineWidth.Y, 1, EdgeRepeatMode.Wrap);
            }

            var outlineImage = new NManagedImage(managedImage.Width, managedImage.Height);
            var outlineImageData = outlineImage.Data;
            var outlineThreshold = 1.0F / ((outlineWidth.X * 2.0F + 1) * (outlineWidth.Y * 2.0F + 1.0F));
            if (isInvert)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var outlineImageDataSpan = outlineImageData.AsSpan(y * imageWidth, imageWidth);
                    var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                       if (maskDataSpan[x] < outlineThreshold)
                       {
                           outlineImageDataSpan[x] = color;
                       }
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var outlineImageDataSpan = outlineImageData.AsSpan(y * imageWidth, imageWidth);
                    var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        if (maskDataSpan[x] >= outlineThreshold)
                        {
                            outlineImageDataSpan[x] = color;
                        }
                    }
                });
            }

            if (outlineOnly)
            {
                ImageBlendProcessor.TransferSameSizeCpu(managedImage, outlineImage, roi);
                outlineImage.Dispose();

                return managedImage;
            }
            else
            {
                ImageBlendProcessor.SameSizeCpu(outlineImage, managedImage, roi, BlendMode.Normal);
                if (managedImage != image)
                {
                    managedImage.Dispose();
                }

                return outlineImage;
            }
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float threshold, Vector2 outlineWidth, Vector4 color, bool outlineOnly)
        {
            var gpuImage = image.ToGpu(device);
            var isInvert = outlineWidth.X < 0.0F;
            outlineWidth = Vector2.Abs(outlineWidth);

            var top = Math.Max((int)MathF.Floor(roi.Top - outlineWidth.Y), 0);
            var bottom = Math.Min((int)MathF.Ceiling(roi.Bottom + outlineWidth.Y), gpuImage.Height);
            var left = Math.Max((int)MathF.Floor(roi.Left - outlineWidth.X), 0);
            var right = Math.Min((int)Math.Ceiling(roi.Right + outlineWidth.X), gpuImage.Width);
            var blurROI = new ROI(new Int32Point(), new Int32Size(gpuImage.Width, gpuImage.Height), left, top, right, bottom);

            using var blurredMask = new GPURasterizedMaskImage(gpuImage.Width, gpuImage.Height, device);
            device.For(blurROI.Width, blurROI.Height, new OutlineExtractAlphaProcess(gpuImage.Data, gpuImage.Width, blurredMask.Data, threshold, isInvert, blurROI.Left, blurROI.Top));

            MaskBoxBlurProcessor.ProcessGpu(device, blurredMask, blurROI, outlineWidth.X, outlineWidth.Y, 1, EdgeRepeatMode.Wrap);

            var outlineImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            var outlineThreshold = 1.0F / ((outlineWidth.X * 2.0F + 1) * (outlineWidth.Y * 2.0F + 1.0F));
            device.For(roi.Width, roi.Height, new OutlineGenerateOutlineProcess(blurredMask.Data, outlineImage.Width, outlineImage.Data, outlineThreshold, isInvert, color, roi.Left, roi.Top));

            if (outlineOnly)
            {
                ImageBlendProcessor.TransferSameSizeGpu(device, gpuImage, outlineImage, roi);
                outlineImage.Dispose();

                return gpuImage;
            }
            else
            {
                ImageBlendProcessor.SameSizeGpu(device, outlineImage, gpuImage, roi, BlendMode.Normal);
                if (gpuImage != image)
                {
                    gpuImage.Dispose();
                }

                return outlineImage;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct OutlineExtractAlphaProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> mask, float threshold, bool isInvert, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (isInvert)
            {
                if (image[pos].W <= threshold)
                {
                    mask[pos] = 1.0F;
                }
            }
            else
            {
                if (image[pos].W > threshold)
                {
                    mask[pos] = 1.0F;
                }
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct OutlineGenerateOutlineProcess(ReadWriteBuffer<float> mask, int width, ReadWriteBuffer<Float4> outlineImage, float outlineThreshold, bool isInvert, Float4 color, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (isInvert)
            {
                if (mask[pos] < outlineThreshold)
                {
                    outlineImage[pos] = color;
                }
            }
            else
            {
                if (mask[pos] >= outlineThreshold)
                {
                    outlineImage[pos] = color;
                }
            }
        }
    }
}

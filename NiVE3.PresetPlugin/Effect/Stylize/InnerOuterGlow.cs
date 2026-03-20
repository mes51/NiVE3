using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Transform;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_InnerOuterGlow_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_InnerOuterGlow_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class InnerOuterGlow : IEffect
    {
        const string ID = "2A3C81DD-5D75-4894-98D1-F844B5EF29F8";

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const string PropertyGlowWidthId = nameof(PropertyGlowWidthId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Stylize_InnerOuterGlow_Direction, typeof(InnerOuterGlowDirection), typeof(LanguageResourceDictionary), InnerOuterGlowDirection.Inner, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyGlowWidthId, LanguageResourceDictionary.ResourceKeys.Stylize_InnerOuterGlow_GlowWidth, 5.0, 0.0, double.MaxValue, digit: 2),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Stylize_InnerOuterGlow_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, new Vector4(0.2F, 1.0F, 1.0F, 1.0F)),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_InnerOuterGlow_Opacity, 75.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_InnerOuterGlow_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var direction = properties.GetValue(PropertyDirectionId, layerTime, InnerOuterGlowDirection.Inner);

            if (direction == InnerOuterGlowDirection.Outer)
            {
                var glowWidth = (float)properties.GetValue(PropertyGlowWidthId, layerTime, 0.0);

                return baseRoi.Expand((int)MathF.Ceiling(glowWidth));
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var glowWidth = (float)properties.GetValue(PropertyGlowWidthId, layerTime, 0.0);
            var opacity = (float)(properties.GetValue(PropertyOpacityId, layerTime, 0.0) * 0.01);
            var direction = properties.GetValue(PropertyDirectionId, layerTime, InnerOuterGlowDirection.Inner);

            if (direction != InnerOuterGlowDirection.Outer && (glowWidth <= 0.0F || opacity <= 0.0F))
            {
                return image;
            }

            glowWidth += 1.0F;

            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.Zero);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);
            color.W = opacity;

            if (useGpu && AcceleratorObject != null)
            {
                if (direction == InnerOuterGlowDirection.Outer)
                {
                    return OuterProcessGpu(AcceleratorObject.CurrentDevice, image, roi, glowWidth, color, blendMode);
                }
                else
                {
                    return InnerProcessGpu(AcceleratorObject.CurrentDevice, image, roi, glowWidth, color, blendMode);
                }
            }
            else
            {
                if (direction == InnerOuterGlowDirection.Outer)
                {
                    return OuterProcessCpu(image, roi, glowWidth, color, blendMode);
                }
                else
                {
                    return InnerProcessCpu(image, roi, glowWidth, color, blendMode);
                }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage InnerProcessCpu(NImage image, ROI roi, float glowWidth, Vector4 color, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            var distanceMap = DistanceTransformProcessor.ProcessCpu(managedImage, float.Epsilon);
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var distanceMapSpan = distanceMap.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var rate = Math.Clamp((glowWidth - distanceMapSpan[x]) / glowWidth, 0.0F, 1.0F);
                    if (rate > 0.0F)
                    {
                        var blendColor = color;
                        blendColor.W *= rate;
                        var backColor = imageDataSpan[x];
                        var a = backColor.W;
                        backColor.W = 1.0F;
                        var newColor = Blend.Process(blendMode, backColor, blendColor);
                        newColor.W = a;
                        imageDataSpan[x] = newColor;
                    }
                }
            });

            return managedImage;
        }

        static NManagedImage OuterProcessCpu(NImage image, ROI roi, float glowWidth, Vector4 color, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            var distanceMap = DistanceTransformProcessor.InvertProcessCpu(managedImage, float.Epsilon);
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var distanceMapSpan = distanceMap.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var rate = Math.Clamp((glowWidth - distanceMapSpan[x]) / glowWidth, 0.0F, 1.0F);
                    if (rate > 0.0F)
                    {
                        var blendColor = color;
                        blendColor.W *= rate;
                        imageDataSpan[x] = Blend.Process(blendMode, blendColor, imageDataSpan[x]);
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage InnerProcessGpu(GraphicsDevice device, NImage image, ROI roi, float glowWidth, Vector4 color, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            var distanceMap = DistanceTransformProcessor.ProcessGpu(device, gpuImage, 1E-20F);

            device.For(roi.Width, roi.Height, new InnerOuterGlowInnerProcess(gpuImage.Data, gpuImage.Width, distanceMap, glowWidth, color, (int)blendMode, roi.Left, roi.Top));

            return gpuImage;
        }

        static NGPUImage OuterProcessGpu(GraphicsDevice device, NImage image, ROI roi, float glowWidth, Vector4 color, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            var distanceMap = DistanceTransformProcessor.InvertProcessGpu(device, gpuImage, 1E-20F);

            device.For(roi.Width, roi.Height, new InnerOuterGlowOuterProcess(gpuImage.Data, gpuImage.Width, distanceMap, glowWidth, color, (int)blendMode, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    enum InnerOuterGlowDirection
    {
        Inner,
        Outer
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct InnerOuterGlowInnerProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> distanceMap, float glowWidth, Float4 color, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var rate = Hlsl.Clamp((glowWidth - distanceMap[pos]) / glowWidth, 0.0F, 1.0F);
            if (rate > 0.0F)
            {
                var blendColor = color;
                blendColor.W *= rate;
                var backColor = image[pos];
                var a = backColor.W;
                backColor.W = 1.0F;
                var newColor = BlendMethods.Process(blendMode, backColor, blendColor);
                newColor.W = a;
                image[pos] = newColor;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct InnerOuterGlowOuterProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> distanceMap, float glowWidth, Float4 color, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var rate = Hlsl.Clamp((glowWidth - distanceMap[pos]) / glowWidth, 0.0F, 1.0F);
            if (rate > 0.0F)
            {
                var blendColor = color;
                blendColor.W *= rate;
                image[pos] = BlendMethods.Process(blendMode, blendColor, image[pos]);
            }
        }
    }
}

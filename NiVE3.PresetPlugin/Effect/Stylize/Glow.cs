using System;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Glow_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Glow_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Glow : IEffect
    {
        const string ID = "924E043C-1EF9-41A8-876A-1EB6ED2D0BDE";

        const string PropertyRangeId = nameof(PropertyRangeId);

        const string PropertyStrengthId = nameof(PropertyStrengthId);

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const string PropertyCompositeOrderId = nameof(PropertyCompositeOrderId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const int BlurRepeatCount = 3;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyRangeId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_Range, 10.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyStrengthId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_Strength, 1.0, 0.0, 100.0, slideChangeValue: 0.1, digit: 3),
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_Threshold, 0.0, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0),
                new EnumProperty(PropertyCompositeOrderId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_CompositeOrder, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Stylize_Glow_Direction, typeof(GlowDirection), typeof(LanguageResourceDictionary), GlowDirection.HorizontalAndVertical, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyRangeId, layerTime, 0.0);
                var repeat = (int)properties.GetValue(PropertyRangeId, layerTime, 1.0);
                var direction = properties.GetValue(PropertyDirectionId, layerTime, GlowDirection.HorizontalAndVertical);

                var expandX = (int)Math.Ceiling(amount * repeat / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount * repeat / downSamplingRateY);
                switch (direction)
                {
                    case GlowDirection.Horizontal:
                        return baseRoi.Expand(-expandX, 0, expandX, 0);
                    case GlowDirection.Vertical:
                        return baseRoi.Expand(0, -expandY, 0, expandY);
                    default:
                        return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
                }
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var range = properties.GetValue(PropertyRangeId, layerTime, 0.0);
            var strength = (float)properties.GetValue(PropertyStrengthId, layerTime, 0.0);
            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.0);
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.UnitW);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Add);
            var compositeOrder = properties.GetValue(PropertyCompositeOrderId, layerTime, CompositeOrder.Front);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);
            var direction = properties.GetValue(PropertyDirectionId, layerTime, GlowDirection.HorizontalAndVertical);

            if ((strength == 0.0 || color == Vector4.UnitW) && blendMode == BlendMode.Add)
            {
                return image;
            }

            var horizontalRange = direction != GlowDirection.Vertical ? (float)(range / downSamplingRateX / BlurRepeatCount) : 0.0F;
            var verticalRange = direction != GlowDirection.Horizontal ? (float)(range / downSamplingRateY / BlurRepeatCount) : 0.0F;
            color.W = 1.0F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, horizontalRange, verticalRange, strength, threshold, color, blendMode, compositeOrder, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, horizontalRange, verticalRange, strength, threshold, color, blendMode, compositeOrder, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float horizontalRange, float verticalRange, float strength, float threshold, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var blurredImage = (NManagedImage)managedImage.Copy();
            var top = Math.Max((int)MathF.Floor(roi.Top - verticalRange * BlurRepeatCount), 0);
            var bottom = Math.Min((int)MathF.Ceiling(roi.Bottom + verticalRange * BlurRepeatCount), blurredImage.Height);
            var left = Math.Max((int)MathF.Floor(roi.Left - horizontalRange * BlurRepeatCount), 0);
            var right = Math.Min((int)Math.Ceiling(roi.Right + horizontalRange * BlurRepeatCount), blurredImage.Width);
            var imageWidth = managedImage.Width;
            var blurredImageData = blurredImage.Data;
            Parallel.For(top, bottom, y =>
            {
                var blurredImageDataSpan = blurredImageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = left; x < right; x++)
                {
                    if (Vector4.Dot(blurredImageDataSpan[x], Const.ConvertToGrayScale) < threshold)
                    {
                        blurredImageDataSpan[x] = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                    }
                }
            });
            blurredImage = BoxBlurProcess.ProcessCpu(blurredImage, roi, horizontalRange, verticalRange, BlurRepeatCount, edgeRepeatMode);

            var imageData = managedImage.Data;
            if (compositeOrder == CompositeOrder.Front)
            {
                Parallel.For(roi.Top, bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var blurredImageDataSpan = blurredImageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], blurredImageDataSpan[x] * strength * color);
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var blurredImageDataSpan = blurredImageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        imageDataSpan[x] = Blend.Process(blendMode, blurredImageDataSpan[x] * strength * color, imageDataSpan[x]);
                    }
                });
            }

            blurredImage.Dispose();

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float horizontalRange, float verticalRange, float strength, float threshold, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            var blurredImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(blurredImage);
            var top = Math.Max((int)MathF.Floor(roi.Top - verticalRange * BlurRepeatCount), 0);
            var bottom = Math.Min((int)MathF.Ceiling(roi.Bottom + verticalRange * BlurRepeatCount), blurredImage.Height);
            var left = Math.Max((int)MathF.Floor(roi.Left - horizontalRange * BlurRepeatCount), 0);
            var right = Math.Min((int)Math.Ceiling(roi.Right + horizontalRange * BlurRepeatCount), blurredImage.Width);

            using (var context = device.CreateComputeContext())
            {
                context.For(right - left, bottom - top, new GlowThresholdProcess(blurredImage.Data, blurredImage.Width, threshold, left, top));
            }

            BoxBlurProcess.ProcessGpu(device, blurredImage, roi, horizontalRange, verticalRange, BlurRepeatCount, edgeRepeatMode);

            using (var context = device.CreateComputeContext())
            {
                context.For(roi.Width, roi.Height, new GlowCompositeProcess(gpuImage.Data, blurredImage.Data, gpuImage.Width, strength, color, (int)blendMode, (int)compositeOrder, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    enum GlowDirection
    {
        HorizontalAndVertical,
        Horizontal,
        Vertical
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlowThresholdProcess(ReadWriteBuffer<Float4> image, int width, float threshold, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (Hlsl.Dot(image[pos].XYZ, Const.ConvertToGrayScaleFloat3) < threshold)
            {
                image[pos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GlowCompositeProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> blurredImage, int width, float strength, Float4 color, int blendMode, int compositeOrder, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            if (compositeOrder == 0) // CompositeOrder.Front
            {
                image[pos] = BlendMethods.Process(blendMode, image[pos], blurredImage[pos] * strength * color);
            }
            else
            {
                image[pos] = BlendMethods.Process(blendMode, blurredImage[pos] * strength * color, image[pos]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Resource;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Effect.Util.Stylize;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_StarBurst_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_StarBurst_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class StarBurst : IEffect
    {
        const string ID = "4B43569F-EAB0-468D-AF31-2E31A8A88A8D";

        const string PropertyStrengthId = nameof(PropertyStrengthId);

        const string PropertyCountId = nameof(PropertyCountId);

        const string PropertyLengthId = nameof(PropertyLengthId);

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyLightBlurId = nameof(PropertyLightBlurId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const string PropertyCompositeOrderId = nameof(PropertyCompositeOrderId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        const string PropertyDrawStarBurstOnly = nameof(PropertyDrawStarBurstOnly);

        const int LightBlurRepeatCount = 3;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var rad = (properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0) / 180.0 * Math.PI;
                var length = (float)(properties.GetValue(PropertyLengthId, layerTime, 0.0) / downSamplingRateX);
                var lightBlurRange = (int)MathF.Ceiling((float)(properties.GetValue(PropertyLightBlurId, layerTime, 0.0) / downSamplingRateX));
                var count = (int)properties.GetValue(PropertyCountId, layerTime, 1.0);
                var maxRange = (int)MathF.Ceiling(length) * 2;
                var radianIncrement = Math.PI / count;

                var maxExpandX = lightBlurRange;
                var maxExpandY = lightBlurRange;
                for (var i = 0; i < count; i++)
                {
                    var sin = (float)Math.Sin(rad + radianIncrement * i);
                    var cos = (float)Math.Cos(rad + radianIncrement * i);
                    maxExpandX = Math.Max(maxExpandX, (int)Math.Ceiling(Math.Abs(maxRange * cos)));
                    maxExpandY = Math.Max(maxExpandY, (int)Math.Ceiling(Math.Abs(maxRange * sin)));
                }

                return baseRoi.Expand(-maxExpandX, -maxExpandY, maxExpandX, maxExpandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyStrengthId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Strength, 1.0, 0.0, double.MaxValue, slideChangeValue: 0.1, digit: 2),
                new DoubleProperty(PropertyCountId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Count, 2, 1, 4, digit: 0),
                new DoubleProperty(PropertyLengthId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Length, 10.0, 0.0, 10000.0, digit: 2),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Angle, 15.0, digit: 2),
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Threshold, 0.6, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyLightBlurId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_LightBlur, 0.0, 0.0, 100.0, digit: 2),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0),
                new EnumProperty(PropertyCompositeOrderId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_CompositeOrder, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyDrawStarBurstOnly, LanguageResourceDictionary.ResourceKeys.Stylize_StarBurst_DrawStarBurstOnly, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var strength = (float)properties.GetValue(PropertyStrengthId, layerTime, 0.0);
            var count = (int)properties.GetValue(PropertyCountId, layerTime, 1.0);
            var length = (float)(properties.GetValue(PropertyLengthId, layerTime, 0.0) / downSamplingRateX);
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0);
            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.0);
            var lightBlurAmount = (float)(properties.GetValue(PropertyLightBlurId, layerTime, 0.0) / downSamplingRateX / LightBlurRepeatCount);
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.UnitW);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Add);
            var compositeOrder = properties.GetValue(PropertyCompositeOrderId, layerTime, CompositeOrder.Front);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);
            var drawStarBurstOnly = properties.GetValue(PropertyDrawStarBurstOnly, layerTime, false);

            if ((strength <= 0.0 || length <= 0.0 || color == Vector4.UnitW) && blendMode == BlendMode.Add && !drawStarBurstOnly)
            {
                return image;
            }

            color.W = 1.0F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, strength, count, length, angle, threshold, lightBlurAmount, color, blendMode, compositeOrder, edgeRepeatMode, drawStarBurstOnly);
            }
            else
            {
                return ProcessCpu(image, roi, strength, count, length, angle, threshold, lightBlurAmount, color, blendMode, compositeOrder, edgeRepeatMode, drawStarBurstOnly);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float strength, int count, float length, float angle, float threshold, float lightBlurAmount, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, EdgeRepeatMode edgeRepeatMode, bool drawStarBurstOnly)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;

            using var starBurstImage = new NManagedImage(managedImage.Width, managedImage.Height);
            using (var blurredImage = (NManagedImage)managedImage.Copy())
            {
                var rad = (angle + 90.0) / 180.0 * Math.PI;
                var maxRange = (int)MathF.Ceiling(length) * 2;

                var top = Math.Max(roi.Top - maxRange, 0);
                var bottom = Math.Min(roi.Bottom + maxRange, blurredImage.Height);
                var left = Math.Max(roi.Left - maxRange, 0);
                var right = Math.Min(roi.Right + maxRange, blurredImage.Width);
                var blurredImageRoi = new ROI(new Int32Point(), new Int32Size(blurredImage.Width, blurredImage.Height), left, top, right, bottom);

                GlowProcess.ThresholdCpu(blurredImage, blurredImageRoi, threshold);
                if (lightBlurAmount > 0.0F)
                {
                    BoxBlurProcess.ProcessCpu(blurredImage, blurredImageRoi, lightBlurAmount, lightBlurAmount, LightBlurRepeatCount, edgeRepeatMode);
                }

                var radianIncrement = Math.PI / count;
                for (var c = 0; c < count; c++)
                {
                    if (c + 1 >= count)
                    {
                        GaussianDirectionalBlurProcess.BidirectionalCpu(blurredImage, roi, rad + radianIncrement * c, length, edgeRepeatMode, true);
                        ImageBlendProcess.SameSizeCpu(starBurstImage, blurredImage, roi, BlendMode.Add);
                    }
                    else
                    {
                        using var temp = (NManagedImage)blurredImage.Copy();
                        GaussianDirectionalBlurProcess.BidirectionalCpu(temp, roi, rad + radianIncrement * c, length, edgeRepeatMode, true);
                        ImageBlendProcess.SameSizeCpu(starBurstImage, temp, roi, BlendMode.Add);
                    }
                }
            }

            if (drawStarBurstOnly)
            {
                GlowProcess.TransferGlowCpu(managedImage, starBurstImage, roi, strength, color);
            }
            else
            {
                GlowProcess.CompositeCpu(managedImage, starBurstImage, roi, strength, color, blendMode, compositeOrder);
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float strength, int count, float length, float angle, float threshold, float lightBlurAmount, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, EdgeRepeatMode edgeRepeatMode, bool drawStarBurstOnly)
        {
            var gpuImage = image.ToGpu(device);

            using var starBurstImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            using (var blurredImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device))
            {
                gpuImage.CopyTo(blurredImage);
            
                var rad = (angle + 90.0) / 180.0 * Math.PI;
                var maxRange = (int)MathF.Ceiling(length) * 2;
            
                var top = Math.Max(roi.Top - maxRange, 0);
                var bottom = Math.Min(roi.Bottom + maxRange, blurredImage.Height);
                var left = Math.Max(roi.Left - maxRange, 0);
                var right = Math.Min(roi.Right + maxRange, blurredImage.Width);
                var blurredImageRoi = new ROI(new Int32Point(), new Int32Size(blurredImage.Width, blurredImage.Height), left, top, right, bottom);

                GlowProcess.ThresholdGpu(device, blurredImage, blurredImageRoi, threshold);
                if (lightBlurAmount > 0.0F)
                {
                    BoxBlurProcess.ProcessGpu(device, blurredImage, blurredImageRoi, lightBlurAmount, lightBlurAmount, LightBlurRepeatCount, edgeRepeatMode);
                }
            
                var radianIncrement = Math.PI / count;
                using var temp = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                for (var c = 0; c < count; c++)
                {
                    if (c + 1 >= count)
                    {
                        GaussianDirectionalBlurProcess.BidirectionalGpu(device, blurredImage, roi, rad + radianIncrement * c, length, edgeRepeatMode);
                        ImageBlendProcess.SameSizeGpu(device, starBurstImage, blurredImage, roi, BlendMode.Add);
                    }
                    else
                    {
                        blurredImage.CopyTo(temp);
                        GaussianDirectionalBlurProcess.BidirectionalGpu(device, temp, roi, rad + radianIncrement * c, length, edgeRepeatMode);
                        ImageBlendProcess.SameSizeGpu(device, starBurstImage, temp, roi, BlendMode.Add);
                    }
                }
            }

            if (drawStarBurstOnly)
            {
                GlowProcess.TransferGlowGpu(device, gpuImage, starBurstImage, roi, strength, color);
            }
            else
            {
                GlowProcess.CompositeGpu(device, gpuImage, starBurstImage, roi, strength, color, blendMode, compositeOrder);
            }

            return gpuImage;
        }
    }
}

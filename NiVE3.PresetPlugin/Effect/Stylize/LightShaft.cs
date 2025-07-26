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
using NiVE3.PresetPlugin.Effect.Blur;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.Stylize;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_LightShaft_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_LightShaft_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class LightShaft : IEffect
    {
        const string ID = "F9925354-520D-4CBF-8CAE-8891129C8859";

        const string PropertyCenterId = nameof(PropertyCenterId);

        const string PropertyLengthId = nameof(PropertyLengthId);

        const string PropertyStrengthId = nameof(PropertyStrengthId);

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const string PropertyCompositeOrderId = nameof(PropertyCompositeOrderId);

        const string PropertyDrawLightShaftOnlyId = nameof(PropertyDrawLightShaftOnlyId);

        const string PropertyFastModeId = nameof(PropertyFastModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new Vector3dProperty(PropertyCenterId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_Center, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                new DoubleProperty(PropertyLengthId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_Length, 10.0, 0.0, 100.0, digit: 2),
                new DoubleProperty(PropertyStrengthId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_Strength, 1.0, 0.0, double.MaxValue, slideChangeValue: 0.1, digit: 3),
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_Threshold, 0.0, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0),
                new EnumProperty(PropertyCompositeOrderId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_CompositeOrder, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyDrawLightShaftOnlyId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_DrawLightShaftOnly, false),
                new CheckBoxProperty(PropertyFastModeId, LanguageResourceDictionary.ResourceKeys.Stylize_LightShaft_FastMode, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var center = (Vector2)(properties.GetValue(PropertyCenterId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var length = (float)(properties.GetValue(PropertyLengthId, layerTime, 0.0) / downSamplingRateX * 0.1);
            var strength = (float)properties.GetValue(PropertyStrengthId, layerTime, 0.0);
            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.0);
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.UnitW);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Add);
            var compositeOrder = properties.GetValue(PropertyCompositeOrderId, layerTime, CompositeOrder.Front);
            var drawLightShaftOnly = properties.GetValue(PropertyDrawLightShaftOnlyId, layerTime, false);
            var fastMode = properties.GetValue(PropertyFastModeId, layerTime, false);

            if ((strength == 0.0 || color == Vector4.UnitW) && blendMode == BlendMode.Add && !drawLightShaftOnly)
            {
                return image;
            }

            color.W = 1.0F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, center, length, strength, threshold, color, blendMode, compositeOrder, drawLightShaftOnly);
            }
            else
            {
                return ProcessCpu(image, roi, center, length, strength, threshold, color, blendMode, compositeOrder, drawLightShaftOnly, fastMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 center, float length, float strength, float threshold, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, bool drawLightShaftOnly, bool fastMode)
        {
            var managedImage = image.ToManaged();
            using var blurredImage = (NManagedImage)managedImage.Copy();

            GlowProcessor.ThresholdCpu(blurredImage, roi, threshold);

            RadialBlurProcessor.ProcessCpu(blurredImage, roi, center, length, fastMode);

            if (drawLightShaftOnly)
            {
                GlowProcessor.TransferGlowCpu(managedImage, blurredImage, roi, strength, color);
            }
            else
            {
                GlowProcessor.CompositeCpu(managedImage, blurredImage, roi, strength, color, blendMode, compositeOrder);
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 center, float length, float strength, float threshold, Vector4 color, BlendMode blendMode, CompositeOrder compositeOrder, bool drawLightShaftOnly)
        {
            var gpuImage = image.ToGpu(device);
            using var blurredImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(blurredImage);

            GlowProcessor.ThresholdGpu(device, blurredImage, roi, threshold);

            RadialBlurProcessor.ProcessGpu(device, blurredImage, roi, center, length);

            if (drawLightShaftOnly)
            {
                GlowProcessor.TransferGlowGpu(device, gpuImage, blurredImage, roi, strength, color);
            }
            else
            {
                GlowProcessor.CompositeGpu(device, gpuImage, blurredImage, roi, strength, color, blendMode, compositeOrder);
            }

            return gpuImage;
        }
    }
}

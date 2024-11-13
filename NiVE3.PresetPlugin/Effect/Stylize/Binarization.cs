using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
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
    [EffectMetadata(LanguageResourceDictionary.Stylize_Binarization_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Binarization_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Binarization : IEffect
    {
        const string ID = "0B1AEAB9-F6EB-4C24-B97B-1DD261E691B1";

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyHighlightColorId = nameof(PropertyHighlightColorId);

        const string PropertyHighlightOpacityId = nameof(PropertyHighlightOpacityId);

        const string PropertyShadowColorId = nameof(PropertyShadowColorId);

        const string PropertyShadowOpacityId = nameof(PropertyShadowOpacityId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_Binarization_Threshold, 0.5, double.MinValue, double.MaxValue, digit: 4, slideChangeValue: 0.01),
                new ColorProperty(PropertyHighlightColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Binarization_HighlightColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new DoubleProperty(PropertyHighlightOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_Binarization_HighlightOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new ColorProperty(PropertyShadowColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Binarization_HighlightColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.UnitW),
                new DoubleProperty(PropertyShadowOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_Binarization_HighlightOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.0);
            var highlightColor = properties.GetValue(PropertyHighlightColorId, layerTime, Vector4.One);
            var highlightOpacity = (float)properties.GetValue(PropertyHighlightOpacityId, layerTime, 0.0);
            var shadowColor = properties.GetValue(PropertyShadowColorId, layerTime, Vector4.One);
            var shadowOpacity = (float)properties.GetValue(PropertyShadowOpacityId, layerTime, 0.0);
            highlightColor.W = highlightOpacity * 0.01F;
            shadowColor.W = shadowOpacity * 0.01F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, threshold, highlightColor, shadowColor);
            }
            else
            {
                return ProcessCpu(image, roi, threshold, highlightColor, shadowColor);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        public NManagedImage ProcessCpu(NImage image, ROI roi, float threshold, Vector4 highlightColor, Vector4 shadowColor)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    imageDataSpan[x] = Vector4.Dot(imageDataSpan[x], Const.ConvertToGrayScale) >= threshold ? highlightColor : shadowColor;
                }
            });

            return managedImage;
        }

        public NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float threshold, Vector4 highlightColor, Vector4 shadowColor)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new BinarizationProcess(gpuImage.Data, gpuImage.Width, threshold, highlightColor, shadowColor, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BinarizationProcess(ReadWriteBuffer<Float4> image, int width, float threshold, Float4 highlightColor, Float4 shadowColor, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            image[pos] = Hlsl.Dot(image[pos].XYZ, Const.ConvertToGrayScaleFloat3) >= threshold ? highlightColor : shadowColor;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_GaussianDirectionalBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_GaussianDirectionalBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class GaussianDirectionalBlur : IEffect
    {
        const string ID = "B553064E-107C-4D8B-83F8-F55BB3DFD4EA";

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyIsSingleDirectionId = nameof(PropertyIsSingleDirectionId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

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
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianDirectionalBlur_Angle, 0.0, digit: 2),
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianDirectionalBlur_Amount, 0.0, 0.0, double.MaxValue, digit: 2),
                new CheckBoxProperty(PropertyIsSingleDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianDirectionalBlur_IsSingleDirection, false),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianDirectionalBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyFastModeId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianDirectionalBlur_FastMode, false)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);
            var isSingleDirection = properties.GetValue(PropertyIsSingleDirectionId, layerTime, false);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var rad = (properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0) / 180.0 * Math.PI;
                var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
                var maxRange = (int)MathF.Ceiling(amount) * 2;
                var sin = (float)Math.Sin(rad);
                var cos = (float)Math.Cos(rad);

                var expandX = (int)Math.Ceiling(Math.Abs(maxRange * cos));
                var expandY = (int)Math.Ceiling(Math.Abs(maxRange * sin));
                if (isSingleDirection)
                {
                    return (cos < 0.0F, sin < 0.0F) switch
                    {
                        (false, false) => baseRoi.Expand(-expandX, -expandY, 0, 0),
                        (false, true) => baseRoi.Expand(-expandX, 0, 0, expandY),
                        (true, false) => baseRoi.Expand(0, -expandY, expandX, 0),
                        _ => baseRoi.Expand(0, 0, expandX, expandY)
                    };
                }
                else
                {
                    return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
                }
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0F;
            var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);
            var fastMode = properties.GetValue(PropertyFastModeId, layerTime, false);
            var isSingleDirection = properties.GetValue(PropertyIsSingleDirectionId, layerTime, false);

            if (amount <= 0.0F)
            {
                return image;
            }

            var rad = angle / 180.0 * Math.PI;
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                if (isSingleDirection)
                {
                    GaussianDirectionalBlurProcess.UnidirectionalGpu(device, gpuImage, roi, rad, amount, edgeRepeatMode);
                }
                else
                {
                    GaussianDirectionalBlurProcess.BidirectionalGpu(device, gpuImage, roi, rad, amount, edgeRepeatMode);
                }
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                if (isSingleDirection)
                {
                    GaussianDirectionalBlurProcess.UnidirectionalCpu(managedImage, roi, rad, amount, edgeRepeatMode, fastMode);
                }
                else
                {
                    GaussianDirectionalBlurProcess.BidirectionalCpu(managedImage, roi, rad, amount, edgeRepeatMode, fastMode);
                }
                return managedImage;
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

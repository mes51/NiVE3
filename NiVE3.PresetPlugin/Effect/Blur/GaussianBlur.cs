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
    [EffectMetadata(LanguageResourceDictionary.Blur_GaussianBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_GaussianBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class GaussianBlur : IEffect
    {
        const string ID = "EE6D548B-DF62-40CF-8ED8-B05430897A98";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const string PropertyIsRepeatEdgeId = nameof(PropertyIsRepeatEdgeId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_Direction, typeof(BlurDirection), typeof(LanguageResourceDictionary), BlurDirection.HorizontalAndVertical, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
                var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);

                var expandX = (int)Math.Ceiling(amount / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount / downSamplingRateY);
                switch (direction)
                {
                    case BlurDirection.Horizontal:
                        return baseRoi.Expand(-expandX, 0, expandX, 0);
                    case BlurDirection.Vertical:
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

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = properties.GetValue(PropertyAmountId, layerTime, 0.0);
            var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F)
            {
                return image;
            }

            var horizontalAmount = direction != BlurDirection.Vertical ? (float)(amount / downSamplingRateX) : 0.0F;
            var verticalAmount = direction != BlurDirection.Horizontal ? (float)(amount / downSamplingRateY) : 0.0F;
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                GaussianBlurProcess.ProcessGpu(AcceleratorObject.CurrentDevice, gpuImage, roi, horizontalAmount, verticalAmount, edgeRepeatMode);
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                GaussianBlurProcess.ProcessCpu(managedImage, roi, horizontalAmount, verticalAmount, edgeRepeatMode);
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

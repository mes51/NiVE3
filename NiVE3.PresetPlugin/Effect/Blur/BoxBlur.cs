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
    [EffectMetadata(LanguageResourceDictionary.Blur_BoxBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_BoxBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class BoxBlur : IEffect
    {
        const string ID = "6DC081A1-4748-45ED-95BB-3E48AA74FD48";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyRepeatId = nameof(PropertyRepeatId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

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
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyRepeatId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Repeat, 3, 1, 50, digit: 0),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Direction, typeof(BlurDirection), typeof(LanguageResourceDictionary), BlurDirection.HorizontalAndVertical, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
                var repeat = (int)properties.GetValue(PropertyRepeatId, layerTime, 1.0);
                var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);

                var expandX = (int)Math.Ceiling(amount * repeat / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount * repeat / downSamplingRateY);
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

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var amount = properties.GetValue(PropertyAmountId, layerTime, 0.0);
            var repeat = (int)properties.GetValue(PropertyRepeatId, layerTime, 1.0);
            var direction  = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);
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
                BoxBlurProcess.ProcessGpu(device, gpuImage, roi, horizontalAmount, verticalAmount, repeat, edgeRepeatMode);
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                BoxBlurProcess.ProcessCpu(managedImage, roi, horizontalAmount, verticalAmount, repeat, edgeRepeatMode);
                return managedImage;
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

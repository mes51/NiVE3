using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
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
    [EffectMetadata(LanguageResourceDictionary.Blur_RadialBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_RadialBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class RadialBlur : IEffect
    {
        const string ID = "D0469D10-7ECD-4554-8373-DD61F3166569";

        const string PropertyCenterId = nameof(PropertyCenterId);

        const string PropertyAmountId = nameof(PropertyAmountId);

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
                new Vector3dProperty(PropertyCenterId, LanguageResourceDictionary.ResourceKeys.Blur_RadialBlur_Center, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_RadialBlur_Amount, 0.0, 0.0, 100.0, digit: 2),
                new CheckBoxProperty(PropertyFastModeId, LanguageResourceDictionary.ResourceKeys.Blur_RadialBlur_FastMode, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX * 0.1);

            if (amount <= 0.0F)
            {
                return image;
            }

            var center = (Vector2)(properties.GetValue(PropertyCenterId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var fastMode = properties.GetValue(PropertyFastModeId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                return RadialBlurProcessor.ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, center, amount);
            }
            else
            {
                return RadialBlurProcessor.ProcessCpu(image, roi, center, amount, fastMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

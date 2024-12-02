using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using ComputeSharp;
using NiVE3.Plugin.Resource;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Noise;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Noise_RandomNoise_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_RandomNoise_Description, ID, IsSupportGpu = true, IsRenderEveryFrame = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class RandomNoise : IEffect
    {
        const string ID = "6D2B8747-EC1B-455B-8670-FAD8B9F79BE2";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyIsColorNoiseId = nameof(PropertyIsColorNoiseId);

        const string PropertySeedId = nameof(PropertySeedId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_Amount, 100.0, 0.0, 100.0, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new CheckBoxProperty(PropertyIsColorNoiseId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_IsColorNoise, false),
                new DoubleProperty(PropertySeedId, LanguageResourceDictionary.ResourceKeys.Noise_RandomNoise_Seed, 0, 0, uint.MaxValue, digit: 0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0) * 0.01F;
            var isColor = (bool)properties.GetValue(PropertyIsColorNoiseId, layerTime, false);
            var seed = (uint)properties.GetValue(PropertySeedId, layerTime, 0.0);

            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                RandomNoiseProcess.ProcessGpu(device, gpuImage, roi, amount, isColor, seed, layerTime);
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                RandomNoiseProcess.ProcessCpu(managedImage, roi, amount, isColor, seed, layerTime);
                return managedImage;
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

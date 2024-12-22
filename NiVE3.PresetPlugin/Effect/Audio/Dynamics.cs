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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NWaves.Operations;

namespace NiVE3.PresetPlugin.Effect.Audio
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Audio_Dynamics_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Audio, LanguageResourceDictionary.Audio_Dynamics_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary), SupportedSource = EffectSupportedSource.Audio)]
    public class Dynamics : IEffect
    {
        const string ID = "B244012D-AEA1-40E6-BB6B-FE253DE62D67";

        const string PropertyDynamicsProcessorTypeId = nameof(PropertyDynamicsProcessorTypeId);

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyRatioId = nameof(PropertyRatioId);

        const string PropertyGainId = nameof(PropertyGainId);

        const string PropertyAttackId = nameof(PropertyAttackId);

        const string PropertyReleaseId = nameof(PropertyReleaseId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyDynamicsProcessorTypeId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_DynamicsProcessorType, typeof(DynamicsMode), typeof(LanguageResourceDictionary), DynamicsMode.Compressor, false, 100.0),
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_Threshold, 0.0, -120.0, 0.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel),
                new DoubleProperty(PropertyRatioId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_Ratio, 2.0, 1.0, 20.0, digit: 2),
                new DoubleProperty(PropertyGainId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_Gain, 0.0, -60.0, 24.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel),
                new DoubleProperty(PropertyAttackId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_Attack, 50.0, 0.01, 1000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_MilliSecond),
                new DoubleProperty(PropertyReleaseId, LanguageResourceDictionary.ResourceKeys.Audio_Dynamics_Release, 500.0, 0.01, 10000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_MilliSecond),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var mode = properties.GetValue(PropertyDynamicsProcessorTypeId, startTime, DynamicsMode.Compressor);
            var leftProcessor = new DynamicsProcessor(mode, Const.AudioSamplingRate, 0.0F, 2.0F);
            var rightProcessor = new DynamicsProcessor(mode, Const.AudioSamplingRate, 0.0F, 2.0F);

            var thresholdProperty = properties.First(p => p.Id == PropertyThresholdId);
            var ratioProperty = properties.First(p => p.Id == PropertyRatioId);
            var gainProperty = properties.First(p => p.Id == PropertyGainId);
            var attackProperty = properties.First(p => p.Id == PropertyAttackId);
            var releaseProperty = properties.First(p => p.Id == PropertyReleaseId);
            for (var i = 0; i < audio.Length; i += 2)
            {
                var sampleTime = i / Const.AudioChannelCount * Const.AudioSampleTime;
                var threshold = (float)thresholdProperty.GetValue(startTime + sampleTime, 0.0);
                var ratio = (float)ratioProperty.GetValue(startTime + sampleTime, 2.0);
                var gain = (float)gainProperty.GetValue(startTime + sampleTime, 0.0);
                var attack = (float)attackProperty.GetValue(startTime + sampleTime, 50.0) * 0.001F;
                var release = (float)releaseProperty.GetValue(startTime + sampleTime, 500.0) * 0.001F;

                leftProcessor.Threshold = threshold;
                rightProcessor.Threshold = threshold;
                leftProcessor.Ratio = ratio;
                rightProcessor.Ratio = ratio;
                leftProcessor.MakeupGain = gain;
                rightProcessor.MakeupGain = gain;
                leftProcessor.Attack = attack;
                rightProcessor.Attack = attack;
                leftProcessor.Release = release;
                rightProcessor.Release = release;

                audio[i] = leftProcessor.Process(audio[i]);
                audio[i + 1] = rightProcessor.Process(audio[i + 1]);
            }

            return audio;
        }

        public void Dispose() { }
    }
}

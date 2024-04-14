using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NWaves.Filters.BiQuad;

namespace NiVE3.PresetPlugin.Effect.Audio
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Audio_ParametricEqualizer_Name, "mes51", "オーディオ", LanguageResourceDictionary.Audio_ParametricEqualizer_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary), SupportedSource = EffectSupportedSource.Audio)]
    public class ParametricEqualizer : IEffect
    {
        const string ID = "E003C444-877F-47AD-8936-05B89EE5C53F";

        const string PropertyBandPointsId = nameof(PropertyBandPointsId);

        const string PropertyBandPointPeakId = nameof(PropertyBandPointPeakId);

        const string PropertyBandPointHighPassId = nameof(PropertyBandPointHighPassId);

        const string PropertyBandPointLowPassId = nameof(PropertyBandPointLowPassId);

        const string PropertyBandPointHighShelfId = nameof(PropertyBandPointHighShelfId);

        const string PropertyBandPointLowShelfId = nameof(PropertyBandPointLowShelfId);

        const string PropertyBandPointFrequencyId = nameof(PropertyBandPointFrequencyId);

        const string PropertyBandPointQId = nameof(PropertyBandPointQId);

        const string PropertyBandPointGainId = nameof(PropertyBandPointGainId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new AppendableProperty(PropertyBandPointsId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoints,
                [
                    new AppendablePropertyItem(PropertyBandPointPeakId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_Peak, () =>
                    {
                        return new PropertyGroup(PropertyBandPointPeakId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_Peak,
                        [
                            new DoubleProperty(PropertyBandPointFrequencyId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Frequency, 800.0, 20.0, 24000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Hertz),
                            new DoubleProperty(PropertyBandPointQId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Q, 1.0, 0.1, 50.0, digit: 2),
                            new DoubleProperty(PropertyBandPointGainId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Gain, 0.0, -30.0, 30.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel)
                        ]);
                    }),
                    new AppendablePropertyItem(PropertyBandPointHighPassId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_HighPass, () =>
                    {
                        return new PropertyGroup(PropertyBandPointHighPassId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_HighPass,
                        [
                            new DoubleProperty(PropertyBandPointFrequencyId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Frequency, 100.0, 20.0, 24000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Hertz),
                            new DoubleProperty(PropertyBandPointQId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Q, 1.0, 0.1, 50.0, digit: 2)
                        ]);
                    }),
                    new AppendablePropertyItem(PropertyBandPointLowPassId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_LowPass, () =>
                    {
                        return new PropertyGroup(PropertyBandPointLowPassId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_LowPass,
                        [
                            new DoubleProperty(PropertyBandPointFrequencyId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Frequency, 16000.0, 20.0, 24000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Hertz),
                            new DoubleProperty(PropertyBandPointQId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Q, 1.0, 0.1, 50.0, digit: 2)
                        ]);
                    }),
                    new AppendablePropertyItem(PropertyBandPointHighShelfId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_HighShelf, () =>
                    {
                        return new PropertyGroup(PropertyBandPointHighShelfId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_HighShelf,
                        [
                            new DoubleProperty(PropertyBandPointFrequencyId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Frequency, 100.0, 20.0, 24000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Hertz),
                            new DoubleProperty(PropertyBandPointQId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Q, 1.0, 0.1, 50.0, digit: 2),
                            new DoubleProperty(PropertyBandPointGainId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Gain, 0.0, -30.0, 30.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel)
                        ]);
                    }),
                    new AppendablePropertyItem(PropertyBandPointLowShelfId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_LowShelf, () =>
                    {
                        return new PropertyGroup(PropertyBandPointLowShelfId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_BandPoint_LowShelf,
                        [
                            new DoubleProperty(PropertyBandPointFrequencyId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Frequency, 16000.0, 20.0, 24000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Hertz),
                            new DoubleProperty(PropertyBandPointQId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Q, 1.0, 0.1, 50.0, digit: 2),
                            new DoubleProperty(PropertyBandPointGainId, LanguageResourceDictionary.ResourceKeys.Audio_ParametricEqualizer_Gain, 0.0, -30.0, 30.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel)
                        ]);
                    })
                ])
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            var leftFilters = new List<BiQuadFilter>();
            var rightFilters = new List<BiQuadFilter>();

            var bands = properties.First();
            var band = (PropertyValueGroup[])(bands.GetValue(startTime) ?? Array.Empty<PropertyValueGroup>());
            for (var i = 0; i < band.Length; i++)
            {
                var freq = (double)(band[i][PropertyBandPointFrequencyId] ?? 0.0) / Const.AudioSamplingRate;
                var q = (double)(band[i][PropertyBandPointQId] ?? 0.0);
                band[i].TryGetValue(PropertyBandPointGainId, out var gain);
                switch (band[i].PropertyGroupId)
                {
                    case PropertyBandPointPeakId:
                        leftFilters.Add(new PeakFilter(freq, q, (double)(gain ?? 0.0)));
                        rightFilters.Add(new PeakFilter(freq, q, (double)(gain ?? 0.0)));
                        break;
                    case PropertyBandPointHighPassId:
                        leftFilters.Add(new HighPassFilter(freq, q));
                        rightFilters.Add(new HighPassFilter(freq, q));
                        break;
                    case PropertyBandPointLowPassId:
                        leftFilters.Add(new LowPassFilter(freq, q));
                        rightFilters.Add(new LowPassFilter(freq, q));
                        break;
                    case PropertyBandPointHighShelfId:
                        leftFilters.Add(new HighShelfFilter(freq, q, (double)(gain ?? 0.0)));
                        rightFilters.Add(new HighShelfFilter(freq, q, (double)(gain ?? 0.0)));
                        break;
                    case PropertyBandPointLowShelfId:
                        leftFilters.Add(new LowShelfFilter(freq, q, (double)(gain ?? 0.0)));
                        rightFilters.Add(new LowShelfFilter(freq, q, (double)(gain ?? 0.0)));
                        break;
                }
            }

            var leftFilterSpan = CollectionsMarshal.AsSpan(leftFilters);
            var rightFilterSpan = CollectionsMarshal.AsSpan(rightFilters);
            for (var i = 0; i < audio.Length; i += 2)
            {
                var bandValue = (PropertyValueGroup[])(bands.GetValue(startTime) ?? Array.Empty<PropertyValueGroup>());
                for (var bi = 0; bi < bandValue.Length; bi++)
                {
                    var freq = (double)(bandValue[bi][PropertyBandPointFrequencyId] ?? 0.0) / Const.AudioSamplingRate;
                    var q = (double)(bandValue[bi][PropertyBandPointQId] ?? 0.0);
                    bandValue[bi].TryGetValue(PropertyBandPointGainId, out var gain);
                    switch (bandValue[bi].PropertyGroupId)
                    {
                        case PropertyBandPointPeakId:
                            ((PeakFilter)leftFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            ((PeakFilter)rightFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            break;
                        case PropertyBandPointHighPassId:
                            ((HighPassFilter)leftFilterSpan[bi]).Change(freq, q);
                            ((HighPassFilter)rightFilterSpan[bi]).Change(freq, q);
                            break;
                        case PropertyBandPointLowPassId:
                            ((LowPassFilter)leftFilterSpan[bi]).Change(freq, q);
                            ((LowPassFilter)rightFilterSpan[bi]).Change(freq, q);
                            break;
                        case PropertyBandPointHighShelfId:
                            ((HighShelfFilter)leftFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            ((HighShelfFilter)rightFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            break;
                        case PropertyBandPointLowShelfId:
                            ((LowShelfFilter)leftFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            ((LowShelfFilter)rightFilterSpan[bi]).Change(freq, q, (double)(gain ?? 0.0));
                            break;
                    }

                    audio[i] = leftFilterSpan[bi].Process(audio[i]);
                    audio[i + 1] = rightFilterSpan[bi].Process(audio[i + 1]);
                }
            }

            return audio;
        }

        public void Dispose() { }
    }
}

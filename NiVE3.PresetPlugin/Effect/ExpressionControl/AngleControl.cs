using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.ExpressionControl
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_AngleControl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ExpressionControl, LanguageResourceDictionary.ExpressionControl_AngleControl_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class AngleControl : IEffect
    {
        const string ID = "D9DB6583-542A-4DFA-ADC7-051651554204";

        const string PropertyAngleId = nameof(PropertyAngleId);

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.ExpressionControl_AngleControl_PropertyName, 0.0, digit: 2)
            ];
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            return image;
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            return audio;
        }

        public void Dispose() { }
    }
}

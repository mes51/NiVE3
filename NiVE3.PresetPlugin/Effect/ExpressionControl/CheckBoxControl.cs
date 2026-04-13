using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
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
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_CheckBoxControl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ExpressionControl, LanguageResourceDictionary.ExpressionControl_CheckBoxControl_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class CheckBoxControl : IEffect
    {
        const string ID = "5F1EF43B-FFAE-4CB9-A3C4-9F1374CF8202";

        const string PropertyCheckBoxId = nameof(PropertyCheckBoxId);

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new CheckBoxProperty(PropertyCheckBoxId, LanguageResourceDictionary.ResourceKeys.ExpressionControl_CheckBoxControl_PropertyName, false)
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

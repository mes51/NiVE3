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
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_LayerControl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ExpressionControl, LanguageResourceDictionary.ExpressionControl_LayerControl_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LayerControl : IEffect
    {
        const string ID = "52616031-7D0B-407B-8114-25970DC02E92";

        const string PropertyLayerId = nameof(PropertyLayerId);

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertyLayerId, LanguageResourceDictionary.ResourceKeys.ExpressionControl_LayerControl_PropertyName, 90.0)
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

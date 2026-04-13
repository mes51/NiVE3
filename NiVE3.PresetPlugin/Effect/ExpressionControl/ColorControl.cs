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
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_ColorControl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ExpressionControl, LanguageResourceDictionary.ExpressionControl_ColorControl_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ColorControl : IEffect
    {
        const string ID = "5A091395-8DF1-400B-8006-8B1FD3A8DB50";

        const string PropertyColorId = nameof(PropertyColorId);

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.ExpressionControl_ColorControl_PropertyName, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One)
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

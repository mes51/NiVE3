using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.Numerics;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Effect.ExpressionControl
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_PointControl_Name, "mes51", "エクスプレッション制御", LanguageResourceDictionary.ExpressionControl_PointControl_Description, "6836A601-35DC-405D-8D77-D6DC52A36845", IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class PointControl : IEffect
    {
        public PropertyBase[] GetProperties()
        {
            return new PropertyBase[]
            {
                new Vector3dProperty("point", new LanguageResourceKey(typeof(LanguageResourceDictionary), LanguageResourceDictionary.ExpressionControl_PointControl_PropertyName), new Vector3d(), true, 2)
            };
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }

        public NImage Process(NImage image, ROI roi, double layerTime, PropertyValueGroup properties)
        {
            return image;
        }
    }
}

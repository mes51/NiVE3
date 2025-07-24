using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
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
    [EffectMetadata(LanguageResourceDictionary.ExpressionControl_Point3DControl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ExpressionControl, LanguageResourceDictionary.ExpressionControl_Point3DControl_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Point3DControl : IEffect
    {
        const string ID = "29C847AD-C9DD-422A-AD32-71D0C380DD51";

        const string PropertyPointId = nameof(PropertyPointId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var defaultValue = new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5;
            return
            [
                new Vector3dProperty(PropertyPointId, LanguageResourceDictionary.ResourceKeys.ExpressionControl_Point3DControl_PropertyName, defaultValue, digit: 2, is3D: true, useInteraction: true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

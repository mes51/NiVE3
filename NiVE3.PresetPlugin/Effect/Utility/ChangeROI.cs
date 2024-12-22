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
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Utility
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Utility_ChangeROI_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Utility, LanguageResourceDictionary.Utility_ChangeROI_Description, ID, IsDummyEffect = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class ChangeROI : IEffect
    {
        const string ID = "88E49421-9275-4454-9F95-FE8F3D2F1A93";

        const string PropertyLeftId = nameof(PropertyLeftId);

        const string PropertyTopId = nameof(PropertyTopId);

        const string PropertyRightId = nameof(PropertyRightId);

        const string PropertyBottomId = nameof(PropertyBottomId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyLeftId, LanguageResourceDictionary.ResourceKeys.Utility_ChangeROI_Left, 0.0, -ushort.MaxValue, ushort.MaxValue, digit: 0),
                new DoubleProperty(PropertyTopId, LanguageResourceDictionary.ResourceKeys.Utility_ChangeROI_Top, 0.0, -ushort.MaxValue, ushort.MaxValue, digit: 0),
                new DoubleProperty(PropertyRightId, LanguageResourceDictionary.ResourceKeys.Utility_ChangeROI_Right, 0.0, -ushort.MaxValue, ushort.MaxValue, digit: 0),
                new DoubleProperty(PropertyBottomId, LanguageResourceDictionary.ResourceKeys.Utility_ChangeROI_Bottom, 0.0, -ushort.MaxValue, ushort.MaxValue, digit: 0),
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var left = (int)properties.GetValue(PropertyLeftId, layerTime, 0.0);
            var top = (int)properties.GetValue(PropertyTopId, layerTime, 0.0);
            var right = (int)properties.GetValue(PropertyRightId, layerTime, 0.0);
            var bottom = (int)properties.GetValue(PropertyBottomId, layerTime, 0.0);

            if (left != 0 || top != 0 || right != 0 || bottom != 0)
            {
                return baseRoi.Expand(-left, -top, right, bottom);
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

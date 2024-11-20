using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;
using NiVE3.PresetPlugin.Property.Control;
using NiVE3.PresetPlugin.Property.Types;

namespace NiVE3.PresetPlugin.Property.Properties
{
    class ToneCurveProperty : PropertyBase
    {
        public ToneCurveProperty(string id, LanguageResourceKey displayNameKey, bool isSupportKeyFrame = true) : base(id, displayNameKey, ToneCurvePropertyType.Instance, ToneCurveParameters.Empty, isSupportKeyFrame) { }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            return new ToneCurvePropertyControl
            {
                DataContext = viewModel
            };
        }

        public override object? CoerceValue(object? value)
        {
            if (value is not ToneCurveParameters parameters)
            {
                return ToneCurveParameters.Empty;
            }

            return new ToneCurveParameters(
                [..parameters.Rgb.Select(p => new ToneCurvePoint(Math.Clamp(p.InValue, 0.0F, 1.0F), Math.Clamp(p.OutValue, 0.0F, 1.0F))).DistinctBy(p => p.InValue)],
                [..parameters.R.Select(p => new ToneCurvePoint(Math.Clamp(p.InValue, 0.0F, 1.0F), Math.Clamp(p.OutValue, 0.0F, 1.0F))).DistinctBy(p => p.InValue)],
                [..parameters.G.Select(p => new ToneCurvePoint(Math.Clamp(p.InValue, 0.0F, 1.0F), Math.Clamp(p.OutValue, 0.0F, 1.0F))).DistinctBy(p => p.InValue)],
                [..parameters.B.Select(p => new ToneCurvePoint(Math.Clamp(p.InValue, 0.0F, 1.0F), Math.Clamp(p.OutValue, 0.0F, 1.0F))).DistinctBy(p => p.InValue)],
                [..parameters.A.Select(p => new ToneCurvePoint(Math.Clamp(p.InValue, 0.0F, 1.0F), Math.Clamp(p.OutValue, 0.0F, 1.0F))).DistinctBy(p => p.InValue)]
            );
        }
    }
}

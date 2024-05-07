using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    public class CheckBoxProperty : PropertyBase
    {
        public CheckBoxProperty(string id, string displayName, bool defaultValue, bool isSupportKeyFrame = true) : base(id, displayName, BooleanPropertyType.Instance, defaultValue, isSupportKeyFrame) { }

        public CheckBoxProperty(string id, LanguageResourceKey displayNameKey, bool defaultValue, bool isSupportKeyFrame = true) : base(id, displayNameKey, BooleanPropertyType.Instance, defaultValue, isSupportKeyFrame) { }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new CheckBoxPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object CoerceValue(object? value)
        {
            return value ?? false;
        }

        public override bool ValidateValue(object? value)
        {
            return value is bool;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Resource;
using NiVE3.Property.Control;
using NiVE3.Property.Types;
using NiVE3.Text;

namespace NiVE3.Property
{
    class SourceTextProperty : PropertyBase
    {
        public SourceTextProperty(string id, string displayName, object? defaultValue) : base(id, displayName, SourceTextPropertyType.Instance, defaultValue, true) { }

        public SourceTextProperty(string id, LanguageResourceKey displayNameKey, object? defaultValue) : base(id, displayNameKey, SourceTextPropertyType.Instance, defaultValue, true) { }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            return new SourceTextPropertyControl();
        }

        public override object CoerceValue(object? value)
        {
            return value switch
            {
                string s => new StyledText(s, TextStyle.Empty, []),
                StyledText d => d,
                _ => StyledText.Empty
            };
        }

        public override bool ValidateValue(object? value)
        {
            return value is StyledText || value is string;
        }
    }
}

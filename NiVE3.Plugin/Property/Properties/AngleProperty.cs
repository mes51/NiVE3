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
    public class AngleProperty : PropertyBase
    {
        public int Digit { get; }

        public AngleProperty(string id, string displayName, double defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayName, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
        }

        public AngleProperty(string id, LanguageResourceKey displayNameKey, double defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayNameKey, DoublePropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new AnglePropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object CoerceValue(object? value)
        {
            return value ?? 0.0;
        }

        public override bool ValidateValue(object? value)
        {
            return value is double;
        }
    }
}

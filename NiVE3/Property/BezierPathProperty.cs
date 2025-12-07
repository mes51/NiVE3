using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Interaction;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.Property.Interaction;
using NiVE3.Property.Types;
using NiVE3.View.Property;

namespace NiVE3.Property
{
    class BezierPathProperty : PropertyBase
    {
        public BezierPathProperty(string id, string displayName) : base(id, displayName, BezierPathPropertyType.Instance, BezierPath.Empty, true)
        {
        }

        public BezierPathProperty(string id, LanguageResourceKey displayNameKey) : base(id, displayNameKey, BezierPathPropertyType.Instance, BezierPath.Empty, true)
        {
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            return new BezierPathPropertyControl
            {
                DataContext = viewModel
            };
        }

        public override object? CoerceValue(object? value)
        {
            return value as BezierPath ?? BezierPath.Empty;
        }

        public override PropertyInteractionBase? CreatePropertyInteraction(IPropertyInteractionViewModel viewModel)
        {
            return new BezierPathPropertyInteraction(viewModel);
        }
    }
}

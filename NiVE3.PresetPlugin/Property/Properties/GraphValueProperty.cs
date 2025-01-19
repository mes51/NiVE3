using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Resource;
using NiVE3.PresetPlugin.Property.Control;
using NiVE3.PresetPlugin.Property.Types;

namespace NiVE3.PresetPlugin.Property.Properties
{
    class GraphValueProperty : PropertyBase
    {
        bool IsSuppressNotifyUpdateValues { get; }

        public GraphValueProperty(string id, string displayName, bool isSuppressNotifyUpdateValue) : this(id, displayName, isSuppressNotifyUpdateValue, GraphValueParameter.LinearUp) { }

        public GraphValueProperty(string id, string displayName, bool isSuppressNotifyUpdateValue, GraphValueParameter defaultGraphValue) : base(id, displayName, GraphValuePropertyType.Instance, defaultGraphValue, true)
        {
            IsSuppressNotifyUpdateValues = isSuppressNotifyUpdateValue;
        }

        public GraphValueProperty(string id, LanguageResourceKey displayNameKey, bool isSuppressNotifyUpdateValue) : this(id, displayNameKey, isSuppressNotifyUpdateValue, GraphValueParameter.LinearUp) { }

        public GraphValueProperty(string id, LanguageResourceKey displayNameKey, bool isSuppressNotifyUpdateValue, GraphValueParameter defaultGraphValue) : base(id, displayNameKey, GraphValuePropertyType.Instance, defaultGraphValue, true)
        {
            IsSuppressNotifyUpdateValues = isSuppressNotifyUpdateValue;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            return new GraphValuePropertyControl
            {
                DataContext = viewModel,
                IsSuppressNotifyUpdateValues = IsSuppressNotifyUpdateValues
            };
        }

        public override object? CoerceValue(object? value)
        {
            if (value is not GraphValueParameter parameter)
            {
                return GraphValueParameter.Identity;
            }

            var values = parameter.Values.Select(v => Math.Clamp(v, 0.0F, 1.0F)).ToArray();
            return new GraphValueParameter(values);
        }
    }
}

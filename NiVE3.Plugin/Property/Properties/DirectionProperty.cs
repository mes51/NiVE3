using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Property.Properties
{
    public class DirectionProperty : PropertyBase
    {
        public int Digit { get; }

        public DirectionProperty(string id, string displayName, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
        }

        public DirectionProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
        }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = new DirectionPropertyControl
            {
                DataContext = viewModel
            };
            return control;
        }

        public override object? CoerceValue(object? value)
        {
            if (value is Vector3d v)
            {
                if (v.IsNaN() || v.IsInfinty())
                {
                    return DefaultValue;
                }
                else
                {
                    return (Vector3d)(value ?? Vector3d.Zero) % 360.0;
                }
            }
            else
            {
                return DefaultValue;
            }
        }
    }
}

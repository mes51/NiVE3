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

        public override object CoerceValue(object value)
        {
            return ((Vector3d)value) % 360.0;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new DirectionPropertyControl();
            control.DataContext = viewModel;
            return control;
        }

        public override bool ValidateValue(object value)
        {
            return value is Vector3d;
        }
    }
}

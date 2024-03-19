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
    internal class Scale3dProperty : PropertyBase
    {
        public int Digit { get; }

        public bool Is3D { get; }

        public Scale3dProperty(string id, string displayName, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false) : base(id, displayName, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
        }

        public Scale3dProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1, bool is3D = false) : base(id, displayNameKey, Vector3dPropertyType.Instance, defaultValue, isSupportKeyFrame)
        {
            Digit = digit;
            Is3D = is3D;
        }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = new VectorPropertyControl();
            control.DataContext = viewModel;
            control.Is3D = Is3D;
            control.Separator = ",";
            control.Unit = "%";
            control.UseLinkRatio = true;
            return control;
        }

        public override object CoerceValue(object value)
        {
            return value;
        }

        public override bool ValidateValue(object value)
        {
            return value is Vector3d;
        }
    }
}

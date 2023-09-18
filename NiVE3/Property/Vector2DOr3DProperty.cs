using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.Struct;

namespace NiVE3.Property
{
    class Vector2DOr3DProperty : Vector3dProperty
    {
        public Vector2DOr3DProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, int digit = -1, bool is3D = false) : base(id, displayNameKey, defaultValue, digit, is3D) { }

        public override PropertyControlBase CreateControl(ICompositionObject composition, ILayerObject? layer, IEffectObject? effect, IPropertyViewModel viewModel)
        {
            var control = base.CreateControl(composition, layer, effect, viewModel);

            if (layer != null)
            {
                var is3DBinding = new Binding
                {
                    Path = new PropertyPath(nameof(ILayerObject.IsEnable3D)),
                    Source = layer,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(control, VectorPropertyControl.Is3DProperty, is3DBinding);
            }

            return control;
        }
    }
}

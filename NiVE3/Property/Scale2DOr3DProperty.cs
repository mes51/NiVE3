using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;

namespace NiVE3.Property
{
    class Scale2DOr3DProperty : Scale3dProperty
    {
        public Scale2DOr3DProperty(string id, LanguageResourceKey displayNameKey, Vector3d defaultValue, bool isSupportKeyFrame = true, int digit = -1) : base(id, displayNameKey, defaultValue, isSupportKeyFrame, digit, true) { }

        public override PropertyControlBase CreateControl(ICompositionViewModel composition, ILayerViewModel? layer, IEffectViewModel? effect, IPropertyViewModel viewModel)
        {
            var control = base.CreateControl(composition, layer, effect, viewModel);

            if (layer != null)
            {
                var is3DBinding = new Binding
                {
                    Path = new PropertyPath(nameof(ILayerViewModel.IsEnable3D)),
                    Source = layer,
                    Mode = BindingMode.OneWay
                };
                BindingOperations.SetBinding(control, VectorPropertyControl.Is3DProperty, is3DBinding);
            }

            return control;
        }
    }
}

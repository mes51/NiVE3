using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Resource;

namespace NiVE3.Property.ViewState
{
    class DimensionDependNamePropertyViewState : PropertyViewState
    {
        LanguageResourceKey DisplayName2D { get; }

        LanguageResourceKey DisplayName3D { get; }

        ILayerObject LayerObject { get; }

        public DimensionDependNamePropertyViewState(LanguageResourceKey displayName2D, LanguageResourceKey displayName3D, ILayerObject layerObject, bool isEnabled = true, bool isVisible = true) : base((layerObject.IsEnable3D ? displayName3D.GetText() : displayName2D.GetText()) ?? "", isEnabled, isVisible)
        {
            DisplayName2D = displayName2D;
            DisplayName3D = displayName3D;
            LayerObject = layerObject;

            // TODO: PropertyViewModel = LayerViewModelと寿命は同じため大丈夫たとは思うが、リークする可能性を考慮した方が良いかも?
            ((INotifyPropertyChanged)layerObject).PropertyChanged += Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerObject.IsEnable3D))
            {
                if (LayerObject.IsEnable3D)
                {
                    DisplayName = DisplayName3D.GetText() ?? "";
                }
                else
                {
                    DisplayName = DisplayName2D.GetText() ?? "";
                }
            }
        }
    }
}

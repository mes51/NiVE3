using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;

namespace NiVE3.Property.ViewState
{
    class DimensionDependVisibilityPropertyViewState : PropertyViewState
    {
        ILayerObject LayerObject { get; }

        public DimensionDependVisibilityPropertyViewState(string sourceDisplayName, ILayerObject layerObject) : base(sourceDisplayName, true, layerObject.IsEnable3D)
        {
            LayerObject = layerObject;

            // TODO: PropertyViewModel = LayerViewModelと寿命は同じため大丈夫たとは思うが、リークする可能性を考慮した方が良いかも?
            ((INotifyPropertyChanged)layerObject).PropertyChanged += Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerObject.IsEnable3D))
            {
                IsVisible = LayerObject.IsEnable3D;
            }
        }
    }
}

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
        ILayerViewModel LayerObject { get; }

        public DimensionDependVisibilityPropertyViewState(string sourceDisplayName, ILayerViewModel layerObject) : base(sourceDisplayName, true, layerObject.IsEnable3D)
        {
            LayerObject = layerObject;
            layerObject.PropertyChanged += Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerViewModel.IsEnable3D))
            {
                IsVisible = LayerObject.IsEnable3D;
            }
        }
    }
}

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

        public DimensionDependVisibilityPropertyViewState(string displayName, ILayerObject layerObject) : base(displayName, true, layerObject.IsEnable3D)
        {
            LayerObject = layerObject;

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

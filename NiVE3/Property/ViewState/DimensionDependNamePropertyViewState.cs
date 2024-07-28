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

        ILayerViewModel LayerObject { get; }

        public DimensionDependNamePropertyViewState(LanguageResourceKey displayName2D, LanguageResourceKey displayName3D, ILayerViewModel layerObject, bool isEnabled = true, bool isVisible = true) : base((layerObject.IsEnable3D ? displayName3D.GetText() : displayName2D.GetText()) ?? "", isEnabled, isVisible)
        {
            DisplayName2D = displayName2D;
            DisplayName3D = displayName3D;
            LayerObject = layerObject;
            layerObject.PropertyChanged += Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerViewModel.IsEnable3D))
            {
                if (LayerObject.IsEnable3D)
                {
                    SourceDisplayName = DisplayName3D.GetText() ?? "";
                }
                else
                {
                    SourceDisplayName = DisplayName2D.GetText() ?? "";
                }
            }
        }
    }
}

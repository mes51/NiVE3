using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;

namespace NiVE3.ViewModel
{
    class LayerSwitchEventArgs : EventArgs
    {
        public string SwitchName { get; }

        public object Value { get; }

        public LayerSwitchEventArgs(string switchName, object value)
        {
            SwitchName = switchName;
            Value = value;
        }
    }

    class EnumEventArgs<T> : EventArgs where T : Enum
    {
        public T NewValue { get; }

        public EnumEventArgs(T newValue)
        {
            NewValue = newValue;
        }
    }

    class ReferenceLayerChangeEvent : EventArgs
    {
        public Guid? LayerId { get; }

        public ReferenceLayerChangeEvent(Guid? layerId)
        {
            LayerId = layerId;
        }
    }

    class CycledLayerEventArgs : EventArgs
    {
        public Guid LayerId { get; }

        public bool Cycled { get; set; }

        public CycledLayerEventArgs(Guid layerId)
        {
            LayerId = layerId;
        }
    }

    class EffectEnableChangeEventArgs : EventArgs
    {
        public bool IsEnabled { get; }

        public EffectEnableChangeEventArgs(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    class SelectItemEventArgs : EventArgs
    {
        public SelectItemType SelectItemType { get; }

        public KeyFrame? KeyFrame { get; }

        public PropertyViewModel? Property { get; }

        public EffectViewModel? Effect { get; }

        public LayerViewModel? Layer { get; }

        public SelectItemEventArgs(SelectItemType selectItemType, KeyFrame? keyFrame = null, PropertyViewModel? property = null, EffectViewModel? effect = null, LayerViewModel? layer = null)
        {
            SelectItemType = selectItemType;
            KeyFrame = keyFrame;
            Property = property;
            Effect = effect;
            Layer = layer;
        }

        public SelectItemEventArgs(SelectItemEventArgs prev, KeyFrame? keyFrame = null, PropertyViewModel? property = null, EffectViewModel? effect = null, LayerViewModel? layer = null)
        {
            SelectItemType = prev.SelectItemType;
            KeyFrame = keyFrame ?? prev.KeyFrame;
            Property = property ?? prev.Property;
            Effect = effect ?? prev.Effect;
            Layer = layer ?? prev.Layer;
        }
    }
}

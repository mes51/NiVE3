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

        public bool IsUserAction { get; }

        public object OriginalSender { get; }

        public object[] ObjectHierarchy { get; }

        public IViewModelShortcutCommand? CommandableOriginalParent => ObjectHierarchy.Skip(1).FirstOrDefault() as IViewModelShortcutCommand;

        public EffectViewModel? Effect => ObjectHierarchy.FirstOrDefault(o => o is EffectViewModel) as EffectViewModel;

        public LayerViewModel? Layer => ObjectHierarchy.FirstOrDefault(o => o is LayerViewModel) as LayerViewModel;

        public SelectItemEventArgs(SelectItemType selectItemType, bool isUserAction, object sender, object? commandableParent = null)
        {
            SelectItemType = selectItemType;
            IsUserAction = isUserAction;
            OriginalSender = sender;
            ObjectHierarchy = commandableParent != null ? new object[] { sender, commandableParent } : new object[] { sender };
        }

        public SelectItemEventArgs(SelectItemEventArgs prev, object parent)
        {
            SelectItemType = prev.SelectItemType;
            IsUserAction = prev.IsUserAction;
            OriginalSender = prev.OriginalSender;
            ObjectHierarchy = prev.ObjectHierarchy.Append(parent).ToArray();
        }
    }

    class PropertyValueCommitedEventArgs : EventArgs
    {
        public object? Value { get; }

        public IInternalPropertyViewModel[] PropertyHierarchy { get; }

        public PropertyValueCommitedEventArgs(object? value, IInternalPropertyViewModel sender)
        {
            Value = value;
            PropertyHierarchy = new IInternalPropertyViewModel[] { sender };
        }

        public PropertyValueCommitedEventArgs(PropertyValueCommitedEventArgs e, IInternalPropertyViewModel parent)
        {
            Value = e.Value;
            PropertyHierarchy = e.PropertyHierarchy.Append(parent).ToArray();
        }
    }
}

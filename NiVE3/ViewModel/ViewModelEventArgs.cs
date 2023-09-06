using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

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

    class BlendModeEventArgs : EventArgs
    {
        public BlendMode BlendMode { get; }

        public BlendModeEventArgs(BlendMode blendMode)
        {
            BlendMode = blendMode;
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
}

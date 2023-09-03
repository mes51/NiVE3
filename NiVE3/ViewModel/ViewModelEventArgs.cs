using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}

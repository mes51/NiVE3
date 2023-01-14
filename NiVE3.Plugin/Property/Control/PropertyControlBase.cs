using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NiVE3.Plugin.Property.Control
{
    public abstract class PropertyControlBase : UserControl
    {
        public abstract Type SupportedPropertyType { get; }
    }
}

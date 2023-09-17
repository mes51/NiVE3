using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Control
{
    public class PropertyControlBase : UserControl
    {
        protected IPropertyViewModel? ViewModel => DataContext as IPropertyViewModel;
    }
}

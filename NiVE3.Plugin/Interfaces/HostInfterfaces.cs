using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.Interfaces
{
    public interface ICompositionObject { }

    public interface ILayerObject { }

    public interface IEffectObject { }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? Value { get; set; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}

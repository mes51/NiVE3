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
    public interface IAcceleratorObject { }

    public interface ICompositionObject { }

    public interface ILayerObject
    {
        bool IsEnable3D { get; }
    }

    public interface IEffectObject { }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? CurrentTimeValue { get; set; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}

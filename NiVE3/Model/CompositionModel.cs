using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class CompositionModel : BindableBase
    {
        private ObservableCollection<LayerModel> layers = new ObservableCollection<LayerModel>();
        public ObservableCollection<LayerModel> Layers
        {
            get { return layers; }
            set
            {
                if (layers != value)
                {
                    layers.CollectionChanged -= Layers_CollectionChanged;
                    value.CollectionChanged += Layers_CollectionChanged;
                }
                SetProperty(ref layers, value);
            }
        }

        public CompositionModel()
        {
            Layers = new ObservableCollection<LayerModel>();
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}

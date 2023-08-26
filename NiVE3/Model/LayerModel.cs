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
    class LayerModel : BindableBase, IDisposable
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public Guid LayerId { get; }

        public string SourceName => FootageModel.Name;

        private ObservableCollection<EffectModel> effects = new ObservableCollection<EffectModel>();
        public ObservableCollection<EffectModel> Effects
        {
            get { return effects; }
            set
            {
                if (effects!= value)
                {
                    effects.CollectionChanged -= Effects_CollectionChanged;
                    value.CollectionChanged += Effects_CollectionChanged;
                }
                SetProperty(ref effects, value);
            }
        }

        FootageModel FootageModel { get; set; }

        public LayerModel(FootageModel footageModel) : this(footageModel, null) { }

        public LayerModel(FootageModel footageModel, Guid? layerId)
        {
            Effects = new ObservableCollection<EffectModel>();
            FootageModel = footageModel;
            Name = footageModel.Name;
            LayerId = layerId ?? Guid.NewGuid();
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public void Dispose()
        {
        }
    }
}

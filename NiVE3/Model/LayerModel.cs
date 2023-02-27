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
    class LayerModel : BindableBase
    {
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

        public LayerModel()
        {
            Effects = new ObservableCollection<EffectModel>();
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}

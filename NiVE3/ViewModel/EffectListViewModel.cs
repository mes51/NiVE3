using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Right)]
    class EffectListViewModel : SingletonePaneViewModelBase
    {
        private ObservableCollection<Tuple<string, string, Guid>> effects = new ObservableCollection<Tuple<string, string, Guid>>();
        public ObservableCollection<Tuple<string, string, Guid>> Effects
        {
            get { return effects; }
            set { SetProperty(ref effects, value); }
        }

        EffectListModel EffectListModel { get; }

        public EffectListViewModel(EffectListModel effectListModel)
        {
            EffectListModel = effectListModel;

            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.EffectListView_Title);

            foreach (var e in EffectListModel.EffectMetadatas)
            {
                Effects.Add(Tuple.Create(e.Name, e.Category, Guid.Parse(e.EffectUuid)));
            }
        }
    }
}

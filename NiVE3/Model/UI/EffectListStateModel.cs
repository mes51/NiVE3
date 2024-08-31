using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model.UI
{
    class EffectListStateModel : BindableBase
    {
        private Guid? recentUsedEffectId;
        public Guid? RecentUsedEffectId
        {
            get { return recentUsedEffectId; }
            set { SetProperty(ref recentUsedEffectId, value); }
        }

        private ObservableCollection<EffectItem> effects = [];
        public ObservableCollection<EffectItem> Effects
        {
            get { return effects; }
            set { SetProperty(ref effects, value); }
        }

        EffectListModel EffectListModel { get; }

        public EffectListStateModel(EffectListModel effectListModel)
        {
            EffectListModel = effectListModel;

            foreach (var e in effectListModel.EffectMetadatas)
            {
                var category = e.Category;
                if (DefaultLanguageResourceNames.EffectCategories.Contains(category))
                {
                    category = LanguageResourceDictionary.Dictionary.GetText(category);
                    if (category.Length < 1)
                    {
                        category = e.Category;
                    }
                }
                Effects.Add(new EffectItem(e.Name, category, Guid.Parse(e.EffectUuid)));
            }
        }
    }

    record EffectItem(string Name, string Category, Guid PluginId);
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using NiVE3.Model;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.Extension;
using Prism.Mvvm;
using System.Text.RegularExpressions;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left2Top)]
    partial class EffectListViewModel : SingletonePaneViewModelBase, IDragSource
    {
        [GeneratedRegex("\\s+", RegexOptions.Compiled)]
        private static partial Regex GenerateFilterSeparatorRegex();
        static readonly Regex FilterSeparatorRegex = GenerateFilterSeparatorRegex();

        private string filterText = "";
        public string FilterText
        {
            get { return filterText; }
            set { SetProperty(ref filterText, value); }
        }

        private ObservableCollection<Tuple<string, string, Guid>> effects = [];
        public ObservableCollection<Tuple<string, string, Guid>> Effects
        {
            get { return effects; }
            set { SetProperty(ref effects, value); }
        }

        public ICollectionView FilteredEffects { get; }

        EffectListModel EffectListModel { get; }

        public EffectListViewModel(EffectListModel effectListModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.EffectListView_Title);
            EffectListModel = effectListModel;
            FilteredEffects = Effects.CreateCollectionView(() => FilterText, FilterEffect);

            foreach (var e in EffectListModel.EffectMetadatas)
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
                Effects.Add(Tuple.Create(e.Name, category, Guid.Parse(e.EffectUuid)));
            }

            PropertyChanged += EffectListViewModel_PropertyChanged;
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            dragInfo.Data = new EffectListDragData(dragInfo.SourceItems.Cast<Tuple<string, string, Guid>>().Select(t => t.Item3).ToArray());
            dragInfo.Effects = DragDropEffects.Copy;
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return true;
        }

        public void Dropped(IDropInfo dropInfo) { }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public void DragCancelled() { }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }

        static bool FilterEffect(Tuple<string, string, Guid> effect, string filterKey)
        {
            if (string.IsNullOrEmpty(filterKey))
            {
                return true;
            }

            var keys = FilterSeparatorRegex.Split(filterKey);
            return keys.All(effect.Item1.Contains) || keys.All(effect.Item2.Contains);
        }

        private void EffectListViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterText))
            {
                FilteredEffects.Refresh();
            }
        }
    }

    record EffectListDragData(Guid[] Effects) { }
}

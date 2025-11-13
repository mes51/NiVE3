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
using System.Text.RegularExpressions;
using NiVE3.Model.UI;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left2Top)]
    [UseReactiveProperty]
    partial class EffectListViewModel : SingletonePaneViewModelBase, IDragSource
    {
        [GeneratedRegex("\\s+", RegexOptions.Compiled)]
        private static partial Regex GenerateFilterSeparatorRegex();

        [ReactiveProperty]
        public partial string FilterText { get; set; } = "";

        [ReactiveProperty]
        public partial ObservableCollection<EffectItem> Effects { get; set; } = [];

        public ICollectionView FilteredEffects { get; }

        EffectListModel EffectListModel { get; }

        EffectListStateModel EffectListStateModel { get; }

        public EffectListViewModel(EffectListModel effectListModel, EffectListStateModel effectListStateModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.EffectListView_Title);
            EffectListModel = effectListModel;
            EffectListStateModel = effectListStateModel;
            FilteredEffects = Effects.CreateCollectionView(() => FilterText, FilterEffect);

            foreach (var e in effectListStateModel.Effects)
            {
                Effects.Add(e);
            }

            PropertyChanged += EffectListViewModel_PropertyChanged;
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            dragInfo.Data = new EffectListDragData(dragInfo.SourceItems.Cast<EffectItem>().Select(t => t.PluginId).ToArray());
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

        static bool FilterEffect(EffectItem effect, string filterKey)
        {
            if (string.IsNullOrEmpty(filterKey))
            {
                return true;
            }

            var keys = GenerateFilterSeparatorRegex().Split(filterKey);
            return keys.All(effect.Name.Contains) || keys.All(effect.Category.Contains);
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

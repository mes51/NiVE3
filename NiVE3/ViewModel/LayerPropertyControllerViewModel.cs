using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(Composition), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    [PaneLocation(PaneLocation.Left2Center)]
    [CommandHandling(nameof(DeleteCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    [CommandHandling(nameof(CutCommand), nameof(ShortcutKeySetting.CutItemGesture))]
    [CommandHandling(nameof(CopyCommand), nameof(ShortcutKeySetting.CopyItemGesture))]
    [CommandHandling(nameof(PasteCommand), nameof(ShortcutKeySetting.PasteItemGesture))]
    [CommandHandling(nameof(DuplicateCommand), nameof(ShortcutKeySetting.DuplicateItemGesture))]
    [CommandHandling(nameof(AddRectangleMaskCommand), nameof(ShortcutKeySetting.AddRectangleMaskGesture))]
    [CommandHandling(nameof(AddEllipseMaskCommand), nameof(ShortcutKeySetting.AddEllipseMaskGesture))]
    [CommandHandling(nameof(AddBezierMaskCommand), nameof(ShortcutKeySetting.AddBezierMaskGesture))]
    [CommandHandling(nameof(ChangeLayerTagsRandomlyCommand), nameof(ShortcutKeySetting.ChangeLayerTagsRandomlyGesture))]
    partial class LayerPropertyControllerViewModel : SingletonePaneViewModelBase
    {
        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerNameColumnWidth))]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerSwitchColumnWidth))]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private Guid? lastSelectedLayerId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? LastSelectedLayerId
        {
            get { return lastSelectedLayerId; }
            set { SetProperty(ref lastSelectedLayerId, value); }
        }

        private Guid? currentEditingCompositionId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private Time timeBarRange;
        [ManualWire(nameof(Composition))]
        public Time TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private Time timeBarRangeStart;
        [ManualWire(nameof(Composition))]
        public Time TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }
        private double frameRate;
        [ManualWire(nameof(Composition), BindTargetName = nameof(CompositionModel.FrameRate), IsOneWay = true)]
        public double CompositionFrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private LayerViewModel? targetLayer;
        public LayerViewModel? TargetLayer
        {
            get { return targetLayer; }
            set { SetProperty(ref targetLayer, value); }
        }

        public ICommand DeleteCommand { get; }

        public ICommand CutCommand { get; }

        public ICommand CopyCommand { get; }

        public ICommand PasteCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand SelectAllCommand { get; }

        public ICommand AddEffectCommand { get; }

        public ICommand AddRectangleMaskCommand { get; }

        public ICommand AddEllipseMaskCommand { get; }

        public ICommand AddBezierMaskCommand { get; }

        public ICommand ChangeLayerTagsRandomlyCommand { get; }

        ProjectModel ProjectModel { get; }

        EffectListStateModel EffectListStateModel { get; }

        ViewStateModel ViewState { get; }

        EventHubModel EventHubModel { get; }

        private CompositionModel? compositionModel;
        public CompositionModel? Composition
        {
            get { return compositionModel; }
            set
            {
                if (compositionModel == value)
                {
                    return;
                }

                if (compositionModel != null)
                {
                    UnbindComposition();
                }
                SetProperty(ref compositionModel, value);
                if (value != null)
                {
                    BindComposition();
                }
            }
        }

        private Dictionary<string, List<EffectItem>> groupedEffects = [];
        public Dictionary<string, List<EffectItem>> GroupedEffects
        {
            get { return groupedEffects; }
            set { SetProperty(ref groupedEffects, value); }
        }

        public LayerPropertyControllerViewModel(ProjectModel project, ViewStateModel viewState, EffectListStateModel effectListStateModel, EventHubModel eventHubModel)
        {
            ProjectModel = project;
            ViewState = viewState;
            EffectListStateModel = effectListStateModel;
            EventHubModel = eventHubModel;
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title_Empty);

            foreach (var e in effectListStateModel.Effects)
            {
                if (!GroupedEffects.TryGetValue(e.Category, out var value))
                {
                    value = [];
                    GroupedEffects.Add(e.Category, value);
                }

                value.Add(e);
            }

            DeleteCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.DeleteCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            CutCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.CutCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            CopyCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.CopyCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            PasteCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.PasteCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            DuplicateCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.DuplicateCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            SelectAllCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                TargetLayer.SelectAllCommand.Execute(TargetLayer.SelectedMasks.Count > 0 ? SelectItemType.Mask : SelectItemType.Effect);
            }, () => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddEffectCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddEffectsToLayers([TargetLayer.LayerId], [effectItem.PluginId]);
            }, _ => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddRectangleMaskCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddShapedMaskToLayers([TargetLayer.LayerId], MaskShapeType.Rectangle);
            }, _ => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddEllipseMaskCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddShapedMaskToLayers([TargetLayer.LayerId], MaskShapeType.Ellipse);
            }, _ => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddBezierMaskCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddBezierMaskToLayers([TargetLayer.LayerId]);
            }, _ => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            ChangeLayerTagsRandomlyCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                Composition.ChangeLayerTagsRandomly([TargetLayer.LayerId]);
            }, _ => Composition != null && TargetLayer != null)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            WiringModel();

            PropertyChanged += LayerPropertyEditorViewModel_PropertyChanged;
        }

        void UpdateTargetLayerAndTitle()
        {
            if (Composition != null)
            {
                var trackMatteCollectionView = Composition.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                var parentLayerCollectionView = Composition.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                var layerModel = Composition.Layers.FirstOrDefault(l => l.LayerId == LastSelectedLayerId);
                if (layerModel != null)
                {
                    TargetLayer = new LayerViewModel(layerModel, ViewState, EventHubModel, trackMatteCollectionView, parentLayerCollectionView);
                    TargetLayer.LayerSwitchChangeRequest += LayerViewModel_LayerSwitchChangeRequest;
                }
                else
                {
                    TargetLayer = null;
                }
            }
            else
            {
                TargetLayer = null;
            }

            if (TargetLayer != null)
            {
                var format = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title);
                Title = string.Format(format, TargetLayer.Name);
            }
            else
            {
                Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title_Empty);
            }
        }

        partial void WiringModel();

        partial void BindComposition();

        partial void UnbindComposition();

        private void LayerPropertyEditorViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentEditingCompositionId):
                    if (CurrentEditingCompositionId != Composition?.CompositionId)
                    {
                        Composition = ProjectModel.CompositionModels.FirstOrDefault(c => c.CompositionId == CurrentEditingCompositionId);
                        UpdateTargetLayerAndTitle();
                    }
                    break;
                case nameof(LastSelectedLayerId):
                    UpdateTargetLayerAndTitle();
                    break;
            }
        }

        private void LayerViewModel_LayerSwitchChangeRequest(object? sender, LayerSwitchEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || Composition == null)
            {
                return;
            }

            Composition.ChangeLayerSwitches([layerViewModel.LayerId], e.SwitchName, e.Value);
        }
    }
}

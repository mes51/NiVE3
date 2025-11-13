using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Commands;

namespace NiVE3.ViewModel
{
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
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(Composition), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    partial class LayerPropertyControllerViewModel : SingletonePaneViewModelBase
    {
        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerNameColumnWidth))]
        public partial double LayerNameColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerSwitchColumnWidth))]
        public partial double LayerSwitchColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial Guid? LastSelectedLayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial Guid? CurrentEditingCompositionId { get; set; }

        [ReactiveProperty]
        [ManualWire(nameof(Composition))]
        public partial Time TimeBarRange { get; set; }

        [ReactiveProperty]
        [ManualWire(nameof(Composition))]
        public partial Time TimeBarRangeStart { get; set; }

        [ReactiveProperty]
        [ManualWire(nameof(Composition), BindTargetName = nameof(CompositionModel.FrameRate), IsOneWay = true)]
        public partial double CompositionFrameRate { get; set; }

        [ReactiveProperty]
        public partial LayerViewModel? TargetLayer { get; set; }

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

        public CompositionModel? Composition
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                if (field != null)
                {
                    UnbindComposition();
                }
                SetProperty(ref field, value);
                if (value != null)
                {
                    BindComposition();
                }
            }
        }

        [ReactiveProperty]
        public partial Dictionary<string, List<EffectItem>> GroupedEffects { get; set; } = [];

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

            AddRectangleMaskCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddShapedMaskToLayers([TargetLayer.LayerId], MaskShapeType.Rectangle);
            }, () => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddEllipseMaskCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddShapedMaskToLayers([TargetLayer.LayerId], MaskShapeType.Ellipse);
            }, () => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            AddBezierMaskCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null || TargetLayer.IsSpecial)
                {
                    return;
                }

                Composition.AddBezierMaskToLayers([TargetLayer.LayerId]);
            }, () => Composition != null && TargetLayer != null && !TargetLayer.IsSpecial)
                .ObservesProperty(() => Composition)
                .ObservesProperty(() => TargetLayer);

            ChangeLayerTagsRandomlyCommand = new DelegateCommand(() =>
            {
                if (Composition == null || TargetLayer == null)
                {
                    return;
                }

                Composition.ChangeLayerTagsRandomly([TargetLayer.LayerId]);
            }, () => Composition != null && TargetLayer != null)
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

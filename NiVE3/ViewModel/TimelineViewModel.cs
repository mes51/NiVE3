using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using GongSolutions.Wpf.DragDrop;
using NiVE3.Config;
using NiVE3.Data.Json.Project;
using NiVE3.Image.Drawing;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Util;
using NiVE3.View.Command;
using NiVE3.View.Dialog;
using NiVE3.View.Dock;
using NiVE3.View.Primitive;
using NiVE3.View.Resource;
using NiVE3.ViewModel.Dialog;
using NiVE3.ViewModel.TimelineEditing;
using NiVE3.Shared.Extension;
using Prism.Commands;
using NiVE3.Model.UI;
using Prism.Dialogs;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Data.Clipboard;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(CompositionModel), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    [CommandHandling(nameof(BeginEditNameCommand), nameof(ShortcutKeySetting.BeginEditNameGesture))]
    [CommandHandling(nameof(AddSolidLayerCommand), nameof(ShortcutKeySetting.AddSolidLayerGesture))]
    [CommandHandling(nameof(DeleteCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    [CommandHandling(nameof(CutCommand), nameof(ShortcutKeySetting.CutItemGesture))]
    [CommandHandling(nameof(CopyCommand), nameof(ShortcutKeySetting.CopyItemGesture))]
    [CommandHandling(nameof(PasteCommand), nameof(ShortcutKeySetting.PasteItemGesture))]
    [CommandHandling(nameof(DuplicateCommand), nameof(ShortcutKeySetting.DuplicateItemGesture))]
    [CommandHandling(nameof(SelectAllCommand), nameof(ShortcutKeySetting.SelectAllGesture))]
    [CommandHandling(nameof(AddKeyFrameCommand), nameof(ShortcutKeySetting.AddKeyFrameGesture))]
    [CommandHandling(nameof(ResetPropertyCommand), nameof(ShortcutKeySetting.ResetPropertyGesture))]
    [CommandHandling(nameof(SplitLayerCommand), nameof(ShortcutKeySetting.SplitLayerGesture))]
    [CommandHandling(nameof(OpenRenderSettingCommand), nameof(ShortcutKeySetting.OpenRenderSettingGesture))]
    [CommandHandling(nameof(AddShapeCommand), nameof(ShortcutKeySetting.AddShapeLayerGesture))]
    [CommandHandling(nameof(AddCameraCommand), nameof(ShortcutKeySetting.AddCameraLayerGesture))]
    [CommandHandling(nameof(AddLightCommand), nameof(ShortcutKeySetting.AddLightLayerGesture))]
    [CommandHandling(nameof(AddNullObjectCommand), nameof(ShortcutKeySetting.AddNullObjectLayerGesture))]
    [CommandHandling(nameof(AddTextCommand), nameof(ShortcutKeySetting.AddTextLayerGesture))]
    [CommandHandling(nameof(AddRectangleMaskCommand), nameof(ShortcutKeySetting.AddRectangleMaskGesture))]
    [CommandHandling(nameof(AddEllipseMaskCommand), nameof(ShortcutKeySetting.AddEllipseMaskGesture))]
    [CommandHandling(nameof(AddBezierMaskCommand), nameof(ShortcutKeySetting.AddBezierMaskGesture))]
    [CommandHandling(nameof(CompositionSettingCommand), nameof(ShortcutKeySetting.OpenCompositionSettingGesture))]
    [CommandHandling(nameof(ChangeLayerTagsRandomlyCommand), nameof(ShortcutKeySetting.ChangeLayerTagsRandomlyGesture))]
    [CommandHandling(nameof(MoveSourceStartPointToIndicatorBaseInPointCommand), nameof(ShortcutKeySetting.MoveSourceStartPointToIndicatorBaseInPointGesture))]
    [CommandHandling(nameof(MoveSourceStartPointToIndicatorBaseOutPointCommand), nameof(ShortcutKeySetting.MoveSourceStartPointToIndicatorBaseOutPointGesture))]
    [CommandHandling(nameof(MoveInPointToIndicatorCommand), nameof(ShortcutKeySetting.MoveInPointToIndicatorGesture))]
    [CommandHandling(nameof(MoveOutPointToIndicatorCommand), nameof(ShortcutKeySetting.MoveOutPointToIndicatorGesture))]
    [CommandHandling(nameof(ShiftSourceStartPointToNextFrameCommand), nameof(ShortcutKeySetting.ShiftSourceStartPointToNextFrameGesture))]
    [CommandHandling(nameof(ShiftSourceStartPointToPreviousFrameCommand), nameof(ShortcutKeySetting.ShiftSourceStartPointToPreviousFrameGesture))]
    [CommandHandling(nameof(ShiftSourceStartPointToNext10FrameCommand), nameof(ShortcutKeySetting.ShiftSourceStartPointToNext10FrameGesture))]
    [CommandHandling(nameof(ShiftSourceStartPointToPrevious10FrameCommand), nameof(ShortcutKeySetting.ShiftSourceStartPointToPrevious10FrameGesture))]
    [CommandHandling(nameof(MoveIndicatorToNextFrameCommand), nameof(ShortcutKeySetting.MoveIndicatorToNextFrameGesture))]
    [CommandHandling(nameof(MoveIndicatorToPreviousFrameCommand), nameof(ShortcutKeySetting.MoveIndicatorToPreviousFrameGesture))]
    [CommandHandling(nameof(MoveIndicatorToNext10FrameCommand), nameof(ShortcutKeySetting.MoveIndicatorToNext10FrameGesture))]
    [CommandHandling(nameof(MoveIndicatorToPrevious10FrameCommand), nameof(ShortcutKeySetting.MoveIndicatorToPrevious10FrameGesture))]
    [CommandHandling(nameof(MoveIndicatorToCompositionBeginCommand), nameof(ShortcutKeySetting.MoveIndicatorToCompositionBeginGesture))]
    [CommandHandling(nameof(MoveIndicatorToCompositionEndCommand), nameof(ShortcutKeySetting.MoveIndicatorToCompositionEndGesture))]
    [CommandHandling(nameof(MoveIndicatorToSelectLayerInPointCommand), nameof(ShortcutKeySetting.MoveIndicatorToSelectLayerInPointGesture))]
    [CommandHandling(nameof(MoveIndicatorToSelectLayerOutPointCommand), nameof(ShortcutKeySetting.MoveIndicatorToSelectLayerOutPointGesture))]
    [CommandHandling(nameof(PlayRateChangeCommand), nameof(ShortcutKeySetting.ChangeLayerPlayRateGesture))]
    [CommandHandling(nameof(ChangeLayerFreezeFrameCommand), nameof(ShortcutKeySetting.ChangeLayerFreezeFrameGesture))]
    [CommandHandling(nameof(MoveWorkareaBeginToIndicatorCommand), nameof(ShortcutKeySetting.MoveWorkareaBeginToIndicatorGesture))]
    [CommandHandling(nameof(MoveWorkareaEndToIndicatorCommand), nameof(ShortcutKeySetting.MoveWorkareaEndToIndicatorGesture))]
    [CommandHandling(nameof(PlayOrStopCommand), nameof(ShortcutKeySetting.PlayOrStopGesture))]
    partial class TimelineViewModel : PaneViewModelBase, IDropTarget
    {
        private Guid compositionId;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public Guid CompositionId
        {
            get { return compositionId; }
            set { SetProperty(ref compositionId, value); }
        }

        private string name = "";
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private double frameRate;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private Time frameDuration;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public Time FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private Time duration;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public Time Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private Time timeBarRange;
        [ManualWire(nameof(CompositionModel))]
        public Time TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private Time timeBarRangeStart;
        [ManualWire(nameof(CompositionModel))]
        public Time TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }

        private Time currentTime;
        [ManualWire(nameof(CompositionModel))]
        public Time CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private Time workareaBegin;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public Time WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private Time workareaEnd;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public Time WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private bool isEnableShy;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableFrameBlend;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private double layerNumberColumnWudth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnWidth))]
        public double LayerNumberColumnWidth
        {
            get { return layerNumberColumnWudth; }
            set { SetProperty(ref layerNumberColumnWudth, value); }
        }

        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNameColumnWidth))]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerCommentColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnWidth))]
        public double LayerCommentColumnWidth
        {
            get { return layerCommentColumnWidth; }
            set { SetProperty(ref layerCommentColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnWidth))]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private double modeColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnWidth))]
        public double ModeColumnWidth
        {
            get { return modeColumnWidth; }
            set { SetProperty(ref modeColumnWidth, value); }
        }

        private double trackMatteColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnWidth))]
        public double TrackMatteColumnWidth
        {
            get { return trackMatteColumnWidth; }
            set { SetProperty(ref trackMatteColumnWidth, value); }
        }

        private double parentLayerColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnWidth))]
        public double ParentLayerColumnWidth
        {
            get { return parentLayerColumnWidth; }
            set { SetProperty(ref parentLayerColumnWidth, value); }
        }

        private bool isAVSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineAVSwitchColumnVisible))]
        public bool IsAVSwitchColumnVisible
        {
            get { return isAVSwitchColumnVisible; }
            set { SetProperty(ref isAVSwitchColumnVisible, value); }
        }

        private bool isTagColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTagColumnVisible))]
        public bool IsTagColumnVisible
        {
            get { return isTagColumnVisible; }
            set { SetProperty(ref isTagColumnVisible, value); }
        }

        private bool isLayerNumberColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnVisible))]
        public bool IsLayerNumberColumnVisible
        {
            get { return isLayerNumberColumnVisible; }
            set { SetProperty(ref isLayerNumberColumnVisible, value); }
        }

        private bool isLayerCommentColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnVisible))]
        public bool IsLayerCommentColumnVisible
        {
            get { return isLayerCommentColumnVisible; }
            set { SetProperty(ref isLayerCommentColumnVisible, value); }
        }

        private bool isLayerSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnVisible))]
        public bool IsLayerSwitchColumnVisible
        {
            get { return isLayerSwitchColumnVisible; }
            set { SetProperty(ref isLayerSwitchColumnVisible, value); }
        }

        private bool isModeColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnVisible))]
        public bool IsModeColumnVisible
        {
            get { return isModeColumnVisible; }
            set { SetProperty(ref isModeColumnVisible, value); }
        }

        private bool isTrackMatteColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnVisible))]
        public bool IsTrackMatteColumnVisible
        {
            get { return isTrackMatteColumnVisible; }
            set { SetProperty(ref isTrackMatteColumnVisible, value); }
        }

        private bool isParentLayerColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnVisible))]
        public bool IsParentLayerColumnVisible
        {
            get { return isParentLayerColumnVisible; }
            set { SetProperty(ref isParentLayerColumnVisible, value); }
        }

        private Guid? lastSelectedLayerId;
        [NeedWire(nameof(ViewState))]
        public Guid? LastSelectedLayerId
        {
            get { return lastSelectedLayerId; }
            set { SetProperty(ref lastSelectedLayerId, value); }
        }

        private ObservableCollection<Guid>? selectedLayerIdsForPreview;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.SelectedLayerIds))]
        public ObservableCollection<Guid>? SelectedLayerIdsForPreview
        {
            get { return selectedLayerIdsForPreview; }
            set { SetProperty(ref selectedLayerIdsForPreview, value); }
        }

        private Guid? currentEditingCompositionId;
        [NeedWire(nameof(ViewState))]
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private CompositionModel? compositionModel;
        public CompositionModel? CompositionModel
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
                    Layers = null;
                    SelectedLayers = null;
                    if (CurrentEditingCompositionId == compositionModel.CompositionId)
                    {
                        CurrentEditingCompositionId = null;
                        LastSelectedLayerId = null;
                        SelectedLayerIdsForPreview = null;
                    }
                    compositionModel.PropertyChanged -= CompositionModel_PropertyChanged;
                }
                SetProperty(ref compositionModel, value);
                if (value != null)
                {
                    BindComposition();
                    var trackMatteCollectionView = value.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                    var parentLayerCollectionView = value.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                    Layers = value.Layers.CreateViewCollection(m =>
                    {
                        var vm = new LayerViewModel(m, ViewState, EventHubModel, trackMatteCollectionView, parentLayerCollectionView);
                        vm.LayerSwitchChangeRequest += LayerViewModel_LayerSwitchChangeRequest;
                        vm.BlendModeChangeRequest += LayerViewModel_BlendModeChangeRequest;
                        vm.TrackMatteLayerChangeRequest += LayerViewModel_TrackMatteLayerChangeRequest;
                        vm.TrackMatteModeChangeRequest += ViewModel_TrackMatteModeChangeRequest;
                        vm.ParentLayerChangeRequest += ViewModel_ParentLayerChangeRequest;
                        vm.CheckCycledParentLayerRequest += ViewModel_CheckCycledParentLayerRequest;
                        vm.SelectItemChanged += ViewModel_SelectItemChanged;
                        return vm;
                    });
                    SelectedLayers = [];
                    value.PropertyChanged += CompositionModel_PropertyChanged;
                }
            }
        }

        private Time timelineScrollBarMax;
        public Time TimelineScrollBarMax
        {
            get { return timelineScrollBarMax; }
            set { SetProperty(ref timelineScrollBarMax, value); }
        }

        private ObservableCollectionView<LayerModel, LayerViewModel>? layers;
        public ObservableCollectionView<LayerModel, LayerViewModel>? Layers
        {
            get { return layers; }
            set
            {
                if (layers != null)
                {
                    layers.CollectionChanged -= Layers_CollectionChanged;
                }
                SetProperty(ref layers, value);
                if (value != null)
                {
                    value.CollectionChanged += Layers_CollectionChanged;
                }
            }
        }

        private ObservableCollection<LayerViewModel>? selectedLayers;
        public ObservableCollection<LayerViewModel>? SelectedLayers
        {
            get { return selectedLayers; }
            set
            {
                if (selectedLayers != null)
                {
                    selectedLayers.CollectionChanged -= SelectedLayers_CollectionChanged;
                }
                if (value != null)
                {
                    value.CollectionChanged += SelectedLayers_CollectionChanged;
                }
                SetProperty(ref selectedLayers, value);
            }
        }

        private bool isScrubbing;
        public bool IsScrubbing
        {
            get { return isScrubbing; }
            set { SetProperty(ref isScrubbing, value); }
        }

        private Dictionary<string, List<EffectItem>> groupedEffects = [];
        public Dictionary<string, List<EffectItem>> GroupedEffects
        {
            get { return groupedEffects; }
            set { SetProperty(ref groupedEffects, value); }
        }

        private ProceduralInputItem[] proceduralInputItems = [];
        public ProceduralInputItem[] ProceduralInputItems
        {
            get { return proceduralInputItems; }
            set { SetProperty(ref proceduralInputItems, value); }
        }

        public ICommand ChangeEnableShyCommand { get; }

        public ICommand ChangeEnableFrameBlendCommand { get; }

        public ICommand ChangeEnableMotionBlurCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand ChangeWorkareaCommand { get; }

        public ICommand AddSolidLayerCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand CutCommand { get; }

        public ICommand CopyCommand { get; }

        public ICommand PasteCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand SelectAllCommand { get; }

        public ICommand AddKeyFrameCommand { get; }

        public ICommand ResetPropertyCommand { get; }

        public ICommand SplitLayerCommand { get; }

        public ICommand ChangeCurrentTimeCommand { get; }

        public ICommand AddShapeCommand { get; }

        public ICommand AddCameraCommand { get; }

        public ICommand AddLightCommand { get; }

        public ICommand AddNullObjectCommand { get; }

        public ICommand AddTextCommand { get; }

        public ICommand AddProceduralFootageCommand { get; }

        public ICommand AddEffectCommand { get; }

        public ICommand AddRectangleMaskCommand { get; }

        public ICommand AddEllipseMaskCommand { get; }

        public ICommand AddBezierMaskCommand { get; }

        public ICommand ChangeLayerTagsRandomlyCommand { get; }

        public ICommand MoveInPointToIndicatorCommand { get; }

        public ICommand MoveOutPointToIndicatorCommand { get; }

        public ICommand MoveSourceStartPointToIndicatorBaseInPointCommand { get; }

        public ICommand MoveSourceStartPointToIndicatorBaseOutPointCommand { get; }

        public ICommand ShiftSourceStartPointToNextFrameCommand { get; }

        public ICommand ShiftSourceStartPointToPreviousFrameCommand { get; }

        public ICommand ShiftSourceStartPointToNext10FrameCommand { get; }

        public ICommand ShiftSourceStartPointToPrevious10FrameCommand { get; }

        public ICommand CompositionSettingCommand { get; }

        public ICommand OpenRenderSettingCommand { get; }

        public ICommand MoveIndicatorToNextFrameCommand { get; }

        public ICommand MoveIndicatorToPreviousFrameCommand { get; }

        public ICommand MoveIndicatorToNext10FrameCommand { get; }

        public ICommand MoveIndicatorToPrevious10FrameCommand { get; }

        public ICommand MoveIndicatorToCompositionBeginCommand { get; }

        public ICommand MoveIndicatorToCompositionEndCommand { get; }

        public ICommand MoveIndicatorToSelectLayerInPointCommand { get; }

        public ICommand MoveIndicatorToSelectLayerOutPointCommand { get; }

        public ICommand PlayOrStopCommand { get; set; }

        public ICommand PlayRateChangeCommand { get; }

        public ICommand ChangeLayerFreezeFrameCommand { get; }

        public ICommand MoveWorkareaBeginToIndicatorCommand { get; }

        public ICommand MoveWorkareaEndToIndicatorCommand { get; }

        WeakEventPublisher<EventArgs> CurrentTimeChangeByUserPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> CurrentTimeChangeByUser
        {
            add { CurrentTimeChangeByUserPublisher.Subscribe(value); }
            remove { CurrentTimeChangeByUserPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> FocusRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> FocusRequest
        {
            add { FocusRequestPublisher.Subscribe(value); }
            remove { FocusRequestPublisher.Unsubscribe(value); }
        }

        private IViewModelShortcutCommand? selectedTarget;
        public IViewModelShortcutCommand? SelectedTarget
        {
            get { return selectedTarget; }
            set { SetProperty(ref selectedTarget, value); }
        }

        private SelectItemType selectedItemType = SelectItemType.None;
        public SelectItemType SelectedItemType
        {
            get { return selectedItemType; }
            set { SetProperty(ref selectedItemType, value); }
        }

        ViewStateModel ViewState { get; }

        EffectListStateModel EffectListStateModel { get; }

        ProceduralInputListModel ProceduralInputListModel { get; }

        AudioPlayerModel AudioPlayerModel { get; }

        HistoryModel HistoryModel { get; }

        EventHubModel EventHubModel { get; }

        IDialogService DialogService { get; }

        bool IsUsingTool { get; set; }

        bool IsEditingDuration { get; set; }

        bool IsEditingAny => IsUsingTool || IsEditingDuration;

        PreviewManipulationStateBase? PreviewManipulation { get; set; }

        DurationManipulationStateBase? DurationManipulation { get; set; }

        public TimelineViewModel(ViewStateModel viewState, EffectListStateModel effectListStateModel, ProceduralInputListModel proceduralInputListModel, AudioPlayerModel audioPlayerModel, HistoryModel historyModel, EventHubModel eventHubModel, IDialogService dialogService)
        {
            ViewState = viewState;
            EffectListStateModel = effectListStateModel;
            ProceduralInputListModel = proceduralInputListModel;
            AudioPlayerModel = audioPlayerModel;
            HistoryModel = historyModel;
            EventHubModel = eventHubModel;
            DialogService = dialogService;
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Timeline_EmptyTitle);
            SelectedLayers = [];

            foreach (var e in effectListStateModel.Effects)
            {
                if (!GroupedEffects.TryGetValue(e.Category, out var value))
                {
                    value = [];
                    GroupedEffects.Add(e.Category, value);
                }

                value.Add(e);
            }

            ProceduralInputItems = proceduralInputListModel.ProceduralFootageItems;

            WiringModel();

            eventHubModel.BeginUseToolRequest += EventHubModel_BeginUseToolRequest;
            eventHubModel.MoveLayersByToolRequest += EventHubModel_MoveLayersByToolRequest;
            eventHubModel.AbortUseToolRequest += EventHubModel_AbortUseToolRequest;
            eventHubModel.AddEffectToSelectedLayers += EventHubModel_AddEffectToSelectedLayers;
            eventHubModel.BeginEditDurationRequest += EventHubModel_BeginEditDurationRequest;
            eventHubModel.UpdateDurationRequest += EventHubModel_UpdateDurationRequest;
            eventHubModel.AbortEditDurationRequest += EventHubModel_AbortEditDurationRequest;
            eventHubModel.TextStyleChangeRequest += EventHubModel_TextStyleChangeRequest;
            PropertyChanged += TimelineViewModel_PropertyChanged;
            PaneSelected += TimelineViewModel_PaneSelected;

            ChangeEnableShyCommand = new DelegateCommand(() => CompositionModel?.ChangeEnableShy(), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            ChangeEnableFrameBlendCommand = new DelegateCommand(() => CompositionModel?.ChangeEnableFrameBlend(), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            ChangeEnableMotionBlurCommand = new DelegateCommand(() => CompositionModel?.ChangeEnableMotionBlur(), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget == null && SelectedLayers.Count > 0 && SelectedLayers.First().BeginEditNameCommand.CanExecute(null))
                {
                    SelectedLayers.First().BeginEditNameCommand.Execute(null);
                }
                else
                {
                    var targetChild = (SelectedTarget as INameEditableParentViewModel)?.TargetChild;
                    if (targetChild?.BeginEditNameCommand?.CanExecute(null) ?? false)
                    {
                        targetChild.BeginEditNameCommand.Execute(null);
                    }
                }
            }, () => SelectedTarget != null || SelectedLayers.Count > 0)
                .ObservesProperty(() => SelectedTarget)
                .ObservesProperty(() => SelectedLayers.Count);

            ChangeWorkareaCommand = new DelegateCommand<Tuple<Time, Time>>(t => CompositionModel?.ChangeWorkarea(t.Item1, t.Item2));

            AddSolidLayerCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || Layers == null)
                {
                    return;
                }

                if (SelectedLayers.Count > 0)
                {
                    CompositionModel.AddSolid(Layers.IndexOf(SelectedLayers.First()) + 1);
                }
                else
                {
                    CompositionModel.AddSolid(Layers.Count);
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            DeleteCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.DeleteCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (SelectedLayers.Count > 0 && SelectedLayers.All(l => l.EditingParameter == EditingLayerParameter.None))
                    {
                        var ids = SelectedLayers.Select(l => l.LayerId).ToArray();
                        CompositionModel?.DeleteLayers(ids);
                        SelectedLayers.Clear();
                        FocusRequestPublisher.Publish(this, EventArgs.Empty);
                    }
                }
            }, () => CompositionModel != null && SelectedItemType != SelectItemType.None)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            CutCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.CutCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (CompositionModel == null || SelectedLayers.Count < 1 && SelectedLayers.Any(l => l.EditingParameter != EditingLayerParameter.None))
                    {
                        return;
                    }

                    var ids = SelectedLayers.Select(l => l.LayerId).ToArray();
                    var copyData = CompositionModel.CutLayers(ids);
                    ClipboardUtil.SetData(copyData);
                    SelectedLayers.Clear();
                    FocusRequestPublisher.Publish(this, EventArgs.Empty);
                }
            }, () => CompositionModel != null && SelectedItemType != SelectItemType.None)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            CopyCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.CopyCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (CompositionModel == null || SelectedLayers.Count < 1 && SelectedLayers.Any(l => l.EditingParameter != EditingLayerParameter.None))
                    {
                        return;
                    }

                    var ids = SelectedLayers.Select(l => l.LayerId).ToArray();
                    ClipboardUtil.SetData(CompositionModel.CopyLayers(ids));
                }
            }, () => CompositionModel != null && SelectedItemType != SelectItemType.None)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            PasteCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.PasteCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (CompositionModel == null || SelectedLayers.Count < 1 && SelectedLayers.Any(l => l.EditingParameter != EditingLayerParameter.None))
                    {
                        return;
                    }

                    var layerData = ClipboardUtil.GetData<LayerData>(CopyDataType.Layer);
                    if (layerData != null)
                    {
                        CompositionModel.PasteLayers(layerData, LastSelectedLayerId);
                        return;
                    }
                    
                    var effectData = ClipboardUtil.GetData<EffectData>(CopyDataType.Effect);
                    if (effectData != null)
                    {
                        CompositionModel.PasteEffects(effectData, [..SelectedLayers.Select(l => l.LayerId)]);
                    }

                    var maskData = ClipboardUtil.GetData<MaskData>(CopyDataType.Mask);
                    if (maskData != null)
                    {
                        CompositionModel.PasteMasks(maskData, [.. SelectedLayers.Select(l => l.LayerId)]);
                    }
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            DuplicateCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.DuplicateCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (CompositionModel == null || SelectedLayers.Count < 1 && SelectedLayers.Any(l => l.EditingParameter != EditingLayerParameter.None))
                    {
                        return;
                    }

                    var ids = SelectedLayers.Select(l => l.LayerId).ToArray();
                    CompositionModel.DuplicateLayers(ids, LastSelectedLayerId);
                }
            }, () => CompositionModel != null && SelectedItemType != SelectItemType.None)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            SelectAllCommand = new DelegateCommand(() =>
            {
                if (SelectedTarget != null)
                {
                    SelectedTarget.SelectAllCommand.Execute(SelectedItemType);
                }
                else
                {
                    if (CompositionModel == null || Layers == null || Layers.Count < 1)
                    {
                        return;
                    }

                    CurrentEditingCompositionId = CompositionModel?.CompositionId;
                    SelectedLayers.Clear();
                    foreach (var layer in Layers)
                    {
                        SelectLayer(layer.LayerId, true);
                    }
                    LastSelectedLayerId = Layers[0].LayerId;
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddKeyFrameCommand = new DelegateCommand(() => SelectedTarget?.AddKeyFrameCommand?.Execute(SelectedItemType), () => CompositionModel != null && SelectedItemType == SelectItemType.Property)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            ResetPropertyCommand = new DelegateCommand(() => SelectedTarget?.ResetPropertyCommand?.Execute(SelectedItemType), () => CompositionModel != null && SelectedItemType == SelectItemType.Property)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedItemType);

            SplitLayerCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Any(l => l.EditingParameter != EditingLayerParameter.None))
                {
                    return;
                }

                var targetLayers = SelectedLayers.Count > 0 ? SelectedLayers.AsEnumerable() : (Layers ?? Enumerable.Empty<LayerViewModel>());
                var ids = targetLayers.Select(l => l.LayerId).ToArray();
                if (ids.Length > 0)
                {
                    CompositionModel.SplitLayers(ids, CurrentTime);
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            ChangeCurrentTimeCommand = new DelegateCommand(() => CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty));

            AddShapeCommand = new DelegateCommand(() => CompositionModel?.AddShape(GetFirstSelectedLayerIndex()), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddCameraCommand = new DelegateCommand(() => CompositionModel?.AddCamera(GetFirstSelectedLayerIndex()), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddLightCommand = new DelegateCommand(() => CompositionModel?.AddLight(GetFirstSelectedLayerIndex()), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddNullObjectCommand = new DelegateCommand(() => CompositionModel?.AddNullObject(GetFirstSelectedLayerIndex()), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddTextCommand = new DelegateCommand(() => CompositionModel?.AddText(GetFirstSelectedLayerIndex()), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddProceduralFootageCommand = new DelegateCommand<ProceduralInputItem>(item =>
            {
                if (CompositionModel == null)
                {
                    return;
                }

                CompositionModel.InsertLayers(item.FootageId, GetFirstSelectedLayerIndex());
            }, _ => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AddEffectCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.AddEffectsToLayers([..SelectedLayers.Select(l => l.LayerId)], [effectItem.PluginId]);
            }, _ => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            AddRectangleMaskCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.AddShapedMaskToLayers([..SelectedLayers.Select(l => l.LayerId)], MaskShapeType.Rectangle);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            AddEllipseMaskCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.AddShapedMaskToLayers([..SelectedLayers.Select(l => l.LayerId)], MaskShapeType.Ellipse);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            AddBezierMaskCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.AddBezierMaskToLayers([..SelectedLayers.Select(l => l.LayerId)]);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            ChangeLayerTagsRandomlyCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.ChangeLayerTagsRandomly([..SelectedLayers.Select(l => l.LayerId)]);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            MoveInPointToIndicatorCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerInPoint([.. SelectedLayers.Select(l => l.LayerId)], CurrentTime);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            MoveOutPointToIndicatorCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerOutPoint([.. SelectedLayers.Select(l => l.LayerId)], CurrentTime);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            MoveSourceStartPointToIndicatorBaseInPointCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPointToInPoint([..SelectedLayers.Select(l => l.LayerId)], CurrentTime);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            MoveSourceStartPointToIndicatorBaseOutPointCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPointToOutPoint([..SelectedLayers.Select(l => l.LayerId)], CurrentTime);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            ShiftSourceStartPointToNextFrameCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPoint([..SelectedLayers.Select(l => l.LayerId)], FrameDuration);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            ShiftSourceStartPointToPreviousFrameCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPoint([..SelectedLayers.Select(l => l.LayerId)], -FrameDuration);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            ShiftSourceStartPointToNext10FrameCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPoint([..SelectedLayers.Select(l => l.LayerId)], FrameDuration * 10.0);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            ShiftSourceStartPointToPrevious10FrameCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                CompositionModel.MoveLayerSourceStartPoint([..SelectedLayers.Select(l => l.LayerId)], FrameDuration * -10.0);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            CompositionSettingCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null)
                {
                    return; 
                }

                var param = new DialogParameters
                {
                    { nameof(CompositionSettingViewModel.Name), CompositionModel.Name },
                    { nameof(CompositionSettingViewModel.Width), CompositionModel.Width },
                    { nameof(CompositionSettingViewModel.Height), CompositionModel.Height },
                    { nameof(CompositionSettingViewModel.FrameRate), CompositionModel.FrameRate },
                    { nameof(CompositionSettingViewModel.Duration), CompositionModel.Duration },
                    { nameof(CompositionSettingViewModel.IsRetentionFrameRate), CompositionModel.IsRetentionFrameRate },
                    { nameof(CompositionSettingViewModel.ApplyToneMappingWhenNested), CompositionModel.ApplyToneMappingWhenNested },
                    { nameof(CompositionSettingViewModel.ShutterAngle), CompositionModel.ShutterAngle },
                    { nameof(CompositionSettingViewModel.ShutterPhase), CompositionModel.ShutterPhase },
                    { nameof(CompositionSettingViewModel.MotionBlurSampleCount), CompositionModel.MotionBlurSampleCount },
                    { CompositionSettingViewModel.SelectedRendererPluginId, CompositionModel.RendererPluginId },
                    { CompositionSettingViewModel.SelectedToneMapperPluginId, CompositionModel.ToneMapperPluginId }
                };
                if (CompositionModel.RendererSetting != null)
                {
                    param.Add(nameof(CompositionSettingViewModel.RendererSetting), CompositionModel.RendererSetting);
                }
                if (CompositionModel.ToneMapperSetting != null)
                {
                    param.Add(nameof(CompositionSettingViewModel.ToneMapperSetting), CompositionModel.ToneMapperSetting);
                }
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(CompositionSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK)
                {
                    CompositionModel.ChangeCompositionSetting(
                        result.Parameters.GetValue<string>(nameof(CompositionSettingViewModel.Name)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.Width)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.Height)),
                        result.Parameters.GetValue<double>(nameof(CompositionSettingViewModel.FrameRate)),
                        result.Parameters.GetValue<Time>(nameof(CompositionSettingViewModel.Duration)),
                        result.Parameters.GetValue<bool>(nameof(CompositionSettingViewModel.IsRetentionFrameRate)),
                        result.Parameters.GetValue<bool>(nameof(CompositionSettingViewModel.ApplyToneMappingWhenNested)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterAngle)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.ShutterPhase)),
                        result.Parameters.GetValue<int>(nameof(CompositionSettingViewModel.MotionBlurSampleCount)),
                        result.Parameters.GetValue<Guid>(CompositionSettingViewModel.SelectedRendererPluginId),
                        result.Parameters.GetValue<Guid>(CompositionSettingViewModel.SelectedToneMapperPluginId),
                        result.Parameters.ContainsKey(CompositionSettingViewModel.RendererSettingViewData),
                        result.Parameters.ContainsKey(CompositionSettingViewModel.RendererSettingViewData) ? result.Parameters.GetValue<object>(CompositionSettingViewModel.RendererSettingViewData) : null,
                        result.Parameters.ContainsKey(CompositionSettingViewModel.ToneMapperSettingViewData),
                        result.Parameters.ContainsKey(CompositionSettingViewModel.ToneMapperSettingViewData) ? result.Parameters.GetValue<object>(CompositionSettingViewModel.ToneMapperSettingViewData) : null
                    );
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            OpenRenderSettingCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null)
                {
                    return;
                }

                var settingParams = new DialogParameters
                {
                    { RenderSettingViewModel.CompositionParameterName, CompositionModel },
                    { nameof(RenderSettingViewModel.RenderRangeType), RenderRangeType.Workarea },
                    { nameof(RenderSettingViewModel.BeginTime), CompositionModel.WorkareaBegin },
                    { nameof(RenderSettingViewModel.EndTime), CompositionModel.WorkareaEnd },
                    { nameof(RenderSettingViewModel.IsOutputVideo), true },
                    { nameof(RenderSettingViewModel.IsOutputAudio), true },
                    { nameof(RenderSettingViewModel.Mode), RenderSettingMode.Enqueue },
                };
                IDialogResult? settingResult = null;
                DialogService.ShowDialog(nameof(RenderSettingView), settingParams, r => settingResult = r);
                if (settingResult?.Result == ButtonResult.OK)
                {
                    CompositionModel.EnqueueRender(
                        settingResult.Parameters.GetValue<string>(nameof(RenderSettingViewModel.FilePath)),
                        settingResult.Parameters.GetValue<RenderRangeType>(nameof(RenderSettingViewModel.RenderRangeType)),
                        settingResult.Parameters.GetValue<Time>(nameof(RenderSettingViewModel.BeginTime)),
                        settingResult.Parameters.GetValue<Time>(nameof(RenderSettingViewModel.EndTime)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputVideo)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputAudio)),
                        settingResult.Parameters.GetValue<ExportLifetimeContext<IOutput>>(RenderSettingViewModel.OutputParameterName)
                    );
                }
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToNextFrameCommand = new DelegateCommand(() =>
            {
                CurrentTime = (CurrentTime + FrameDuration).RoundToFrameRate(FrameRate);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToPreviousFrameCommand = new DelegateCommand(() =>
            {
                CurrentTime = (CurrentTime - FrameDuration).RoundToFrameRate(FrameRate);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToNext10FrameCommand = new DelegateCommand(() =>
            {
                CurrentTime = (CurrentTime + FrameDuration * 10.0).RoundToFrameRate(FrameRate);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToPrevious10FrameCommand = new DelegateCommand(() =>
            {
                CurrentTime = (CurrentTime - FrameDuration * 10.0).RoundToFrameRate(FrameRate);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToCompositionBeginCommand = new DelegateCommand(() =>
            {
                TimeBarRangeStart = Time.Zero;
                CurrentTime = Time.Zero;
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToCompositionEndCommand = new DelegateCommand(() =>
            {
                TimeBarRangeStart = Duration - TimeBarRange;
                CurrentTime = (Duration - FrameDuration).RoundToFrameRate(FrameRate);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveIndicatorToSelectLayerInPointCommand = new DelegateCommand(() =>
            {
                if (SelectedLayers.Count < 1)
                {
                    return;
                }

                var targetLayer = SelectedLayers[0];
                CurrentTime = (targetLayer.SourceStartPoint + targetLayer.InPoint).RoundToFrameRate(FrameRate);
                TimeBarRangeStart = Time.MaxAndMin(CurrentTime - TimeBarRange * 0.5, Time.Zero, Duration - TimeBarRange);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            MoveIndicatorToSelectLayerOutPointCommand = new DelegateCommand(() =>
            {
                if (SelectedLayers.Count < 1)
                {
                    return;
                }

                var targetLayer = SelectedLayers[0];
                CurrentTime = (targetLayer.SourceStartPoint + targetLayer.OutPoint - FrameDuration).RoundToFrameRate(FrameRate);
                TimeBarRangeStart = Time.MaxAndMin(CurrentTime - TimeBarRange * 0.5, Time.Zero, Duration - TimeBarRange);
                CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty);
            }, () => CompositionModel != null && SelectedLayers.Count > 0)
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers.Count);

            PlayOrStopCommand = new DelegateCommand(() => EventHubModel.NotifyPlayOrStop(), () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            PlayRateChangeCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                var baseTargetLayer = SelectedLayers[0];
                var param = new DialogParameters
                {
                    { nameof(PlayRateSettingViewModel.PlayRate), baseTargetLayer.PlayRate },
                    { nameof(PlayRateSettingViewModel.SourceDuration), baseTargetLayer.SourceDuration },
                    { nameof(PlayRateSettingViewModel.CompositionFrameRate), FrameRate }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(PlayRateSettingView), param, r => result = r);
                if (result != null && result.Result == ButtonResult.OK)
                {
                    CompositionModel.ChangeLayerPlayRate([..SelectedLayers.Select(l => l.LayerId)], result.Parameters.GetValue<double>(nameof(PlayRateSettingViewModel.PlayRate)));
                }
            }, () => CompositionModel != null && SelectedLayers.Count > 0 && SelectedLayers.All(l => l.HasDuration))
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers);

            ChangeLayerFreezeFrameCommand = new DelegateCommand(() =>
            {
                if (CompositionModel == null || SelectedLayers.Count < 1)
                {
                    return;
                }

                var baseTargetLayer = SelectedLayers[0];
                CompositionModel.ChangeFreezeFrame([..SelectedLayers.Select(l => l.LayerId)], !baseTargetLayer.IsFreezeFrame, CurrentTime);
            }, () => CompositionModel != null && SelectedLayers.Count > 0 && SelectedLayers.All(l => l.HasDuration))
                .ObservesProperty(() => CompositionModel)
                .ObservesProperty(() => SelectedLayers);

            MoveWorkareaBeginToIndicatorCommand = new DelegateCommand(() =>
            {
                CompositionModel?.ChangeWorkarea(Time.MaxAndMin(CurrentTime, Time.Zero, (WorkareaEnd - FrameDuration).RoundToFrameRate(FrameRate)), WorkareaEnd);
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            MoveWorkareaEndToIndicatorCommand = new DelegateCommand(() =>
            {
                CompositionModel?.ChangeWorkarea(WorkareaBegin, Time.MaxAndMin(CurrentTime, (WorkareaBegin + FrameDuration).RoundToFrameRate(FrameRate), Duration));
            }, () => CompositionModel != null).ObservesProperty(() => CompositionModel);

            AudioPlayerModel = audioPlayerModel;
        }

        public void SelectLayer(Guid? layerId, bool isMultiSelect)
        {
            if (layerId == null && !isMultiSelect)
            {
                SelectedLayers?.Clear();
            }
            else if (Layers != null && SelectedLayers != null)
            {
                if (isMultiSelect)
                {
                    var selected = SelectedLayers.FirstOrDefault(l => l.LayerId == layerId);
                    if (selected != null)
                    {
                        SelectedLayers.Remove(selected);
                    }
                    else
                    {
                        SelectedLayers.Add(Layers.First(l => l.LayerId == layerId));
                    }
                }
                else if (!SelectedLayers.Any(l => l.LayerId == layerId))
                {
                    SelectedLayers.Clear();
                    CurrentEditingCompositionId = CompositionModel?.CompositionId;
                    SelectedLayers.Add(Layers.First(l => l.LayerId == layerId));
                }

                if (SelectedLayers.Count > 0)
                {
                    if (SelectedItemType != SelectItemType.Layer)
                    {
                        foreach (var layer in SelectedLayers)
                        {
                            layer.DeSelect();
                        }
                    }
                    SelectedItemType = SelectItemType.Layer;
                }
                else
                {
                    SelectedItemType = SelectItemType.None;
                }
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (Layers == null || CompositionModel == null)
            {
                return;
            }

            switch (dropInfo.Data)
            {
                case IFootageViewModel:
                case IFootageViewModel[]:
                    dropInfo.Effects = DragDropEffects.Copy;
                    if (dropInfo.TargetItem is LayerViewModel)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    }
                    break;
                case LayerViewModel layer when Layers.Contains(layer):
                case ItemDragData<LayerViewModel> itemDragData when itemDragData.SelectedItems.All(Layers.Contains):
                    dropInfo.Effects = DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (Layers == null || CompositionModel == null)
            {
                return;
            }

            switch (dropInfo.Data)
            {
                case IFootageViewModel footage:
                    CompositionModel.InsertLayers(footage.FootageId, dropInfo.InsertIndex);
                    break;
                case IFootageViewModel[] footages:
                    CompositionModel.InsertLayers([..footages.Select(f => f.FootageId)], dropInfo.InsertIndex);
                    break;
                case LayerViewModel layer:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Layers.IndexOf(layer) < newIndex)
                        {
                            newIndex--;
                        }
                        CompositionModel.MoveLayer(layer.LayerId, newIndex);
                    }
                    break;
                case ItemDragData<LayerViewModel> itemDragData:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Layers.IndexOf(itemDragData.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        CompositionModel.MoveLayers([..itemDragData.SelectedItems.Select(l => l.LayerId)], itemDragData.DragItem.LayerId, newIndex);
                    }
                    break;
            }
        }

        int GetFirstSelectedLayerIndex()
        {
            if (Layers != null && SelectedLayers != null && SelectedLayers.Count > 0)
            {
                var firstSelectedId = SelectedLayers[0].LayerId;
                return Layers.FindIndex(l => l.LayerId == firstSelectedId);
            }
            else
            {
                return 0;
            }
        }

        partial void WiringModel();

        partial void BindComposition();

        partial void UnbindComposition();

        private void EventHubModel_AbortEditDurationRequest(object? sender, AbortEditDurationEventArgs e)
        {
            if (!IsEditingDuration || CompositionModel == null || Layers == null || e.CompositionId != CompositionId || DurationManipulation == null)
            {
                return;
            }

            DurationManipulation.Abort();
            IsEditingDuration = false;
            DurationManipulation = null;
        }

        private void EventHubModel_UpdateDurationRequest(object? sender, UpdateDurationEventArgs e)
        {
            if (!IsEditingDuration || CompositionModel == null || Layers == null || e.CompositionId != CompositionId || DurationManipulation == null)
            {
                return;
            }

            if (e.IsCommit)
            {
                DurationManipulation.Commit(e.InPointDiff, e.OutPointDiff, e.SourceStartPointDiff);
                IsEditingDuration = false;
                DurationManipulation = null;
            }
            else
            {
                DurationManipulation.Update(e.InPointDiff, e.OutPointDiff, e.SourceStartPointDiff);
            }
        }

        private void EventHubModel_BeginEditDurationRequest(object? sender, BeginEditDurationEventArgs e)
        {
            if (IsEditingAny || CompositionModel == null || Layers == null || e.CompositionId != CompositionId || Layers.All(l => l.LayerId != e.LayerId))
            {
                return;
            }

            var targetLayers = (SelectedLayers?.Any(l => l.LayerId == e.LayerId) ?? false) ? SelectedLayers.ToArray() : [Layers.First(l => l.LayerId == e.LayerId)];
            switch (e.Type)
            {
                case BeginEditDurationEventArgs.DurationType.InPoint:
                    DurationManipulation = new InPointDurationManipulationState(CompositionModel, targetLayers, HistoryModel);
                    break;
                case BeginEditDurationEventArgs.DurationType.OutPoint:
                    DurationManipulation = new OutPointDurationManipulationState(CompositionModel, targetLayers, HistoryModel);
                    break;
                case BeginEditDurationEventArgs.DurationType.SourceStartPoint:
                    DurationManipulation = new SourceStartPointDurationManipulationState(CompositionModel, targetLayers, HistoryModel);
                    break;
                case BeginEditDurationEventArgs.DurationType.Slip:
                    DurationManipulation = new SlipDurationManipulationState(CompositionModel, targetLayers, HistoryModel);
                    break;
            }

            IsEditingDuration = DurationManipulation != null;
        }

        private void EventHubModel_AddEffectToSelectedLayers(object? sender, AddEffectEventArgs e)
        {
            if (IsEditingAny || CompositionModel == null || Layers == null || e.CompositionId != CompositionId)
            {
                return;
            }

            if (e.TargetLayerId.HasValue)
            {
                if (Layers.All(l => l.LayerId != e.TargetLayerId))
                {
                    return;
                }

                if ((SelectedLayers != null && SelectedLayers.Any(l => l.LayerId == e.TargetLayerId)))
                {
                    CompositionModel.AddEffectsToLayers([.. SelectedLayers.Select(l => l.LayerId)], e.EffectPluginIds);
                }
                else
                {
                    CompositionModel.AddEffectsToLayers([e.TargetLayerId.Value], e.EffectPluginIds);
                }
            }
            else if (SelectedLayers != null && SelectedLayers.Count > 0)
            {
                CompositionModel.AddEffectsToLayers([.. SelectedLayers.Select(l => l.LayerId)], e.EffectPluginIds);
            }
        }

        private void EventHubModel_AbortUseToolRequest(object? sender, AbortUseToolEventArgs e)
        {
            if (!IsUsingTool || CompositionModel == null || e.CompositionId != CompositionId || PreviewManipulation == null)
            {
                return;
            }

            PreviewManipulation.Abort();
            IsUsingTool = false;
            PreviewManipulation = null;
        }

        private void EventHubModel_MoveLayersByToolRequest(object? sender, MoveLayersByToolEventArgs e)
        {
            if (!IsUsingTool || CompositionModel == null || e.CompositionId != CompositionId || PreviewManipulation == null)
            {
                return;
            }

            if (e.IsCommit)
            {
                PreviewManipulation.Commit(e.NextScreenPos);
                IsUsingTool = false;
                PreviewManipulation = null;
            }
            else
            {
                PreviewManipulation.Update(e.NextScreenPos);
            }
        }

        private void EventHubModel_BeginUseToolRequest(object? sender, BeginUseToolEventArgs e)
        {
            if (IsEditingAny || CompositionModel == null || Layers == null || e.CompositionId != CompositionId || (SelectedItemType != SelectItemType.Layer && e.Type.HasFlag(BeginUseToolEventArgs.PropertyType.LayerProperty)))
            {
                return;
            }

            Guid? activeCameraId;
            CameraSetting? cameraSetting;
            Guid? baseLayerId;
            LayerSkeleton? baseLayerSkeleton;
            using (var checker = CycleChecker.StartCheck())
            {
                activeCameraId = CompositionModel.GetActiveCamera(CurrentTime)?.LayerId;
                cameraSetting = CompositionModel.GetActiveCameraSetting(CurrentTime);
                baseLayerId = CompositionModel.FindLayerByPreviewPosition(CurrentTime, e.StartScreenPosition);
                baseLayerSkeleton = baseLayerId.HasValue ? CompositionModel.GetLayerSkeleton(baseLayerId.Value, CurrentTime) : null;
            }
            var baseLayerIs3D = baseLayerSkeleton?.IsEnable3D ?? false;
            var imageLayers = SelectedLayers?.Where(l => l.HasImage)?.ToArray() ?? [];

            switch (e.Type)
            {
                case BeginUseToolEventArgs.PropertyType.Transform when imageLayers.Length > 0 && baseLayerSkeleton != null:
                    PreviewManipulation = new PositionPreviewManipulationState(imageLayers, baseLayerSkeleton, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
                case BeginUseToolEventArgs.PropertyType.RotateAll when imageLayers.Length > 0 && !baseLayerIs3D && baseLayerSkeleton != null:
                case BeginUseToolEventArgs.PropertyType.RotateX when imageLayers.Length > 0 &&!baseLayerIs3D && baseLayerSkeleton != null:
                case BeginUseToolEventArgs.PropertyType.RotateY when imageLayers.Length > 0 && !baseLayerIs3D && baseLayerSkeleton != null:
                case BeginUseToolEventArgs.PropertyType.RotateZ when imageLayers.Length > 0 && baseLayerSkeleton != null:
                    PreviewManipulation = new RotateZPreviewManipulationState(imageLayers, baseLayerSkeleton, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
                case BeginUseToolEventArgs.PropertyType.RotateAll when baseLayerSkeleton != null:
                    imageLayers = [..imageLayers.Where(l => l.IsEnable3D)];
                    if (imageLayers.Length > 0)
                    {
                        PreviewManipulation = new RotateAllPreviewManipulationState(imageLayers, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    }
                    break;
                case BeginUseToolEventArgs.PropertyType.RotateX:
                    imageLayers = [.. imageLayers.Where(l => l.IsEnable3D)];
                    if (imageLayers.Length > 0)
                    {
                        PreviewManipulation = new RotateXPreviewManipulationState(imageLayers, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    }
                    break;
                case BeginUseToolEventArgs.PropertyType.RotateY:
                    imageLayers = [.. imageLayers.Where(l => l.IsEnable3D)];
                    if (imageLayers.Length > 0)
                    {
                        PreviewManipulation = new RotateYPreviewManipulationState(imageLayers, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    }
                    break;
                case BeginUseToolEventArgs.PropertyType.Scale when imageLayers.Length > 0 && baseLayerSkeleton != null:
                    PreviewManipulation = new ScalePreviewManipulationState(imageLayers, baseLayerSkeleton, CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
                case BeginUseToolEventArgs.PropertyType.CameraOrbit when activeCameraId != null:
                    PreviewManipulation = new CameraOrbitPreviewManipulationState(Layers.First(l => l.LayerId == activeCameraId), CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
                case BeginUseToolEventArgs.PropertyType.CameraPan when activeCameraId != null:
                    PreviewManipulation = new CameraPanPreviewManipulationState(Layers.First(l => l.LayerId == activeCameraId), CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
                case BeginUseToolEventArgs.PropertyType.CameraDolly when activeCameraId != null:
                    PreviewManipulation = new CameraDollyPreviewManipulationState(Layers.First(l => l.LayerId == activeCameraId), CurrentTime, CompositionModel, cameraSetting, e.StartScreenPosition, HistoryModel);
                    break;
            }

            IsUsingTool = PreviewManipulation != null;
        }

        private void EventHubModel_TextStyleChangeRequest(object? sender, TextStyleChangeEventArgs e)
        {
            if (IsUsingTool || CompositionModel == null || e.CompositionId != CompositionId || SelectedLayers == null)
            {
                return;
            }

            var selectedTextLayerIds = SelectedLayers.Where(l => l.IsText).Select(l => l.LayerId).ToArray();
            CompositionModel.ChangeTextStyle(selectedTextLayerIds, e.TargetLayerId, e.TargetLayerPrevValue);
        }

        private void TimelineViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CompositionModel):
                    if (CompositionModel == null)
                    {
                        Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Timeline_EmptyTitle);
                        Duration = Time.Zero;
                        FrameRate = 30.0;
                        FrameDuration = new Time(1, 30.0);
                    }
                    else
                    {
                        Title = CompositionModel.Name;
                    }
                    SelectedItemType = SelectItemType.None;
                    break;
                case nameof(Name):
                    if (CompositionModel == null)
                    {
                        Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Timeline_EmptyTitle);
                    }
                    else
                    {
                        Title = Name;
                    }
                    break;
                case nameof(Duration):
                case nameof(TimeBarRange):
                    TimelineScrollBarMax = Duration - TimeBarRange;
                    break;
                case nameof(CurrentEditingCompositionId) when CurrentEditingCompositionId == CompositionModel?.CompositionId && SelectedLayers != null:
                    SelectedLayerIdsForPreview = new ObservableCollection<Guid>(SelectedLayers.Select(l => l.LayerId));
                    break;
                case nameof(IsScrubbing) when CompositionModel != null:
                    if (IsScrubbing)
                    {
                        AudioPlayerModel.PlayScrub();
                    }
                    else
                    {
                        AudioPlayerModel.StopScrub();
                    }
                    break;
                case nameof(CurrentTime) when CompositionModel != null && IsScrubbing && Keyboard.IsKeyDown(Key.LeftCtrl):
                    AudioPlayerModel.AddScrubSample(CompositionModel.RenderAudio(CurrentTime, CompositionModel.FrameDuration));
                    break;
            }
        }

        private void TimelineViewModel_PaneSelected(object? sender, EventArgs e)
        {
            CurrentEditingCompositionId = CompositionModel?.CompositionId;
        }

        private void CompositionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompositionModel.IsEnableShy) && (CompositionModel?.IsEnableShy ?? true))
            {
                foreach (var l in (SelectedLayers?.Where(l => l.IsEnableShy)?.ToArray() ?? []))
                {
                    l.DeSelect();
                    SelectedLayers?.Remove(l);
                }
            }
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var layer in e.OldItems?.OfType<LayerViewModel>() ?? [])
            {
                SelectedLayers?.Remove(layer);
            }
            if (Layers?.All(l => l.LayerId != LastSelectedLayerId) ?? false)
            {
                LastSelectedLayerId = null;
            }
        }

        private void SelectedLayers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (Layers != null)
                {
                    foreach (var vm in Layers)
                    {
                        vm.DeSelect();
                    }
                }
                SelectedLayerIdsForPreview?.Clear();
                SelectedItemType = SelectItemType.None;
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (var vm in e.OldItems.OfType<LayerViewModel>())
                    {
                        vm.DeSelect();
                        SelectedLayerIdsForPreview?.Remove(vm.LayerId);
                    }
                    if (CurrentEditingCompositionId == CompositionModel?.CompositionId)
                    {
                        foreach (var vm in e.OldItems.OfType<LayerViewModel>())
                        {
                            if (SelectedLayerIdsForPreview?.Contains(vm.LayerId) ?? false)
                            {
                                SelectedLayerIdsForPreview?.Remove(vm.LayerId);
                            }
                        }
                    }
                }
                if (e.NewItems != null)
                {
                    if (CurrentEditingCompositionId == CompositionModel?.CompositionId)
                    {
                        foreach (var vm in e.NewItems.OfType<LayerViewModel>())
                        {
                            if (!(SelectedLayerIdsForPreview?.Contains(vm.LayerId) ?? true))
                            {
                                SelectedLayerIdsForPreview?.Add(vm.LayerId);
                            }
                        }
                    }
                }
            }
        }

        private void LayerViewModel_LayerSwitchChangeRequest(object? sender, LayerSwitchEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || SelectedLayers == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeLayerSwitches(targetLayers.Select(l => l.LayerId).ToArray(), e.SwitchName, e.Value);
        }

        private void LayerViewModel_BlendModeChangeRequest(object? sender, EnumEventArgs<BlendMode> e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || SelectedLayers ==  null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeBlendModes(targetLayers.Select(l => l.LayerId).ToArray(), e.NewValue);
        }

        private void LayerViewModel_TrackMatteLayerChangeRequest(object? sender, ReferenceLayerChangeEvent e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || SelectedLayers == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeTrackMatteLayers(targetLayers.Select(l => l.LayerId).ToArray(), e.LayerId);
        }

        private void ViewModel_TrackMatteModeChangeRequest(object? sender, EnumEventArgs<TrackMatteMode> e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || SelectedLayers == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeTrackMatteModes(targetLayers.Select(l => l.LayerId).ToArray(), e.NewValue);
        }

        private void ViewModel_ParentLayerChangeRequest(object? sender, ReferenceLayerChangeEvent e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || SelectedLayers == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeParentLayer(targetLayers.Select(l => l.LayerId).ToArray(), e.LayerId);
        }

        private void ViewModel_CheckCycledParentLayerRequest(object? sender, CycledLayerEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            e.Cycled = CompositionModel.CheckCycledParentLayer(layerViewModel.LayerId, e.LayerId);
        }

        private void ViewModel_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            if (e.IsUserAction)
            {
                SelectedItemType = e.SelectItemType;
                SelectedTarget = e.SelectItemType switch
                {
                    SelectItemType.Effect or ViewModel.SelectItemType.Mask or SelectItemType.Property or SelectItemType.KeyFrame => e.CommandableOriginalParent,
                    _ => null,
                };
                if (e.SelectItemType == SelectItemType.Layer && SelectedLayers != null)
                {
                    foreach (var layer in SelectedLayers)
                    {
                        layer.DeSelect();
                    }
                }
            }
            if (e.SelectItemType != SelectItemType.Layer && SelectedLayers != null)
            {
                foreach (var layer in SelectedLayers.Where(v => v != e.Layer).ToArray())
                {
                    layer.DeSelect();
                    SelectedLayers.Remove(layer);
                }
                if (e.Layer != null && !SelectedLayers.Contains(e.Layer))
                {
                    SelectedLayers.Add(e.Layer);
                }
            }
            if (SelectedTarget != null || (e.SelectItemType == SelectItemType.Layer && e.Layer != null && (SelectedLayers?.Contains(e.Layer) ?? false)))
            {
                CurrentEditingCompositionId = CompositionModel?.CompositionId;
                LastSelectedLayerId = e.Layer?.LayerId;
            }
            else
            {
                LastSelectedLayerId = null;
                if ((SelectedLayers?.Count ?? 0) < 1)
                {
                    SelectedItemType = SelectItemType.None;
                }
            }
        }
    }

    enum SelectItemType
    {
        None,
        Layer,
        Effect,
        Mask,
        Property,
        KeyFrame,
    }
}

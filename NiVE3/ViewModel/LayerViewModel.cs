using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GongSolutions.Wpf.DragDrop;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Image.Drawing;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.UI.Dialog;
using NiVE3.Util;
using NiVE3.View.Part;
using NiVE3.View.Primitive;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerViewModel : BindableBase, IDropTarget, IViewModelShortcutCommand, INameEditableViewModel, INameEditableParentViewModel
    {
        private Guid layerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid LayerId
        {
            get { return layerId; }
            set { SetProperty(ref layerId, value); }
        }

        private string name = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private double duration;
        [NeedWire(nameof(LayerModel))]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double sourceStartPoint;
        [NeedWire(nameof(LayerModel))]
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double inPoint;
        [NeedWire(nameof(LayerModel))]
        public double InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private double outPoint;
        [NeedWire(nameof(LayerModel))]
        public double OutPoint
        {
            get { return outPoint; }
            set { SetProperty(ref outPoint, value); }
        }

        private bool isEnableTimeRemap;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableTimeRemap
        {
            get { return isEnableTimeRemap; }
            set { SetProperty(ref isEnableTimeRemap, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private Color tagColor;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Color TagColor
        {
            get { return tagColor; }
            set { SetProperty(ref tagColor, value); }
        }

        private bool isEnableVideo;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableVideo
        {
            get { return isEnableVideo; }
            set { SetProperty(ref isEnableVideo, value); }
        }

        private bool isEnableAudio;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableAudio
        {
            get { return isEnableAudio; }
            set { SetProperty(ref isEnableAudio, value); }
        }

        private bool isEnableSolo;
        [NeedWire(nameof(LayerModel))]
        public bool IsEnableSolo
        {
            get { return isEnableSolo; }
            set { SetProperty(ref isEnableSolo, value); }
        }

        private bool isLock;
        [NeedWire(nameof(LayerModel))]
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private bool isEnableShy;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableCollapse;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableCollapse
        {
            get { return isEnableCollapse; }
            set { SetProperty(ref isEnableCollapse, value); }
        }

        private bool isEnableEffect;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableEffect
        {
            get { return isEnableEffect; }
            set { SetProperty(ref isEnableEffect, value); }
        }

        private bool isEnableFrameBlend;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private bool isEnableAdjustmentLayer;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnableAdjustmentLayer
        {
            get { return isEnableAdjustmentLayer; }
            set { SetProperty(ref isEnableAdjustmentLayer, value); }
        }

        private bool isEnable3D;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private ImageInterpolationQuality interpolationQuality;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public ImageInterpolationQuality InterpolationQuality
        {
            get { return interpolationQuality; }
            set { SetProperty(ref interpolationQuality, value); }
        }

        private bool hasEffect;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool HasEffect
        {
            get { return hasEffect; }
            set { SetProperty(ref hasEffect, value); }
        }

        private BlendMode blendMode;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public BlendMode BlendMode
        {
            get { return blendMode; }
            set { SetProperty(ref blendMode, value); }
        }

        private Guid? trackMatteLayerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid? TrackMatteLayerId
        {
            get { return trackMatteLayerId; }
            set { SetProperty(ref trackMatteLayerId, value); }
        }

        private TrackMatteMode trackMatteMode;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public TrackMatteMode TrackMatteMode
        {
            get { return trackMatteMode; }
            set { SetProperty(ref trackMatteMode, value); }
        }

        private Guid? parentLayerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid? ParentLayerId
        {
            get { return parentLayerId; }
            set { SetProperty(ref parentLayerId, value); }
        }

        private bool isSpecial;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsSpecial
        {
            get { return isSpecial; }
            set { SetProperty(ref isSpecial, value); }
        }

        private bool isCamera;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsCamera
        {
            get { return isCamera; }
            set { SetProperty(ref isCamera, value); }
        }

        private bool isLight;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsLight
        {
            get { return isLight; }
            set { SetProperty(ref isLight, value); }
        }

        private bool isNullObject;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsNullObject
        {
            get { return isNullObject; }
            set { SetProperty(ref isNullObject, value); }
        }

        private bool isText;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsText
        {
            get { return isText; }
            set { SetProperty(ref isText, value); }
        }

        private bool isNotRenderable;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsNotRenderable
        {
            get { return isNotRenderable; }
            set { SetProperty(ref isNotRenderable, value); }
        }

        private double layerNumberColumnWudth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnWidth), IsOneWay = true)]
        public double LayerNumberColumnWidth
        {
            get { return layerNumberColumnWudth; }
            set { SetProperty(ref layerNumberColumnWudth, value); }
        }

        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNameColumnWidth), IsOneWay = true)]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerCommentColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnWidth), IsOneWay = true)]
        public double LayerCommentColumnWidth
        {
            get { return layerCommentColumnWidth; }
            set { SetProperty(ref layerCommentColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnWidth), IsOneWay = true)]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private double modeColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnWidth), IsOneWay = true)]
        public double ModeColumnWidth
        {
            get { return modeColumnWidth; }
            set { SetProperty(ref modeColumnWidth, value); }
        }

        private double trackMatteColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnWidth), IsOneWay = true)]
        public double TrackMatteColumnWidth
        {
            get { return trackMatteColumnWidth; }
            set { SetProperty(ref trackMatteColumnWidth, value); }
        }

        private double parentLayerColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnWidth), IsOneWay = true)]
        public double ParentLayerColumnWidth
        {
            get { return parentLayerColumnWidth; }
            set { SetProperty(ref parentLayerColumnWidth, value); }
        }

        private bool isAVSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineAVSwitchColumnVisible), IsOneWay = true)]
        public bool IsAVSwitchColumnVisible
        {
            get { return isAVSwitchColumnVisible; }
            set { SetProperty(ref isAVSwitchColumnVisible, value); }
        }

        private bool isTagColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTagColumnVisible), IsOneWay = true)]
        public bool IsTagColumnVisible
        {
            get { return isTagColumnVisible; }
            set { SetProperty(ref isTagColumnVisible, value); }
        }

        private bool isLayerNumberColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnVisible), IsOneWay = true)]
        public bool IsLayerNumberColumnVisible
        {
            get { return isLayerNumberColumnVisible; }
            set { SetProperty(ref isLayerNumberColumnVisible, value); }
        }

        private bool isLayerCommentColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnVisible), IsOneWay = true)]
        public bool IsLayerCommentColumnVisible
        {
            get { return isLayerCommentColumnVisible; }
            set { SetProperty(ref isLayerCommentColumnVisible, value); }
        }

        private bool isLayerSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnVisible), IsOneWay = true)]
        public bool IsLayerSwitchColumnVisible
        {
            get { return isLayerSwitchColumnVisible; }
            set { SetProperty(ref isLayerSwitchColumnVisible, value); }
        }

        private bool isModeColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnVisible), IsOneWay = true)]
        public bool IsModeColumnVisible
        {
            get { return isModeColumnVisible; }
            set { SetProperty(ref isModeColumnVisible, value); }
        }

        private bool isTrackMatteColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnVisible), IsOneWay = true)]
        public bool IsTrackMatteColumnVisible
        {
            get { return isTrackMatteColumnVisible; }
            set { SetProperty(ref isTrackMatteColumnVisible, value); }
        }

        private bool isParentLayerColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnVisible), IsOneWay = true)]
        public bool IsParentLayerColumnVisible
        {
            get { return isParentLayerColumnVisible; }
            set { SetProperty(ref isParentLayerColumnVisible, value); }
        }

        private double propertyNameAreaWidth;
        public double PropertyNameAreaWidth
        {
            get { return propertyNameAreaWidth; }
            set { SetProperty(ref propertyNameAreaWidth, value); }
        }

        private LayerModelProxy? trackMatteLayerProxy;
        public LayerModelProxy? TrackMatteLayerProxy
        {
            get { return trackMatteLayerProxy; }
            set { SetProperty(ref trackMatteLayerProxy, value); }
        }

        private LayerModelProxy? parentLayerProxy;
        public LayerModelProxy? ParentLayerProxy
        {
            get { return parentLayerProxy; }
            set { SetProperty(ref parentLayerProxy, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        private WaveFormType audioWaveFormType = WaveFormType.Stereo;
        public WaveFormType AudioWaveFormType
        {
            get { return audioWaveFormType; }
            set { SetProperty(ref audioWaveFormType, value); }
        }

        private EditingLayerParameter editingParameter;
        public EditingLayerParameter EditingParameter
        {
            get { return editingParameter; }
            set { SetProperty(ref editingParameter, value); }
        }

        private ObservableCollectionView<EffectModel, EffectViewModel> effects;
        public ObservableCollectionView<EffectModel, EffectViewModel> Effects
        {
            get { return effects; }
            set { SetProperty(ref effects, value); }
        }

        private ObservableCollection<EffectViewModel> selectedEffects = [];
        public ObservableCollection<EffectViewModel> SelectedEffects
        {
            get { return selectedEffects; }
            set
            {
                selectedEffects.CollectionChanged -= SelectedEffects_CollectionChanged;
                value.CollectionChanged += SelectedEffects_CollectionChanged;
                SetProperty(ref selectedEffects, value);
            }
        }

        private EffectViewModel? lastSelectedEffect;
        public EffectViewModel? LastSelectedEffect
        {
            get { return lastSelectedEffect; }
            set { SetProperty(ref lastSelectedEffect, value); }
        }

        public PropertyGroupViewModel TransformProperties { get; }

        public PropertyGroupViewModel? LayerOptionProperties { get; }

        public PropertyGroupViewModel? TextProperties { get; }

        public PropertyGroupViewModel? ShapeProperties { get; }

        public PropertyGroupViewModel? SourceOptionProperties { get; }

        public PropertyGroupViewModel? AudioOptionProperties { get; }

        public bool IsComposition { get; }

        public IEnumerable<LayerModelProxy> TrackMatteViewSource { get; }

        public IEnumerable<LayerModelProxy> ParentLayerViewSource { get; }

        public bool IsNameEditing => EditingParameter == EditingLayerParameter.Name;

        public bool HasImage => SourceType.HasFlag(SourceType.Image) || SourceType.HasFlag(SourceType.Video);

        public bool HasAudio => SourceType.HasFlag(SourceType.Audio);

        public bool HasDuration => HasAudio || SourceType.HasFlag(SourceType.Video);

        public INameEditableViewModel? TargetChild => SelectedEffects.FirstOrDefault();

        public ICommand BeginEditDurationRequestCommand { get; }

        public ICommand UpdateDurationRequestCommand { get; }

        public ICommand AbortEditDurationRequestCommand { get; }

        public ICommand BeginEditDurationCommand { get; }

        public ICommand CommitEditDurationCommand { get; }

        public ICommand AbortEditDurationCommand { get; }

        public ICommand ChangeLayerSwitchCommand { get; }

        public ICommand ChangeInterpolationQualityCommand { get; }

        public ICommand ChangeBlendModeCommand { get; }

        public ICommand ChangeTrackMatteCommand { get; }

        public ICommand ChangeTrackMatteModeCommand { get; }

        public ICommand ChangeParentLayerCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        public ICommand SelectItemCommand { get; }

        public ICommand ChangeTagColorCommand { get; }

        public ICommand CutEffectCommand { get; }

        public ICommand CopyEffectCommand { get; }

        public ICommand PasteEffectCommand { get; }

        public ICommand DuplicateEffectCommand { get; }

        public ICommand DeleteEffectCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        WeakEventPublisher<LayerSwitchEventArgs> LayerSwitchChangeRequestPublisher { get; } = new WeakEventPublisher<LayerSwitchEventArgs>();
        public event EventHandler<LayerSwitchEventArgs> LayerSwitchChangeRequest
        {
            add { LayerSwitchChangeRequestPublisher.Subscribe(value); }
            remove { LayerSwitchChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EnumEventArgs<BlendMode>> BlendModeChangeRequestPublisher { get; } = new WeakEventPublisher<EnumEventArgs<BlendMode>>();
        public event EventHandler<EnumEventArgs<BlendMode>> BlendModeChangeRequest
        {
            add { BlendModeChangeRequestPublisher.Subscribe(value); }
            remove { BlendModeChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<ReferenceLayerChangeEvent> TrackMatteLayerChangeRequestPublisher { get; } = new WeakEventPublisher<ReferenceLayerChangeEvent>();
        public event EventHandler<ReferenceLayerChangeEvent> TrackMatteLayerChangeRequest
        {
            add { TrackMatteLayerChangeRequestPublisher.Subscribe(value); }
            remove { TrackMatteLayerChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EnumEventArgs<TrackMatteMode>> TrackMatteModeChangeRequestPublisher { get; } = new WeakEventPublisher<EnumEventArgs<TrackMatteMode>>();
        public event EventHandler<EnumEventArgs<TrackMatteMode>> TrackMatteModeChangeRequest
        {
            add { TrackMatteModeChangeRequestPublisher.Subscribe(value); }
            remove { TrackMatteModeChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<ReferenceLayerChangeEvent> ParentLayerChangeRequestPublisher { get; } = new WeakEventPublisher<ReferenceLayerChangeEvent>();
        public event EventHandler<ReferenceLayerChangeEvent> ParentLayerChangeRequest
        {
            add { ParentLayerChangeRequestPublisher.Subscribe(value); }
            remove { ParentLayerChangeRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<CycledLayerEventArgs> CheckCycledParentLayerRequestPublisher { get; } = new WeakEventPublisher<CycledLayerEventArgs>();
        public event EventHandler<CycledLayerEventArgs> CheckCycledParentLayerRequest
        {
            add { CheckCycledParentLayerRequestPublisher.Subscribe(value); }
            remove { CheckCycledParentLayerRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<SelectItemEventArgs> SelectItemChangedPublisher { get; } = new WeakEventPublisher<SelectItemEventArgs>();
        public event EventHandler<SelectItemEventArgs> SelectItemChanged
        {
            add { SelectItemChangedPublisher.Subscribe(value); }
            remove { SelectItemChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<PropertyValueCommitedEventArgs> PropertyValueCommitedPublisher { get; } = new WeakEventPublisher<PropertyValueCommitedEventArgs>();
        public event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited
        {
            add { PropertyValueCommitedPublisher.Subscribe(value); }
            remove { PropertyValueCommitedPublisher.Unsubscribe(value); }
        }

        LayerModel LayerModel { get; }

        ViewStateModel ViewState { get; }

        EventHubModel EventHubModel { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        double PrevInPoint { get; set; }

        double PrevOutPoint { get; set; }

        double PrevSourceStartPoint { get; set; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public LayerViewModel(LayerModel layerModel, ViewStateModel viewState, EventHubModel eventHubModel, IEnumerable<LayerModelProxy> trackMatteViewSource, IEnumerable<LayerModelProxy> parentLayerViewSource)
#pragma warning restore CS8618
        {
            LayerModel = layerModel;
            ViewState = viewState;
            EventHubModel = eventHubModel;
            TrackMatteViewSource = trackMatteViewSource;
            ParentLayerViewSource = parentLayerViewSource;
            SelectedEffects = [];

            Effects = layerModel.Effects.CreateViewCollection(e =>
            {
                var vm = new EffectViewModel(e);
                vm.EffectEnableChangeRequest += Effect_EffectEnableChangeRequest;
                vm.SelectItemChanged += Effect_SelectItemChanged;
                vm.PropertyValueCommited += Effect_PropertyValueCommited;
                return vm;
            });

            if (layerModel.TransformProperties != null)
            {
                TransformProperties = new PropertyGroupViewModel(layerModel.TransformProperties);
                TransformProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                TransformProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            if (layerModel.LayerOptionProperties != null)
            {
                LayerOptionProperties = new PropertyGroupViewModel(layerModel.LayerOptionProperties);
                LayerOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                LayerOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            if (layerModel.TextProperties != null)
            {
                TextProperties = new PropertyGroupViewModel(layerModel.TextProperties);
                TextProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                TextProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            if (layerModel.ShapeProperties != null)
            {
                ShapeProperties = new PropertyGroupViewModel(layerModel.ShapeProperties);
                ShapeProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                ShapeProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            if (layerModel.SourceOptionProperties != null)
            {
                SourceOptionProperties = new PropertyGroupViewModel(layerModel.SourceOptionProperties);
                SourceOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                SourceOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            if (layerModel.AudioOptionProperties != null)
            {
                AudioOptionProperties = new PropertyGroupViewModel(layerModel.AudioOptionProperties);
                AudioOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                AudioOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }

            WiringModel();
            RefreshLayerProxies();

            PropertyNameAreaWidth = (IsLayerNumberColumnVisible ? LayerNumberColumnWidth : 0.0) + LayerNameColumnWidth;
            IsComposition = layerModel.IsComposition;

            BeginEditDurationRequestCommand = new DelegateCommand<BeginEditDurationEventArgs.DurationType?>(type =>
            {
                EventHubModel.NotifyBeginEditDuration(LayerModel.ParentCompositionId, LayerId, type ?? BeginEditDurationEventArgs.DurationType.None);
            }, _ => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            UpdateDurationRequestCommand = new DelegateCommand<Tuple<double, double, double, bool>>(t =>
            {
                EventHubModel.NotifyUpdateDuration(LayerModel.ParentCompositionId, t.Item1, t.Item2, t.Item3, t.Item4);
            }, _ => EditingParameter == EditingLayerParameter.Duration).ObservesProperty(() => EditingParameter);

            AbortEditDurationRequestCommand = new DelegateCommand(() =>
            {
                eventHubModel.NotifyAbortEditDuration(LayerModel.ParentCompositionId);
            }, () => EditingParameter == EditingLayerParameter.Duration).ObservesProperty(() => EditingParameter);

            BeginEditDurationCommand = new DelegateCommand(() =>
            {
                PrevInPoint = InPoint;
                PrevOutPoint = OutPoint;
                PrevSourceStartPoint = SourceStartPoint;
                EditingParameter = EditingLayerParameter.Duration;
            }, () => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            CommitEditDurationCommand = new DelegateCommand(() =>
            {
                if (InPoint != PrevInPoint || OutPoint != PrevOutPoint || SourceStartPoint != PrevSourceStartPoint)
                {
                    LayerModel.CommitEditDuration(PrevInPoint, InPoint, PrevOutPoint, OutPoint, PrevSourceStartPoint, SourceStartPoint);
                }
                EditingParameter = EditingLayerParameter.None;
            }, () => EditingParameter == EditingLayerParameter.Duration).ObservesProperty(() => EditingParameter);

            AbortEditDurationCommand = new DelegateCommand(() =>
            {
                InPoint = PrevInPoint;
                OutPoint = PrevOutPoint;
                SourceStartPoint = PrevSourceStartPoint;
                EditingParameter = EditingLayerParameter.None;
            }, () => EditingParameter == EditingLayerParameter.Duration).ObservesProperty(() => EditingParameter);

            ChangeLayerSwitchCommand = new DelegateCommand<string>(name =>
            {
                var newValue = !(name switch
                {
                    nameof(IsEnableVideo) => IsEnableVideo,
                    nameof(IsEnableAudio) => IsEnableAudio,
                    nameof(IsEnableSolo) => IsEnableSolo,
                    nameof(IsLock) => IsLock,
                    nameof(IsEnableShy) => IsEnableShy,
                    nameof(IsEnableCollapse) => IsEnableCollapse,
                    nameof(IsEnableEffect) => IsEnableEffect,
                    nameof(IsEnableFrameBlend) => IsEnableFrameBlend,
                    nameof(IsEnableMotionBlur) => IsEnableMotionBlur,
                    nameof(IsEnableAdjustmentLayer) => IsEnableAdjustmentLayer,
                    nameof(IsEnable3D) => IsEnable3D,
                    _ => false
                });
                LayerSwitchChangeRequestPublisher.Publish(this, new LayerSwitchEventArgs(name, newValue));
            });

            ChangeInterpolationQualityCommand = new DelegateCommand(() =>
            {
                var values = Enum.GetValues<ImageInterpolationQuality>();
                var newValue = values[(Array.IndexOf(values, InterpolationQuality) + 1) % values.Length];
                LayerSwitchChangeRequestPublisher.Publish(this, new LayerSwitchEventArgs(nameof(InterpolationQuality), newValue));
            });

            ChangeBlendModeCommand = new DelegateCommand<BlendMode?>(blendMode =>
            {
                if (!blendMode.HasValue)
                {
                    return;
                }
                BlendModeChangeRequestPublisher.Publish(this, new EnumEventArgs<BlendMode>(blendMode.Value));
            });

            ChangeTrackMatteCommand = new DelegateCommand<LayerModelProxy?>(trackMatteLayer =>
            {
                TrackMatteLayerChangeRequestPublisher.Publish(this, new ReferenceLayerChangeEvent(trackMatteLayer?.LayerId));
            });

            ChangeTrackMatteModeCommand = new DelegateCommand<TrackMatteMode?>(trackMatteMode =>
            {
                if (!trackMatteMode.HasValue)
                {
                    return;
                }
                TrackMatteModeChangeRequestPublisher.Publish(this, new EnumEventArgs<TrackMatteMode>(trackMatteMode.Value));
            });

            ChangeParentLayerCommand = new DelegateCommand<LayerModelProxy?>(parentLayer =>
            {
                ParentLayerChangeRequestPublisher.Publish(this, new ReferenceLayerChangeEvent(parentLayer?.LayerId));
            });

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                PrevName = Name;
                EditingParameter = EditingLayerParameter.Name;
            }, () => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            BeginEditCommentCommand = new DelegateCommand(() =>
            {
                PrevComment = Comment;
                EditingParameter = EditingLayerParameter.Comment;
            }, () => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            EndEditNameCommand = new DelegateCommand<bool?>(commit =>
            {
                if ((commit ?? false) && !string.IsNullOrEmpty(Name))
                {
                    LayerModel.ChangeName(Name);
                }
                else
                {
                    Name = PrevName;
                }
                EditingParameter = EditingLayerParameter.None;
            }, _ => EditingParameter == EditingLayerParameter.Name).ObservesProperty(() => EditingParameter);

            EndEditCommentCommand = new DelegateCommand<bool?>(commit =>
            {
                if (commit ?? false)
                {
                    LayerModel.ChangeComment(Comment);
                }
                else
                {
                    Comment = PrevComment;
                }
                EditingParameter = EditingLayerParameter.None;
            }, _ => EditingParameter == EditingLayerParameter.Comment).ObservesProperty(() => EditingParameter);

            SelectItemCommand = new DelegateCommand(() => SelectItemChangedPublisher.Publish(this, new SelectItemEventArgs(SelectItemType.Layer, true, this)));

            CutEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                {
                    var copyData = LayerModel.CutEffects([.. SelectedEffects.Select(e => e.EffectId)]);
                    ClipboardUtil.SetData(copyData);
                }
                SelectedEffects.Clear();
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            CopyEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                {
                    ClipboardUtil.SetData(LayerModel.CopyEffects([.. SelectedEffects.Select(e => e.EffectId)]));
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            PasteEffectCommand = new RequerySuggestedCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                var data = ClipboardUtil.GetData<EffectData>();
                if (data != null)
                {
                    var insertTargetId = LastSelectedEffect?.EffectId;
                    LayerModel.PasteEffects(data, [.. SelectedEffects.Select(e => e.EffectId)], insertTargetId);
                }
            }, () => EditingParameter == EditingLayerParameter.None && ClipboardUtil.GetData<EffectData>()?.Type == CopyDataType.Effect);

            DuplicateEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None && SelectedEffects.Count < 1)
                {
                    return;
                }

                var insertTargetId = LastSelectedEffect?.EffectId;
                LayerModel.DuplicateEffects([.. SelectedEffects.Select(e => e.EffectId)], insertTargetId);
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            DeleteEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                {
                    LayerModel.DeleteEffect([.. SelectedEffects.Select(e => e.EffectId)]);
                }
                SelectedEffects.Clear();
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            DeleteCommand = new DelegateCommand<SelectItemType?>(type => DeleteEffectCommand.Execute(null));

            CutCommand = new DelegateCommand<SelectItemType?>(type => CutEffectCommand.Execute(null));

            CopyCommand = new DelegateCommand<SelectItemType?>(type => CopyEffectCommand.Execute(null));

            PasteCommand = new DelegateCommand<SelectItemType?>(type => PasteEffectCommand.Execute(null));

            DuplicateCommand = new DelegateCommand<SelectItemType?>(type => DuplicateEffectCommand.Execute(null));

            SelectAllCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                DeSelect();
                foreach (var e in Effects)
                {
                    SelectedEffects.Add(e);
                }
            });

            ChangeTagColorCommand = new DelegateCommand(() =>
            {
                var colorDialog = new ColorPickerDialog(TagColor);
                if (colorDialog.ShowDialog() ?? false)
                {
                    LayerModel.ChangeTagColor(colorDialog.Color);
                }
            });

            PropertyChanged += LayerViewModel_PropertyChanged;
        }

        public bool CheckParentLayerCycled(Guid? layerId)
        {
            if (!layerId.HasValue)
            {
                return false;
            }

            var eventArgs = new CycledLayerEventArgs(layerId.Value);
            CheckCycledParentLayerRequestPublisher.Publish(this, eventArgs);
            return eventArgs.Cycled;
        }

        public float[] GetAudio(double time, double length)
        {
            return LayerModel.GetAudio(time, length);
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (IsSpecial)
            {
                dropInfo.NotHandled = true;
                return;
            }

            switch (dropInfo.Data)
            {
                case EffectListDragData effectListData when dropInfo.VisualTarget is EffectCollectionView:
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    break;
                case EffectListDragData effectListData:
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    break;
                case EffectViewModel effect when Effects.Contains(effect):
                case ItemDragData<EffectViewModel> itemDragData when itemDragData.SelectedItems.All(Effects.Contains):
                    dropInfo.Effects = DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    break;
                default:
                    dropInfo.NotHandled = true;
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (IsSpecial)
            {
                return;
            }

            switch (dropInfo.Data)
            {
                case EffectListDragData effectListData when dropInfo.VisualTarget is EffectCollectionView:
                    LayerModel.InsertEffect(effectListData.Effects, dropInfo.InsertIndex);
                    break;
                case EffectListDragData effectListData:
                    EventHubModel.NotifyAddEffectToSelectedLayers(LayerModel.ParentCompositionId, LayerId, effectListData.Effects);
                    break;
                case EffectViewModel effect:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Effects.IndexOf(effect) < newIndex)
                        {
                            newIndex--;
                        }
                        LayerModel.MoveEffect(effect.EffectId, newIndex);
                    }
                    break;
                case ItemDragData<EffectViewModel> itemDragData:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Effects.IndexOf(itemDragData.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        LayerModel.MoveEffects(itemDragData.SelectedItems.Select(l => l.EffectId).ToArray(), itemDragData.DragItem.EffectId, newIndex);
                    }
                    break;
                default:
                    dropInfo.NotHandled = true;
                    break;
            }
        }

        public void DeSelect()
        {
            foreach (var e in SelectedEffects)
            {
                e.DeSelect();
            }
            SelectedEffects.Clear();
            TransformProperties?.DeSelect();
            LayerOptionProperties?.DeSelect();
            TextProperties?.DeSelect();
            ShapeProperties?.DeSelect();
            SourceOptionProperties?.DeSelect();
            AudioOptionProperties?.DeSelect();
        }

        partial void WiringModel();

        void RefreshLayerProxies()
        {
            TrackMatteLayerProxy = TrackMatteViewSource.FirstOrDefault(l => l.LayerId == TrackMatteLayerId);
            ParentLayerProxy = ParentLayerViewSource.FirstOrDefault(l => l.LayerId == ParentLayerId);
        }

        private void LayerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TrackMatteLayerId):
                    TrackMatteLayerProxy = TrackMatteViewSource.FirstOrDefault(l => l.LayerId == TrackMatteLayerId);
                    break;
                case nameof(ParentLayerId):
                    ParentLayerProxy = ParentLayerViewSource.FirstOrDefault(l => l.LayerId == ParentLayerId);
                    break;
                case nameof(LayerNumberColumnWidth):
                case nameof(LayerNameColumnWidth):
                case nameof(IsLayerNumberColumnVisible):
                    PropertyNameAreaWidth = (IsLayerNumberColumnVisible ? LayerNumberColumnWidth : 0.0) + LayerNameColumnWidth;
                    break;
            }
        }

        private void Effect_EffectEnableChangeRequest(object? sender, EffectEnableChangeEventArgs e)
        {
            if (sender is not EffectViewModel effect)
            {
                return;
            }

            var targetEffects = new List<EffectViewModel>();
            if (SelectedEffects.Count < 1 || !SelectedEffects.Contains(effect))
            {
                targetEffects.Add(effect);
            }
            else
            {
                targetEffects.AddRange(SelectedEffects);
            }

            LayerModel.ChangeEffectEnable(targetEffects.Select(e => e.EffectId).ToArray(), e.IsEnabled);
        }

        private void Effect_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            if (e.SelectItemType != SelectItemType.Effect)
            {
                foreach (var effect in SelectedEffects.Where(v => v != e.Effect).ToArray())
                {
                    SelectedEffects.Remove(effect);
                }
                if (e.Effect != null && !SelectedEffects.Contains(e.Effect))
                {
                    SelectedEffects.Add(e.Effect);

                    TransformProperties?.DeSelect();
                    LayerOptionProperties?.DeSelect();
                    TextProperties?.DeSelect();
                    ShapeProperties?.DeSelect();
                    SourceOptionProperties?.DeSelect();
                    AudioOptionProperties?.DeSelect();
                }
            }
            else
            {
                TransformProperties?.DeSelect();
                LayerOptionProperties?.DeSelect();
                TextProperties?.DeSelect();
                ShapeProperties?.DeSelect();
                SourceOptionProperties?.DeSelect();
                AudioOptionProperties?.DeSelect();
            }
        }

        private void SelectedEffects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var effect in Effects)
                {
                    effect.DeSelect();
                }
            }
            else
            {
                foreach (var effect in e.OldItems?.OfType<EffectViewModel>() ?? [])
                {
                    effect.DeSelect();
                }
            }
        }

        private void Effect_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }

        private void PropertyGroupViewModel_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            foreach (var effect in SelectedEffects)
            {
                effect.DeSelect();
            }
            SelectedEffects.Clear();
        }

        private void PropertyGroupViewModel_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerModelProxy : BindableBase
    {
        private Guid layerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid LayerId
        {
            get { return layerId; }
            set { SetProperty(ref layerId, value); }
        }

        private string name = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        LayerModel LayerModel { get; }

        public LayerModelProxy(LayerModel layerModel)
        {
            LayerModel = layerModel;

            WiringModel();
        }

        partial void WiringModel();
    }

    enum EditingLayerParameter
    {
        None,
        Name,
        Comment,
        Duration
    }
}

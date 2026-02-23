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
using Microsoft.Win32;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Image.Drawing;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.UI.Command;
using NiVE3.UI.Dialog;
using NiVE3.Util;
using NiVE3.View.Part;
using NiVE3.View.Primitive;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerViewModel : BindableBase, IDropTarget, IViewModelShortcutCommand, INameEditableViewModel, INameEditableParentViewModel
    {
        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Guid LayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial string Comment { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Time SourceDuration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial Time SourceStartPoint { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial Time InPoint { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial Time OutPoint { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial bool IsEnableTimeRemap { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsFreezeFrame { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Time FreezeFrameTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial SourceType SourceType { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Color TagColor { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial double PlayRate { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial bool IsEnableVideo { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial bool IsEnableAudio { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial bool IsEnableSolo { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel))]
        public partial bool IsLock { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableShy { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableExplode { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableEffect { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableFrameBlend { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableMotionBlur { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnableAdjustmentLayer { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnable3D { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial ImageInterpolationQuality InterpolationQuality { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool HasEffect { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool HasNonDummyEffect { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool HasMask { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial BlendMode BlendMode { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Guid? TrackMatteLayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial TrackMatteMode TrackMatteMode { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Guid? ParentLayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsSpecial { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsCamera { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsLight { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsNullObject { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsText { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsNotRenderable { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsDisableDuration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnWidth), IsOneWay = true)]
        public partial double LayerNumberColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNameColumnWidth), IsOneWay = true)]
        public partial double LayerNameColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnWidth), IsOneWay = true)]
        public partial double LayerCommentColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnWidth), IsOneWay = true)]
        public partial double LayerSwitchColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnWidth), IsOneWay = true)]
        public partial double ModeColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnWidth), IsOneWay = true)]
        public partial double TrackMatteColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnWidth), IsOneWay = true)]
        public partial double ParentLayerColumnWidth { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineAVSwitchColumnVisible), IsOneWay = true)]
        public partial bool IsAVSwitchColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTagColumnVisible), IsOneWay = true)]
        public partial bool IsTagColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnVisible), IsOneWay = true)]
        public partial bool IsLayerNumberColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnVisible), IsOneWay = true)]
        public partial bool IsLayerCommentColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnVisible), IsOneWay = true)]
        public partial bool IsLayerSwitchColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnVisible), IsOneWay = true)]
        public partial bool IsModeColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTrackMatteColumnVisible), IsOneWay = true)]
        public partial bool IsTrackMatteColumnVisible { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnVisible), IsOneWay = true)]
        public partial bool IsParentLayerColumnVisible { get; set; }

        [ReactiveProperty]
        public partial double PropertyNameAreaWidth { get; set; }

        [ReactiveProperty]
        public partial LayerModelProxy? TrackMatteLayerProxy { get; set; }

        [ReactiveProperty]
        public partial LayerModelProxy? ParentLayerProxy { get; set; }

        [ReactiveProperty]
        public partial bool IsExpanded { get; set; }

        [ReactiveProperty]
        public partial WaveFormType AudioWaveFormType { get; set; } = WaveFormType.Stereo;

        [ReactiveProperty]
        public partial EditingLayerParameter EditingParameter { get; set; }

        [ReactiveProperty]
        public partial ObservableCollectionView<EffectModel, EffectViewModel> Effects { get; set; }

        [ReactiveProperty]
        public partial ObservableCollectionView<MaskModel, MaskViewModel> Masks { get; set; }

        [ReactiveProperty]
        public partial bool HasAudioLevelValueKeyFrames { get; set; }

        public ObservableCollection<EffectViewModel> SelectedEffects
        {
            get;
            set
            {
                if (field != value)
                {
                    field.CollectionChanged -= SelectedEffects_CollectionChanged;
                    value.CollectionChanged += SelectedEffects_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        } = [];

        public ObservableCollection<MaskViewModel> SelectedMasks
        {
            get;
            set
            {
                if (field != value)
                {
                    field.CollectionChanged -= SelectedMasks_CollectionChanged;
                    value.CollectionChanged += SelectedMasks_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        } = [];

        [ReactiveProperty]
        public partial EffectViewModel? LastSelectedEffect { get; set; }

        [ReactiveProperty]
        public partial MaskViewModel? LastSelectedMask { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? TransformProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? LayerOptionProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? TextProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? ShapeProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? SourceOptionProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? AudioOptionProperties { get; set; }

        [ReactiveProperty]
        public partial PropertyGroupViewModel? AudioLevelValueProperties { get; set; }

        public bool IsComposition { get; }

        public IEnumerable<LayerModelProxy> TrackMatteViewSource { get; }

        public IEnumerable<LayerModelProxy> ParentLayerViewSource { get; }

        public bool IsNameEditing => EditingParameter == EditingLayerParameter.Name;

        public bool HasImage => LayerModel.HasImage;

        public bool HasAudio => LayerModel.HasAudio;

        public bool IsVideo => LayerModel.IsVideo;

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

        public ICommand SaveEffectPresetCommand { get; }

        public ICommand LoadEffectPresetCommand { get; }

        public ICommand SaveMaskPresetCommand { get; }

        public ICommand LoadMaskPresetCommand { get; }

        public ICommand CutMaskCommand { get; }

        public ICommand CopyMaskCommand { get; }

        public ICommand PasteMaskCommand { get; }

        public ICommand DuplicateMaskCommand { get; }

        public ICommand DeleteMaskCommand { get; }

        public ICommand ShowFootagePreviewCommand { get; }

        public DelegateCommand<SelectItemType?> DeleteCommand { get; }

        public DelegateCommand<SelectItemType?> CutCommand { get; }

        public DelegateCommand<SelectItemType?> CopyCommand { get; }

        public DelegateCommand<SelectItemType?> PasteCommand { get; }

        public DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        public DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        public DelegateCommand<SelectItemType?> SavePresetCommand { get; }

        public DelegateCommand<SelectItemType?> LoadPresetCommand { get; }

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

        WeakEventPublisher<ReferenceLayerChangeEventArgs> TrackMatteLayerChangeRequestPublisher { get; } = new WeakEventPublisher<ReferenceLayerChangeEventArgs>();
        public event EventHandler<ReferenceLayerChangeEventArgs> TrackMatteLayerChangeRequest
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

        WeakEventPublisher<ParentLayerChangeEventArgs> ParentLayerChangeRequestPublisher { get; } = new WeakEventPublisher<ParentLayerChangeEventArgs>();
        public event EventHandler<ParentLayerChangeEventArgs> ParentLayerChangeRequest
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

        WeakEventPublisher<EventArgs> FocusRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> FocusRequest
        {
            add { FocusRequestPublisher.Subscribe(value); }
            remove { FocusRequestPublisher.Unsubscribe(value); }
        }

        LayerModel LayerModel { get; }

        ViewStateModel ViewState { get; }

        EventHubModel EventHubModel { get; }

        string PrevName { get; set; } = "";

        string PrevComment { get; set; } = "";

        Time PrevInPoint { get; set; }

        Time PrevOutPoint { get; set; }

        Time PrevSourceStartPoint { get; set; }

        public LayerViewModel(LayerModel layerModel, ViewStateModel viewState, EventHubModel eventHubModel, IEnumerable<LayerModelProxy> trackMatteViewSource, IEnumerable<LayerModelProxy> parentLayerViewSource)
        {
            LayerModel = layerModel;
            ViewState = viewState;
            EventHubModel = eventHubModel;
            TrackMatteViewSource = trackMatteViewSource;
            ParentLayerViewSource = parentLayerViewSource;
            SelectedEffects = [];
            SelectedMasks = [];

            Effects = layerModel.Effects.CreateViewCollection(e =>
            {
                var vm = new EffectViewModel(e, viewState);
                vm.EffectEnableChangeRequest += Effect_EffectEnableChangeRequest;
                vm.SelectItemChanged += Effect_SelectItemChanged;
                vm.PropertyValueCommited += Effect_PropertyValueCommited;
                return vm;
            });

            Masks = layerModel.Masks.CreateViewCollection(m =>
            {
                var vm = new MaskViewModel(m, viewState);
                vm.MaskEnableChangeRequest += Mask_MaskEnableChangeRequest;
                vm.SelectItemChanged += Mask_SelectItemChanged;
                vm.PropertyValueCommited += Mask_PropertyValueCommited;
                return vm;
            });

            UpdatePropertyGroupViewModels();

            WiringModel();
            RefreshLayerProxies();

            PropertyNameAreaWidth = (IsLayerNumberColumnVisible ? LayerNumberColumnWidth : 0.0) + LayerNameColumnWidth;
            IsComposition = layerModel.IsComposition;

            BeginEditDurationRequestCommand = new DelegateCommand<BeginEditDurationEventArgs.DurationType?>(type =>
            {
                EventHubModel.NotifyBeginEditDuration(LayerModel.ParentCompositionId, LayerId, type ?? BeginEditDurationEventArgs.DurationType.None);
            }, _ => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            UpdateDurationRequestCommand = new DelegateCommand<Tuple<Time, Time, Time, bool>>(t =>
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
                    nameof(IsEnableExplode) => IsEnableExplode,
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
                TrackMatteLayerChangeRequestPublisher.Publish(this, new ReferenceLayerChangeEventArgs(trackMatteLayer?.LayerId));
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
                var resetTransform = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                var skipKeepTransform = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                ParentLayerChangeRequestPublisher.Publish(this, new ParentLayerChangeEventArgs(parentLayer?.LayerId, resetTransform, skipKeepTransform));
            });

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                PrevName = Name;
                EditingParameter = EditingLayerParameter.Name;
            }, () => !IsLock && EditingParameter == EditingLayerParameter.None)
                .ObservesProperty(() => IsLock)
                .ObservesProperty(() => EditingParameter);

            BeginEditCommentCommand = new DelegateCommand(() =>
            {
                PrevComment = Comment;
                EditingParameter = EditingLayerParameter.Comment;
            }, () => !IsLock && EditingParameter == EditingLayerParameter.None)
                .ObservesProperty(() => IsLock)
                .ObservesProperty(() => EditingParameter);

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
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedEffects.Count > 0)
                {
                    var copyData = LayerModel.CutEffects([..SelectedEffects.Select(e => e.EffectId)]);
                    ClipboardUtil.SetData(copyData);
                    SelectedEffects.Clear();
                    FocusRequestPublisher.Publish(this, EventArgs.Empty);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            CopyEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedEffects.Count > 0)
                {
                    ClipboardUtil.SetData(LayerModel.CopyEffects([..SelectedEffects.Select(e => e.EffectId)]));
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

                if (ClipboardUtil.GetData<EffectData>(CopyDataType.Effect) is CopyData<EffectData> effectData)
                {
                    var insertTargetId = LastSelectedEffect?.EffectId;
                    LayerModel.PasteEffects(effectData, [..SelectedEffects.Select(e => e.EffectId)], insertTargetId);
                }
            }, () => EditingParameter == EditingLayerParameter.None && ClipboardUtil.GetDataType() == CopyDataType.Effect);

            DuplicateEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedEffects.Count > 0)
                {
                    var insertTargetId = LastSelectedEffect?.EffectId;
                    LayerModel.DuplicateEffects([..SelectedEffects.Select(e => e.EffectId)], insertTargetId);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            DeleteEffectCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedEffects.Count > 0)
                {
                    LayerModel.DeleteEffect([..SelectedEffects.Select(e => e.EffectId)]);
                    SelectedEffects.Clear();
                    FocusRequestPublisher.Publish(this, EventArgs.Empty);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            SaveEffectPresetCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None || SelectedEffects.Count < 1)
                {
                    return;
                }

                var save = new SaveFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveEffectPreset_Filter_EffectPreset)}({Const.PropertyEffectExtensionFilter})|{Const.PropertyEffectExtensionFilter}"
                };
                if (!(save.ShowDialog() ?? false))
                {
                    return;
                }

                try
                {
                    LayerModel.SaveEffectPreset(save.FileName, [..SelectedEffects.Select(e => e.EffectId)]);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedEffects.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedEffects.Count);

            LoadEffectPresetCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                var open = new OpenFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveEffectPreset_Filter_EffectPreset)}(({Const.PropertyEffectExtensionFilter})|{Const.PropertyEffectExtensionFilter}"
                };
                if (!(open.ShowDialog() ?? false))
                {
                    return;
                }

                var insertTargetId = LastSelectedEffect?.EffectId;
                try
                {
                    LayerModel.LoadEffectPreset(open.FileName, [..SelectedEffects.Select(e => e.EffectId)], insertTargetId);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => EditingParameter == EditingLayerParameter.None).ObservesProperty(() => EditingParameter);

            CutMaskCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedMasks.Count > 0)
                {
                    var copyData = LayerModel.CutMasks([.. SelectedMasks.Select(m => m.MaskId)]);
                    ClipboardUtil.SetData(copyData);
                    SelectedMasks.Clear();
                    FocusRequestPublisher.Publish(this, EventArgs.Empty);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedMasks.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedMasks.Count);

            CopyMaskCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedMasks.Count > 0)
                {
                    ClipboardUtil.SetData(LayerModel.CopyMasks([.. SelectedMasks.Select(m => m.MaskId)]));
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedMasks.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedMasks.Count);

            PasteMaskCommand = new RequerySuggestedCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (ClipboardUtil.GetData<MaskData>(CopyDataType.Mask) is CopyData<MaskData> maskData)
                {
                    var insertTargetId = LastSelectedMask?.MaskId;
                    LayerModel.PasteMasks(maskData, [.. SelectedMasks.Select(m => m.MaskId)], insertTargetId);
                }
            }, () => EditingParameter == EditingLayerParameter.None && ClipboardUtil.GetDataType() ==  CopyDataType.Mask);

            DuplicateMaskCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedMasks.Count > 0)
                {
                    var insertTargetId = LastSelectedMask?.MaskId;
                    LayerModel.DuplicateMasks([.. SelectedMasks.Select(m => m.MaskId)], insertTargetId);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedMasks.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedMasks.Count);

            DeleteMaskCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                if (SelectedMasks.Count > 0)
                {
                    LayerModel.DeleteMask([.. SelectedMasks.Select(m => m.MaskId)]);
                    SelectedMasks.Clear();
                    FocusRequestPublisher.Publish(this, EventArgs.Empty);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedMasks.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedMasks.Count);

            SaveMaskPresetCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None || SelectedMasks.Count < 1)
                {
                    return;
                }

                var save = new SaveFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveMaskPreset_Filter_MaskPreset)}({Const.PropertyMaskExtensionFilter})|{Const.PropertyMaskExtensionFilter}"
                };
                if (!(save.ShowDialog() ?? false))
                {
                    return;
                }

                try
                {
                    LayerModel.SaveMaskPreset(save.FileName, [..SelectedMasks.Select(m => m.MaskId)]);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_SavePresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, () => EditingParameter == EditingLayerParameter.None && SelectedMasks.Count > 0)
                .ObservesProperty(() => EditingParameter)
                .ObservesProperty(() => SelectedMasks.Count);

            LoadMaskPresetCommand = new DelegateCommand(() =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                var open = new OpenFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSaveMaskPreset_Filter_MaskPreset)}(({Const.PropertyMaskExtensionFilter})|{Const.PropertyMaskExtensionFilter}"
                };
                if (!(open.ShowDialog() ?? false))
                {
                    return;
                }

                var insertTargetId = LastSelectedMask?.MaskId;
                try
                {
                    LayerModel.LoadMaskPreset(open.FileName, [..SelectedMasks.Select(m => m.MaskId)], insertTargetId);
                }
                catch
                {
                    var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Title);
                    var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadPresetError_Text);
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            ShowFootagePreviewCommand = new DelegateCommand(() => EventHubModel.NotifyShowFootagePreview(LayerModel.FootageId));

            DeleteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    DeleteMaskCommand.Execute(null);
                }
                else
                {
                    DeleteEffectCommand.Execute(null);
                }
            });

            CutCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    CutMaskCommand.Execute(null);
                }
                else
                {
                    CutEffectCommand.Execute(null);
                }
            });

            CopyCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    CopyMaskCommand.Execute(null);
                }
                else
                {
                    CopyEffectCommand.Execute(null);
                }
            });

            PasteCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    PasteMaskCommand.Execute(null);
                }
                else
                {
                    PasteEffectCommand.Execute(null);
                }
            });

            DuplicateCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    DuplicateMaskCommand.Execute(null);
                }
                else
                {
                    DuplicateEffectCommand.Execute(null);
                }
            });

            SelectAllCommand = new DelegateCommand<SelectItemType?>(_ =>
            {
                if (EditingParameter != EditingLayerParameter.None)
                {
                    return;
                }

                var selectTargetIsMask = SelectedMasks.Count > 0;

                DeSelect();
                if (selectTargetIsMask)
                {
                    foreach (var m in Masks)
                    {
                        SelectedMasks.Add(m);
                    }
                }
                else
                {
                    foreach (var e in Effects)
                    {
                        SelectedEffects.Add(e);
                    }
                }
            });

            SavePresetCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    SaveMaskPresetCommand.Execute(null);
                }
                else
                {
                    SaveEffectPresetCommand.Execute(null);
                }
            });

            LoadPresetCommand = new DelegateCommand<SelectItemType?>(type =>
            {
                if (type == SelectItemType.Mask)
                {
                    LoadMaskPresetCommand.Execute(null);
                }
                else
                {
                    LoadEffectPresetCommand.Execute(null);
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

            layerModel.FootageReplaced += LayerModel_FootageReplaced;
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

        public float[] GetAudio(Time time, Time length)
        {
            return LayerModel.GetAudio(time, length);
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (IsLock || (IsSpecial && !IsNullObject))
            {
                dropInfo.NotHandled = true;
                return;
            }

            switch (dropInfo.Data)
            {
                case EffectListDragData effectListData:
                    if (LayerModel.EffectsIsSupported(effectListData.Effects))
                    {
                        dropInfo.Effects = DragDropEffects.Copy;
                        if (dropInfo.VisualTarget is EffectCollectionView)
                        {
                            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                        }
                        else
                        {
                            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                        }
                    }
                    else
                    {
                        dropInfo.NotHandled = true;
                    }
                    break;
                case EffectViewModel effect when Effects.Contains(effect):
                case MaskViewModel mask when Masks.Contains(mask):
                case ItemDragData<EffectViewModel> effectItemDragData when effectItemDragData.SelectedItems.All(Effects.Contains):
                case ItemDragData<MaskViewModel> maskItemDragData when maskItemDragData.SelectedItems.All(Masks.Contains):
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
            if (IsLock || (IsSpecial && !IsNullObject))
            {
                return;
            }

            switch (dropInfo.Data)
            {
                case EffectListDragData effectListData when dropInfo.VisualTarget is EffectCollectionView:
                    if (LayerModel.EffectsIsSupported(effectListData.Effects))
                    {
                        LayerModel.InsertEffect(effectListData.Effects, dropInfo.InsertIndex);
                    }
                    break;
                case EffectListDragData effectListData:
                    if (LayerModel.EffectsIsSupported(effectListData.Effects))
                    {
                        EventHubModel.NotifyAddEffectToSelectedLayers(LayerModel.ParentCompositionId, LayerId, effectListData.Effects);
                    }
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
                case ItemDragData<EffectViewModel> effectItemDragData:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Effects.IndexOf(effectItemDragData.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        LayerModel.MoveEffects([..effectItemDragData.SelectedItems.Select(l => l.EffectId)], effectItemDragData.DragItem.EffectId, newIndex);
                    }
                    break;
                case MaskViewModel mask:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Masks.IndexOf(mask) < newIndex)
                        {
                            newIndex--;
                        }
                        LayerModel.MoveMask(mask.MaskId, newIndex);
                    }
                    break;
                case ItemDragData<MaskViewModel> maskItemDragData:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Masks.IndexOf(maskItemDragData.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        LayerModel.MoveMasks([..maskItemDragData.SelectedItems.Select(m => m.MaskId)], maskItemDragData.DragItem.MaskId, newIndex);
                    }
                    break;
                default:
                    dropInfo.NotHandled = true;
                    break;
            }
        }

        public void DeSelect()
        {
            DeSelectEffects();
            DeSelectMasks();
            TransformProperties?.DeSelect();
            LayerOptionProperties?.DeSelect();
            TextProperties?.DeSelect();
            ShapeProperties?.DeSelect();
            SourceOptionProperties?.DeSelect();
            AudioOptionProperties?.DeSelect();
            TransformProperties?.ClearInteractionState();
            LayerOptionProperties?.ClearInteractionState();
            TextProperties?.ClearInteractionState();
            ShapeProperties?.ClearInteractionState();
            SourceOptionProperties?.ClearInteractionState();
            AudioOptionProperties?.ClearInteractionState();
        }

        void RefreshLayerProxies()
        {
            TrackMatteLayerProxy = TrackMatteViewSource.FirstOrDefault(l => l.LayerId == TrackMatteLayerId);
            ParentLayerProxy = ParentLayerViewSource.FirstOrDefault(l => l.LayerId == ParentLayerId);
        }

        void DeSelectEffects()
        {
            foreach (var effect in SelectedEffects)
            {
                effect.DeSelect();
            }
            SelectedEffects.Clear();
        }

        void DeSelectMasks()
        {
            foreach (var mask in SelectedMasks)
            {
                mask.DeSelect();
            }
            SelectedMasks.Clear();
        }

        void UpdatePropertyGroupViewModels()
        {
            if (LayerModel.TransformProperties != null)
            {
                TransformProperties = new PropertyGroupViewModel(LayerModel.TransformProperties, ViewState);
                TransformProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                TransformProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (TransformProperties != null)
            {
                TransformProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                TransformProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                TransformProperties = null;
            }

            if (LayerModel.LayerOptionProperties != null)
            {
                LayerOptionProperties = new PropertyGroupViewModel(LayerModel.LayerOptionProperties, ViewState);
                LayerOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                LayerOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (LayerOptionProperties != null)
            {
                LayerOptionProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                LayerOptionProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                LayerOptionProperties = null;
            }

            if (LayerModel.TextProperties != null)
            {
                TextProperties = new PropertyGroupViewModel(LayerModel.TextProperties, ViewState);
                TextProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                TextProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (TextProperties != null)
            {
                TextProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                TextProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                TextProperties = null;
            }

            if (LayerModel.ShapeProperties != null)
            {
                ShapeProperties = new PropertyGroupViewModel(LayerModel.ShapeProperties, ViewState);
                ShapeProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                ShapeProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (ShapeProperties != null)
            {
                ShapeProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                ShapeProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                ShapeProperties = null;
            }

            if (LayerModel.SourceOptionProperties != null)
            {
                SourceOptionProperties = new PropertyGroupViewModel(LayerModel.SourceOptionProperties, ViewState);
                SourceOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                SourceOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (SourceOptionProperties != null)
            {
                SourceOptionProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                SourceOptionProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                SourceOptionProperties = null;
            }

            if (LayerModel.AudioOptionProperties != null)
            {
                AudioOptionProperties = new PropertyGroupViewModel(LayerModel.AudioOptionProperties, ViewState);
                AudioOptionProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged;
                AudioOptionProperties.PropertyValueCommited += PropertyGroupViewModel_PropertyValueCommited;
            }
            else if (AudioOptionProperties != null)
            {
                AudioOptionProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                AudioOptionProperties.PropertyValueCommited -= PropertyGroupViewModel_PropertyValueCommited;
                AudioOptionProperties = null;
            }

            if (LayerModel.AudioLevelValueProperties != null)
            {
                AudioLevelValueProperties = new PropertyGroupViewModel(LayerModel.AudioLevelValueProperties, ViewState);
                AudioLevelValueProperties.SelectItemChanged += PropertyGroupViewModel_SelectItemChanged; ;
                AudioLevelValueProperties.PropertyValueCommited += AudioLevelValueProperties_PropertyValueCommited;
                HasAudioLevelValueKeyFrames = LayerModel.AudioLevelValueProperties.Children?.OfType<PropertyModel>()?.Any(p => p.HasKeyFrame()) ?? false;
            }
            else if (AudioLevelValueProperties != null)
            {
                AudioLevelValueProperties.SelectItemChanged -= PropertyGroupViewModel_SelectItemChanged;
                AudioLevelValueProperties.PropertyValueCommited -= AudioLevelValueProperties_PropertyValueCommited;
                AudioLevelValueProperties = null;
            }
        }

        partial void WiringModel();

        private void LayerModel_FootageReplaced(object? sender, EventArgs e)
        {
            UpdatePropertyGroupViewModels();
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

            LayerModel.ChangeEffectsEnable([..targetEffects.Select(e => e.EffectId)], e.IsEnabled);
        }

        private void Effect_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            if (IsLock)
            {
                DeSelect();
            }
            else if (e.SelectItemType != SelectItemType.Effect)
            {
                var targetEffect = e.Effect;
                foreach (var effect in SelectedEffects.Where(v => v != targetEffect).ToArray())
                {
                    SelectedEffects.Remove(effect);
                }
                if (targetEffect != null && !SelectedEffects.Contains(targetEffect))
                {
                    SelectedEffects.Add(targetEffect);

                    DeSelectMasks();
                    TransformProperties?.DeSelect();
                    LayerOptionProperties?.DeSelect();
                    TextProperties?.DeSelect();
                    ShapeProperties?.DeSelect();
                    SourceOptionProperties?.DeSelect();
                    AudioOptionProperties?.DeSelect();
                    TransformProperties?.ClearInteractionState();
                    LayerOptionProperties?.ClearInteractionState();
                    TextProperties?.ClearInteractionState();
                    ShapeProperties?.ClearInteractionState();
                    SourceOptionProperties?.ClearInteractionState();
                    AudioOptionProperties?.ClearInteractionState();
                }
            }
            else
            {
                // NOTE: エフェクトのプロパティのみ選択解除する
                foreach (var effect in SelectedEffects)
                {
                    effect.DeSelect();
                }

                DeSelectMasks();
                TransformProperties?.DeSelect();
                LayerOptionProperties?.DeSelect();
                TextProperties?.DeSelect();
                ShapeProperties?.DeSelect();
                SourceOptionProperties?.DeSelect();
                AudioOptionProperties?.DeSelect();
                TransformProperties?.ClearInteractionState();
                LayerOptionProperties?.ClearInteractionState();
                TextProperties?.ClearInteractionState();
                ShapeProperties?.ClearInteractionState();
                SourceOptionProperties?.ClearInteractionState();
                AudioOptionProperties?.ClearInteractionState();
            }
        }

        private void Effect_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }

        private void Mask_MaskEnableChangeRequest(object? sender, MaskEnableChangeEventArgs e)
        {
            if (sender is not MaskViewModel mask)
            {
                return;
            }

            var targetMasks = new List<MaskViewModel>();
            if (SelectedMasks.Count < 1 || !SelectedMasks.Contains(mask))
            {
                targetMasks.Add(mask);
            }
            else
            {
                targetMasks.AddRange(SelectedMasks);
            }

            LayerModel.ChangeMasksEnable([..targetMasks.Select(m => m.MaskId)], e.IsEnabled);
        }

        private void Mask_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            if (IsLock)
            {
                DeSelect();
            }
            else if (e.SelectItemType != SelectItemType.Mask)
            {
                var targetMask = e.Mask;
                foreach (var mask in SelectedMasks.Where(v => v != targetMask).ToArray())
                {
                    SelectedMasks.Remove(mask);
                }
                if (targetMask != null && !SelectedMasks.Contains(targetMask))
                {
                    SelectedMasks.Add(targetMask);

                    DeSelectEffects();
                    TransformProperties?.DeSelect();
                    LayerOptionProperties?.DeSelect();
                    TextProperties?.DeSelect();
                    ShapeProperties?.DeSelect();
                    SourceOptionProperties?.DeSelect();
                    AudioOptionProperties?.DeSelect();
                    TransformProperties?.ClearInteractionState();
                    LayerOptionProperties?.ClearInteractionState();
                    TextProperties?.ClearInteractionState();
                    ShapeProperties?.ClearInteractionState();
                    SourceOptionProperties?.ClearInteractionState();
                    AudioOptionProperties?.ClearInteractionState();
                }
            }
            else
            {
                // NOTE: マスクのプロパティのみ選択解除する
                foreach (var mask in SelectedMasks)
                {
                    mask.DeSelect();
                }

                DeSelectEffects();
                TransformProperties?.DeSelect();
                LayerOptionProperties?.DeSelect();
                TextProperties?.DeSelect();
                ShapeProperties?.DeSelect();
                SourceOptionProperties?.DeSelect();
                AudioOptionProperties?.DeSelect();
                TransformProperties?.ClearInteractionState();
                LayerOptionProperties?.ClearInteractionState();
                TextProperties?.ClearInteractionState();
                ShapeProperties?.ClearInteractionState();
                SourceOptionProperties?.ClearInteractionState();
                AudioOptionProperties?.ClearInteractionState();
            }
        }

        private void Mask_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
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

        private void SelectedMasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var mask in Masks)
                {
                    mask.DeSelect();
                }
            }
            else
            {
                foreach (var mask in e.OldItems?.OfType<MaskViewModel>() ?? [])
                {
                    mask.DeSelect();
                }
            }
        }

        private void PropertyGroupViewModel_SelectItemChanged(object? sender, SelectItemEventArgs e)
        {
            SelectItemChangedPublisher.Publish(sender, new SelectItemEventArgs(e, this));
            DeSelectEffects();
            DeSelectMasks();
            if (!e.ObjectHierarchy.Contains(TransformProperties))
            {
                TransformProperties?.DeSelect();
                TransformProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(LayerOptionProperties))
            {
                LayerOptionProperties?.DeSelect();
                LayerOptionProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(TextProperties))
            {
                TextProperties?.DeSelect();
                TextProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(ShapeProperties))
            {
                ShapeProperties?.DeSelect();
                ShapeProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(SourceOptionProperties))
            {
                SourceOptionProperties?.DeSelect();
                SourceOptionProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(AudioOptionProperties))
            {
                AudioOptionProperties?.DeSelect();
                AudioOptionProperties?.ClearInteractionState();
            }
            if (!e.ObjectHierarchy.Contains(AudioLevelValueProperties))
            {
                AudioLevelValueProperties?.DeSelect();
                AudioLevelValueProperties?.ClearInteractionState();
            }
        }

        private void PropertyGroupViewModel_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
        }

        private void AudioLevelValueProperties_PropertyValueCommited(object? sender, PropertyValueCommitedEventArgs e)
        {
            PropertyValueCommitedPublisher.Publish(this, e);
            HasAudioLevelValueKeyFrames = AudioLevelValueProperties?.Children?.OfType<PropertyViewModel>()?.Any(p => p.HasKeyFrame) ?? false;
        }
    }

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class LayerModelProxy : BindableBase
    {
        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Guid LayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial SourceType SourceType { get; set; }

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

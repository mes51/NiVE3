using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.Util;
using NiVE3.ValueObject;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Commands;
using System.Windows.Threading;
using NiVE3.Numerics;
using NiVE3.View.Command;
using NiVE3.Config;
using GongSolutions.Wpf.DragDrop;
using ComputeSharp;
using NiVE3.InternalShader;
using NiVE3.Exceptions;
using NiVE3.Model.UI;
using System.Threading;
using System.Buffers;
using NiVE3.Plugin.ValueObject;
using Prism.Dialogs;
using NiVE3.ViewModel.Dialog;
using NiVE3.View.Dialog;
using NiVE3.Cache;
using System.Numerics;
using NiVE3.Shared.Extension;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PreviewViewModel : PaneViewModelBase, IDropTarget
    {
        const int Black = 255 << 24;

        const double AudioShiftToleranceRate = 0.5;

        static readonly TimeSpan AudioSpeedChangeInterval = TimeSpan.FromSeconds(2.0);

        static readonly WriteableBitmap EmptyImage = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Bgra32, null);

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial bool IsFootage { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial SourceType SourceType { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial Time WorkareaBegin { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial Time WorkareaEnd { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel))]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial int Width { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial int Height { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel))]
        public partial double DownScaleRate { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel))]
        public partial bool IsLock { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial bool IsIgnoreUpdatePreview { get; set; }

        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public ObservableCollection<Guid>? SelectedLayerIds
        {
            get;
            set
            {
                if (field != null)
                {
                    field.CollectionChanged -= SelectedLayerIds_CollectionChanged;
                }
                if (value != null)
                {
                    value.CollectionChanged += SelectedLayerIds_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        }

        [ReactiveProperty]
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public partial Guid? CurrentEditingCompositionId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel), IsOneWay = true)]
        public partial bool IsPlaying { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(HistoryModel), BindTargetName = nameof(HistoryModel.IsChanging), IsOneWay = true)]
        public partial bool HistoryIsChanging { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public partial NManagedImage? SnapShotImage { get; set; }

        [ReactiveProperty]
        public partial double RealFrameRate { get; set; }

        [ReactiveProperty]
        public partial bool RealFrameRateIsUpdated { get; set; }

        [ReactiveProperty]
        public partial Time TimeBarRange { get; set; }

        [ReactiveProperty]
        public partial Time TimeBarRangeStart { get; set; }

        [ReactiveProperty]
        public partial double Scale { get; set; } = 100.0;

        [ReactiveProperty]
        public partial double ScreenX { get; set; }

        [ReactiveProperty]
        public partial double ScreenY { get; set; }

        [ReactiveProperty]
        public partial bool IsStretchPreview { get; set; }

        [ReactiveProperty]
        public partial bool IsStretchLimited { get; set; }

        [ReactiveProperty]
        public partial bool IsScrubbing { get; set; }

        [ReactiveProperty]
        public partial PreviewColorChannel PreviewColorChannel { get; set; } = PreviewColorChannel.Rgb;

        [ReactiveProperty]
        public partial WriteableBitmap CurrentFrame { get; set; } = EmptyImage;

        [ReactiveProperty]
        public partial WriteableBitmap SnapShotFrame { get; set; } = EmptyImage;

        [ReactiveProperty]
        public partial ObservableCollection<ColoredPreviewBoundingBox> BoundingBoxes { get; set; } = [];

        [ReactiveProperty]
        public partial ToolType ToolType { get; set; } = ToolType.Select;

        [ReactiveProperty]
        public partial ToolType ActiveRotateTool { get; set; } = ToolType.RotateAll;

        [ReactiveProperty]
        public partial ToolType ActiveCameraTool { get; set; } = ToolType.CameraOrbit;

        [ReactiveProperty]
        public partial ProceduralInputItem[] ProceduralInputItems { get; set; } = [];

        [ReactiveProperty]
        public partial bool IsShowSnapShotImage { get; set; }

        public PreviewModelBase PreviewModel { get; }

        public ICommand ChangeCurrentTimeCommand { get; }

        public ICommand SelectLayerCommand { get; }

        public ICommand BeginUseToolCommand { get; }

        public ICommand KeyDownWhenUsingToolCommand { get; }

        public ICommand KeyUpWhenUsingToolCommand { get; }

        public ICommand MoveLayersByToolCommand { get; }

        public ICommand AbortUseToolCommand { get; }

        [ShortcutGesture(nameof(ShortcutKeySetting.SelectHandToolGesture), IsGlobal = true)]
        public ICommand ChangeToHandToolCommand { get; }

        [ShortcutGesture(nameof(ShortcutKeySetting.SelectSelectToolGesture), IsGlobal = true)]
        public ICommand ChangeToSelectToolCommand { get; }

        [ShortcutGesture(nameof(ShortcutKeySetting.SelectRotateToolGesture), IsGlobal = true)]
        public ICommand ChangeToRotateToolCommand { get; }

        [ShortcutGesture(nameof(ShortcutKeySetting.SelectScaleGestureGesture), IsGlobal = true)]
        public ICommand ChangeToScaleCommand { get; }

        [ShortcutGesture(nameof(ShortcutKeySetting.SelectCameraToolGesture), IsGlobal = true)]
        public ICommand ChangeToCameraToolCommand { get; }

        public ICommand AddShapeCommand { get; }

        public ICommand AddCameraCommand { get; }

        public ICommand AddLightCommand { get; }

        public ICommand AddNullObjectCommand { get; }

        public ICommand AddTextCommand { get; }

        public ICommand AddProceduralFootageCommand { get; }

        public ICommand CompositionSettingCommand { get; }

        public ICommand CaptureSnapShotCommand { get; }

        // TODO: 描画をコマンドにするべき?
        public ICommand RenderPropertyInteractionCommand { get; }

        int[] ImageBuffer { get; set; }

        Int32Rect BufferImageSize { get; set; }

        bool IsDirtyImageBuffer { get; set; }

        bool IsDirtyBoundingBoxesBuffer { get; set; }

        bool IsCurrentFrameUpdating { get; set; } // フラグでは無く何かしらのロック機構を用意した方が良い?

        bool NeedUpdateFrameNextTick { get; set; }

        bool IsUsingTool { get; set; }

        Debouncer FrameUpdateDebouncer { get; }

        ViewStateModel ViewState { get; }

        ApplicationModel ApplicationModel { get; }

        PlayControllerModel PlayControllerModel { get; }

        AudioPlayerModel AudioPlayerModel { get; }

        AudioInformationModel AudioInformationModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        EventHubModel EventHubModel { get; }

        HistoryModel HistoryModel { get; }

        IDialogService DialogService { get; }

        ColoredPreviewBoundingBox[] BoundingBoxesBuffer { get; set; } = [];

        DispatcherTimer RealFrameRateUpdateTimer { get; }

        WeakEventPublisher<EventArgs> SourceChangedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> SourceChanged
        {
            add { SourceChangedPublisher.Subscribe(value); }
            remove { SourceChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> WorkareaChangedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> WorkareaChanged
        {
            add { WorkareaChangedPublisher.Subscribe(value); }
            remove { WorkareaChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> CurrentTimeChangeByUserPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> CurrentTimeChangeByUser
        {
            add { CurrentTimeChangeByUserPublisher.Subscribe(value); }
            remove { CurrentTimeChangeByUserPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> UpdatePropertyInteractionRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> UpdatePropertyInteractionRequest
        {
            add { UpdatePropertyInteractionRequestPublisher.Subscribe(value); }
            remove { UpdatePropertyInteractionRequestPublisher.Unsubscribe(value); }
        }

        Task? RenderRamPreviewTask { get; set; }

        CancellationTokenSource RenderRamPreviewTaskCancellationTokenSource { get; set; } = new CancellationTokenSource();

        List<int[]> CachedRamPreviewFrames { get; set; } = [];

        public PreviewViewModel(
            PreviewModelBase previewModel,
            ViewStateModel viewState,
            ApplicationModel applicationModel,
            ProceduralInputListModel proceduralInputListModel,
            PlayControllerModel playControllerModel,
            AudioPlayerModel audioPlayerModel,
            AudioInformationModel audioInformationModel,
            AcceleratorModel acceleratorModel,
            EventHubModel eventHubModel,
            HistoryModel historyModel,
            IDialogService dialogService
        )
        {
            PreviewModel = previewModel;
            ViewState = viewState;
            ApplicationModel = applicationModel;
            PlayControllerModel = playControllerModel;
            AudioPlayerModel = audioPlayerModel;
            AudioInformationModel = audioInformationModel;
            AcceleratorModel = acceleratorModel;
            EventHubModel = eventHubModel;
            HistoryModel = historyModel;
            DialogService = dialogService;

            ProceduralInputItems = proceduralInputListModel.ProceduralFootageItems;

            RealFrameRateUpdateTimer = new DispatcherTimer { Interval = AudioSpeedChangeInterval };
            RealFrameRateUpdateTimer.Tick += RealFrameRateUpdateTimer_Tick;

            ChangeCurrentTimeCommand = new DelegateCommand(() => CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty));

            SelectLayerCommand = new DelegateCommand<PreviewSelectArgs>(args =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                args.Selected = EventHubModel.NotifySelectLayer(compositionPreviewModel.Composition.CompositionId, args.Position, args.PreviewScale, CurrentTime);
            });

            BeginUseToolCommand = new DelegateCommand<Tuple<Vector2d, Vector2d>>(t =>
            {
                if (ToolType == ToolType.Hand || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var propertyType = ToolType switch
                {
                    ToolType.Select => BeginUseToolEventArgs.PropertyType.Transform,
                    ToolType.RotateAll => BeginUseToolEventArgs.PropertyType.RotateAll,
                    ToolType.RotateX => BeginUseToolEventArgs.PropertyType.RotateX,
                    ToolType.RotateY => BeginUseToolEventArgs.PropertyType.RotateY,
                    ToolType.RotateZ => BeginUseToolEventArgs.PropertyType.RotateZ,
                    ToolType.Scale => BeginUseToolEventArgs.PropertyType.Scale,
                    ToolType.CameraOrbit => BeginUseToolEventArgs.PropertyType.CameraOrbit,
                    ToolType.CameraPan => BeginUseToolEventArgs.PropertyType.CameraPan,
                    ToolType.CameraDolly => BeginUseToolEventArgs.PropertyType.CameraDolly,
                    _ => throw new Exception() // bug
                };
                IsUsingTool = true;
                var (pos, scale) = t;
                EventHubModel.NotifyBeginUseTool(compositionPreviewModel.Composition.CompositionId, pos, scale, propertyType, CurrentTime);
            });

            KeyDownWhenUsingToolCommand = new DelegateCommand<PropertyInteractionKeyArgs>(args =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                args.Processed = EventHubModel.NotifyKeyDownWhenUsingTool(compositionPreviewModel.Composition.CompositionId, args.Key, args.Position, args.PreviewScale, CurrentTime);
            });

            KeyUpWhenUsingToolCommand = new DelegateCommand<PropertyInteractionKeyArgs>(args =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                args.Processed = EventHubModel.NotifyKeyUpWhenUsingTool(compositionPreviewModel.Composition.CompositionId, args.Key, args.Position, args.PreviewScale, CurrentTime);
            });

            MoveLayersByToolCommand = new DelegateCommand<Tuple<Vector2d, Vector2d, bool>>(t =>
            {
                if (!IsUsingTool || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var (nextPos, scale, isCommit) = t;
                IsUsingTool = !isCommit;

                EventHubModel.NotifyMoveLayersByTool(compositionPreviewModel.Composition.CompositionId, nextPos, scale, isCommit, CurrentTime);
            });

            AbortUseToolCommand = new DelegateCommand(() =>
            {
                if (!IsUsingTool || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                IsUsingTool = false;

                EventHubModel.NotifyAbortUseTool(compositionPreviewModel.Composition.CompositionId);
            });

            ChangeToHandToolCommand = new DelegateCommand(() => ToolType = ToolType.Hand, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            ChangeToSelectToolCommand = new DelegateCommand(() => ToolType = ToolType.Select, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            ChangeToRotateToolCommand = new DelegateCommand(() =>
            {
                if (ToolType == ToolType.RotateAll || ToolType == ToolType.RotateX || ToolType == ToolType.RotateY || ToolType == ToolType.RotateZ)
                {
                    ActiveRotateTool = ToolType switch
                    {
                        ToolType.RotateAll => ToolType.RotateX,
                        ToolType.RotateX => ToolType.RotateY,
                        ToolType.RotateY => ToolType.RotateZ,
                        _ => ToolType.RotateAll
                    };
                }
                ToolType = ActiveRotateTool;
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null);

            ChangeToScaleCommand = new DelegateCommand(() => ToolType = ToolType.Scale, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            ChangeToCameraToolCommand = new DelegateCommand(() =>
            {
                if (ToolType == ToolType.CameraOrbit || ToolType == ToolType.CameraPan || ToolType == ToolType.CameraDolly)
                {
                    ActiveCameraTool = ToolType switch
                    {
                        ToolType.CameraOrbit => ToolType.CameraPan,
                        ToolType.CameraPan => ToolType.CameraDolly,
                        _ => ToolType.CameraOrbit
                    };
                }
                ToolType = ActiveCameraTool;
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddShapeCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.AddShape(0);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddCameraCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.AddCamera(0);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddLightCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.AddLight(0);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddNullObjectCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.AddNullObject(0);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddTextCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.AddText(0);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            AddProceduralFootageCommand = new DelegateCommand<ProceduralInputItem>(item =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                compositionPreviewModel.Composition.InsertLayers(item.FootageId, 0);
            }, _ => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);
            
            CompositionSettingCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var compositionModel = compositionPreviewModel.Composition;

                var param = new DialogParameters
                {
                    { nameof(CompositionSettingViewModel.Name), compositionModel.Name },
                    { nameof(CompositionSettingViewModel.Width), compositionModel.Width },
                    { nameof(CompositionSettingViewModel.Height), compositionModel.Height },
                    { nameof(CompositionSettingViewModel.FrameRate), compositionModel.FrameRate },
                    { nameof(CompositionSettingViewModel.Duration), compositionModel.Duration },
                    { nameof(CompositionSettingViewModel.IsRetentionFrameRate), compositionModel.IsRetentionFrameRate },
                    { nameof(CompositionSettingViewModel.ApplyToneMappingWhenNested), compositionModel.ApplyToneMappingWhenNested },
                    { nameof(CompositionSettingViewModel.ShutterAngle), compositionModel.ShutterAngle },
                    { nameof(CompositionSettingViewModel.ShutterPhase), compositionModel.ShutterPhase },
                    { nameof(CompositionSettingViewModel.MotionBlurSampleCount), compositionModel.MotionBlurSampleCount },
                    { CompositionSettingViewModel.SelectedRendererPluginId, compositionModel.RendererPluginId },
                    { CompositionSettingViewModel.SelectedToneMapperPluginId, compositionModel.ToneMapperPluginId }
                };
                if (compositionModel.RendererSetting != null)
                {
                    param.Add(nameof(CompositionSettingViewModel.RendererSetting), compositionModel.RendererSetting);
                }
                if (compositionModel.ToneMapperSetting != null)
                {
                    param.Add(nameof(CompositionSettingViewModel.ToneMapperSetting), compositionModel.ToneMapperSetting);
                }
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(CompositionSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK)
                {
                    compositionModel.ChangeCompositionSetting(
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
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            CaptureSnapShotCommand = new DelegateCommand(() =>
            {
                if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                using var checker = CycleChecker.StartCheck();

                compositionPreviewModel.CaptureSnapShot(CurrentTime);
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            RenderPropertyInteractionCommand = new DelegateCommand<Tuple<DrawingContext, Vector2d, Vector2d>>(t =>
            {
                var compositionId = (PreviewModel as CompositionPreviewModel)?.Composition?.CompositionId;
                if (!compositionId.HasValue)
                {
                    return;
                }

                var (drawingContext, previewImagePosition, previewImageScale) = t;
                EventHubModel.NotifyRenderPreviewInteractionRequest(compositionId.Value, CurrentTime, drawingContext, previewImagePosition, previewImageScale);
            }, _ => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
                .ObservesProperty(() => PreviewModel);

            WiringModel();

            PreviewModel.SourceChanged += PreviewModel_SourceChanged;

            Title = $"{LanguageResourceDictionary.Dictionary.GetText(IsFootage ? LanguageResourceDictionary.PreviewView_FootageTitle : LanguageResourceDictionary.PreviewView_CompositionTitle)} {(SourceType != SourceType.None ? Name : LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PreviewView_Title_ItemEmpty))}";
            TimeBarRange = previewModel.Duration;

            BufferImageSize = new Int32Rect(0, 0, Math.Max(Width, 1), Math.Max(Height, 1));
            CurrentFrame = new WriteableBitmap(BufferImageSize.Width, BufferImageSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            PreviewModel.FrameUpdateRequest += PreviewModel_FrameUpdateRequest;
            PropertyChanged += PreviewViewModel_PropertyChanged;

            ImageBuffer = new int[BufferImageSize.Width * BufferImageSize.Height];
            ImageBuffer.AsSpan().Fill(new Vector4(1.0F, 1.0F, 1.0F, 0.0F).ToIntColor());
            FrameUpdateDebouncer = new Debouncer(1);
            FrameUpdateDebouncer.Tick += (_, _) =>
            {
                UpdateCurrentFrame();
            };
            FrameUpdateDebouncer.ResetAndStart();

            PlayControllerModel.PreviewPlay += PlayControllerModel_PreviewPlay;
            PlayControllerModel.Stopped += PlayControllerModel_Stopped;
            PlayControllerModel.PauseChanged += PlayControllerModel_PauseChanged;
            PlayControllerModel.StartRenderRamPreview += PlayControllerModel_StartRenderRamPreview;
            PlayControllerModel.StopRenderRamPreview += PlayControllerModel_StopRenderRamPreview;
            ViewState.PropertyChanged += ViewState_PropertyChanged;

            CompositionTarget.Rendering += (_, _) =>
            {
                if (RenderRamPreviewTask != null)
                {
                    if (CachedRamPreviewFrames.Count > 0)
                    {
                        CurrentFrame.WritePixels(BufferImageSize, CachedRamPreviewFrames[^1], BufferImageSize.Width * 4, 0);
                    }
                    return;
                }

                if (IsDirtyImageBuffer)
                {
                    CurrentFrame.WritePixels(BufferImageSize, ImageBuffer, BufferImageSize.Width * 4, 0);
                    IsDirtyImageBuffer = false;
                }
                if (ViewState.IsIgnoreUpdatePreview || HistoryIsChanging)
                {
                    return;
                }

                if (IsDirtyBoundingBoxesBuffer)
                {
                    BoundingBoxes.Clear();
                    foreach (var bb in BoundingBoxesBuffer)
                    {
                        BoundingBoxes.Add(bb);
                    }
                    IsDirtyBoundingBoxesBuffer = false;
                }
                if (NeedUpdateFrameNextTick)
                {
                    NeedUpdateFrameNextTick = false;
                    FrameUpdateDebouncer.ResetAndStart();
                }
            };
        }

        public Vector4 GetPixel(int x, int y)
        {
            if (x < 0 || x > Width || y < 0 || y > Height)
            {
                return new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                return ImageConversion.ToBGRA128(ImageBuffer[y * Width + x]);
            }
        }

        public void Unbind()
        {
            PlayControllerModel.PreviewPlay -= PlayControllerModel_PreviewPlay;
            PlayControllerModel.Stopped -= PlayControllerModel_Stopped;
            PlayControllerModel.PauseChanged -= PlayControllerModel_PauseChanged;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
            {
                dropInfo.NotHandled = true;
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(dropInfo.VisualTarget);
            var screenPos = (Vector2d)dropInfo.DropPosition - new Vector2d(ScreenX, ScreenY);
            var pos = screenPos * new Vector2d(dpi.DpiScaleX, dpi.DpiScaleY) / (Scale * 0.01);
            var layerId = (Guid?)null;

            using (var checker = CycleChecker.StartCheck())
            {
                layerId = compositionPreviewModel.Composition.FindLayerByPreviewPosition(CurrentTime, pos);
            }

            switch (dropInfo.Data)
            {
                case IFootageViewModel:
                case IFootageViewModel[]:
                case EffectListDragData when layerId.HasValue:
                    dropInfo.Effects = DragDropEffects.Copy;
                    break;
                default:
                    dropInfo.NotHandled = true;
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
            {
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(dropInfo.VisualTarget);
            var screenPos = (Vector2d)dropInfo.DropPosition - new Vector2d(ScreenX, ScreenY);
            var pos = screenPos * new Vector2d(dpi.DpiScaleX, dpi.DpiScaleY) / (Scale * 0.01);
            var layerId = (Guid?)null;

            using (var checker = CycleChecker.StartCheck())
            {
                layerId = compositionPreviewModel.Composition.FindLayerByPreviewPosition(CurrentTime, pos);
            }

            switch (dropInfo.Data)
            {
                case IFootageViewModel footage:
                    compositionPreviewModel.Composition.InsertLayers(footage.FootageId, 0, CurrentTime, (Vector3d)pos);
                    break;
                case IFootageViewModel[] footages:
                    compositionPreviewModel.Composition.InsertLayers([..footages.Select(f => f.FootageId)], 0, CurrentTime, (Vector3d)pos);
                    break;
                case EffectListDragData effectListData when layerId.HasValue:
                    EventHubModel.NotifyAddEffectToSelectedLayers(compositionPreviewModel.Composition.CompositionId, layerId.Value, effectListData.Effects);
                    break;
            }
        }

        partial void WiringModel();

        void UpdateCurrentFrame()
        {
            if (IsDirtyImageBuffer || IsIgnoreUpdatePreview || HistoryIsChanging || IsCurrentFrameUpdating || !PreviewModel.CanRendering)
            {
                NeedUpdateFrameNextTick = true;
                return;
            }
            else if (SourceType == SourceType.Audio)
            {
                return;
            }

            IsCurrentFrameUpdating = true;
            using (var checker = CycleChecker.StartCheck())
            {
                NImage? image;
                try
                {
                    image = PreviewModel.GetImage(CurrentTime);
                }
                catch (GPUException ex)
                {
                    ApplicationModel.CaughtGPUException(ex);
                    IsCurrentFrameUpdating = false;
                    NeedUpdateFrameNextTick = true;
                    return;
                }

                if (image != null && BufferImageSize.Width == image.Width && BufferImageSize.Height == image.Height)
                {
                    try
                    {
                        ConvertTo8bpcImage(AcceleratorModel, image, ImageBuffer, PreviewColorChannel);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            image.Dispose();
                        }
                        catch { }

                        ApplicationModel.CaughtGPUException(new GPUException(ex));
                        IsCurrentFrameUpdating = false;
                        NeedUpdateFrameNextTick = true;
                        return;
                    }
                }
                else
                {
                    if (PreviewColorChannel != PreviewColorChannel.Rgb)
                    {
                        ImageBuffer.AsSpan().Fill(Black);
                    }
                    else
                    {
                        ImageBuffer.AsSpan().Clear();
                    }
                }

                image?.Dispose();
            }

            IsDirtyImageBuffer = true;
            UpdateBoundingBox();
            UpdatePropertyInteraction();
            IsCurrentFrameUpdating = false;
        }

        void UpdateBoundingBox()
        {
            if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null || compositionPreviewModel.Composition.CompositionId != CurrentEditingCompositionId || SelectedLayerIds == null)
            {
                BoundingBoxesBuffer = [];
                return;
            }
            else
            {
                using var checker = CycleChecker.StartCheck();
                BoundingBoxesBuffer = compositionPreviewModel.Composition.GetBoundingBoxes([..SelectedLayerIds], CurrentTime);
            }
            IsDirtyBoundingBoxesBuffer = true;
        }

        void UpdatePropertyInteraction()
        {
            UpdatePropertyInteractionRequestPublisher.Publish(this, EventArgs.Empty);
        }

        void OnWorkareaChanged()
        {
            WorkareaChangedPublisher.Publish(this, EventArgs.Empty);
        }

        void ClearCachedRenderedRamPreviewFrame()
        {
            foreach (var buffer in CachedRamPreviewFrames)
            {
                ArrayPool<int>.Shared.Return(buffer);
            }
            CachedRamPreviewFrames.Clear();
        }

        static void ConvertTo8bpcImage(AcceleratorModel acceleratorModel, NImage image, int[] buffer, PreviewColorChannel channel)
        {
            if (image is NGPUImage gpuImage)
            {
                try
                {
                    var device = acceleratorModel.CurrentDevice;
                    using var convertedImageData = device.AllocateReadWriteBuffer<int>(image.DataLength);
                    using (var context = device.CreateComputeContext())
                    {
                        context.For(gpuImage.Width, gpuImage.Height, new ConvertToPreviewImage(gpuImage.Data, convertedImageData, gpuImage.Width, (int)channel));
                    }
                    convertedImageData.CopyTo(buffer.AsSpan(0, image.DataLength));
                }
                catch (Exception ex)
                {
                    throw new GPUException(ex);
                }
            }
            else
            {
                var dataSize = image.DataLength;
                var imageData = image.GetData();

                // TODO: SDR変換を入れるかどうか
                switch (channel)
                {
                    case PreviewColorChannel.R:
                        Parallel.For(0, dataSize, i =>
                        {
                            var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b10101010);
                            var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                            var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                            var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                            buffer[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                        });
                        break;
                    case PreviewColorChannel.G:
                        Parallel.For(0, dataSize, i =>
                        {
                            var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b01010101);
                            var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                            var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                            var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                            buffer[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                        });
                        break;
                    case PreviewColorChannel.B:
                        Parallel.For(0, dataSize, i =>
                        {
                            var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b00000000);
                            var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                            var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                            var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                            buffer[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                        });
                        break;
                    case PreviewColorChannel.Alpha:
                        Parallel.For(0, dataSize, i =>
                        {
                            var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b11111111);
                            var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                            var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                            var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                            buffer[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                        });
                        break;
                    case PreviewColorChannel.RgbStraight:
                        ImageConversion.ConvertToBGR32(imageData, buffer, dataSize);
                        break;
                    default:
                        ImageConversion.ConvertToBGRA32(imageData, buffer, dataSize);
                        break;
                }
            }
        }

        private void RealFrameRateUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (SourceType == SourceType.Audio)
            {
                CurrentTime = AudioPlayerModel.GetPlayingPosition().RoundToFrameRate(FrameRate);
                return;
            }

            RealFrameRate = Math.Min(PlayControllerModel.RealFrameRate, FrameRate);
            if (RealFrameRate > 0.0)
            {
                var tolerance = new Time(1, FrameRate / AudioShiftToleranceRate);
                var audioPosition = AudioPlayerModel.GetPlayingPosition();
                if (Time.Abs(CurrentTime - audioPosition) > tolerance)
                {
                    AudioPlayerModel.SetPlayingPosition(CurrentTime);
                    AudioPlayerModel.PreviewSpeed = RealFrameRate / FrameRate;
                }
                RealFrameRateIsUpdated = true;
            }
        }

        private void PreviewModel_FrameUpdateRequest(object? sender, EventArgs e)
        {
            UpdateCurrentFrame();
        }

        private void PreviewViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SourceType):
                case nameof(Name):
                    Title = $"{(IsFootage ? "フッテージ" : "コンポジション")} {(SourceType != SourceType.None ? Name : "(なし)")}";
                    break;
                case nameof(Width):
                case nameof(Height):
                    BufferImageSize = new Int32Rect(0, 0, Math.Max(Width, 1), Math.Max(Height, 1));
                    CurrentFrame = new WriteableBitmap(BufferImageSize.Width, BufferImageSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                    ImageBuffer = new int[BufferImageSize.Width * BufferImageSize.Height];
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    UpdateCurrentFrame();
                    break;
                case nameof(CurrentTime):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    if (PlayControllerModel.IsPlaying && !PlayControllerModel.IsPaused && PlayControllerModel.UseRamPreview)
                    {
                        var frame = (int)Math.Round((double)(CurrentTime - WorkareaBegin) * FrameRate);
                        if (frame > -1 && frame < CachedRamPreviewFrames.Count)
                        {
                            IsCurrentFrameUpdating = true;
                            var buffer = CachedRamPreviewFrames[frame];
                            buffer.AsSpan(0, ImageBuffer.Length).CopyTo(ImageBuffer);
                            IsDirtyImageBuffer = true;
                            IsCurrentFrameUpdating = false;
                        }
                        else
                        {
                            UpdateCurrentFrame();
                        }
                    }
                    else
                    {
                        UpdateCurrentFrame();
                    }
                    if (PreviewModel.IsFootage && !PlayControllerModel.IsPlaying && Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var audio = PreviewModel.GetAudio(CurrentTime, new Time(1, FrameRate));
                        if (audio != null)
                        {
                            AudioPlayerModel.AddScrubSample(audio);
                        }
                    }
                    if (PlayControllerModel.IsPlaying && !PlayControllerModel.IsPaused)
                    {
                        var startSample = (int)((double)CurrentTime * Const.AudioSamplingRate) * Const.AudioChannelCount;
                        var length = (int)((double)(CurrentTime + new Time(1, FrameRate)) * Const.AudioSamplingRate) * Const.AudioChannelCount - startSample;
                        length = Math.Min(length, AudioPlayerModel.Audio.Length - startSample);
                        if (length > 0)
                        {
                            AudioInformationModel.CalcAudioLevel(AudioPlayerModel.Audio.AsSpan(startSample, length));
                        }
                        else
                        {
                            AudioInformationModel.ClearLevel();
                        }
                    }
                    break;
                case nameof(Duration):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    TimeBarRange = Duration;
                    TimeBarRangeStart = Time.Zero;
                    OnWorkareaChanged();
                    break;
                case nameof(WorkareaBegin):
                case nameof(WorkareaEnd):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    OnWorkareaChanged();
                    break;
                case nameof(PreviewColorChannel):
                case nameof(DownScaleRate):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    UpdateCurrentFrame();
                    break;
                case nameof(SelectedLayerIds):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    UpdateBoundingBox();
                    UpdatePropertyInteraction();
                    break;
                case nameof(IsScrubbing) when PreviewModel.IsFootage && !PlayControllerModel.IsPlaying:
                    if (IsScrubbing)
                    {
                        AudioPlayerModel.PlayScrub();
                    }
                    else
                    {
                        AudioPlayerModel.StopScrub();
                    }
                    break;
                case nameof(ToolType) when IsUsingTool:
                    AbortUseToolCommand.Execute(null);
                    break;
                case nameof(IsShowSnapShotImage):
                    if (IsShowSnapShotImage && SnapShotImage != null)
                    {
                        var temp = ArrayPool<int>.Shared.Rent(SnapShotImage.DataLength);
                        temp.AsSpan(0, SnapShotImage.DataLength).Clear();
                        ConvertTo8bpcImage(AcceleratorModel, SnapShotImage, temp, PreviewColorChannel);
                        if (SnapShotFrame.Width != SnapShotImage.Width || SnapShotFrame.Height != SnapShotImage.Height)
                        {
                            SnapShotFrame = new WriteableBitmap(SnapShotImage.Width, SnapShotImage.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                        }
                        SnapShotFrame.WritePixels(new Int32Rect(0, 0, SnapShotImage.Width, SnapShotImage.Height), temp, SnapShotImage.Width * 4, 0);
                        ArrayPool<int>.Shared.Return(temp);
                    }
                    break;
            }
        }

        private void PreviewModel_SourceChanged(object? sender, EventArgs e)
        {
            if (IsUsingTool)
            {
                AbortUseToolCommand.Execute(null);
            }
            Scale = 100.0;
            IsStretchPreview = false;
            IsStretchLimited = false;
            DownScaleRate = 1;
            PreviewColorChannel = PreviewColorChannel.Rgb;
            CurrentTime = Time.Zero;
            UpdateCurrentFrame();
            SourceChangedPublisher.Publish(this, EventArgs.Empty);
        }

        private void SelectedLayerIds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateBoundingBox();
            UpdatePropertyInteraction();
        }

        private void PlayControllerModel_PreviewPlay(object? sender, EventArgs e)
        {
            if (!IsSelected)
            {
                return;
            }
            if (IsUsingTool)
            {
                AbortUseToolCommand.Execute(null);
            }

            var audio = Array.Empty<float>();
            if (PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null)
            {
                audio = compositionPreviewModel.Composition.RenderAudio(Time.Zero, Duration);
            }
            else if (PreviewModel is FootagePreviewModel footagePreviewModel && footagePreviewModel.Footage != null && footagePreviewModel.SourceType.HasFlag(SourceType.Audio))
            {
                audio = footagePreviewModel.Footage.ReadAudio(Time.Zero, Duration);
            }
            AudioPlayerModel.SetPreviewAudio(audio, WorkareaBegin, PlayControllerModel.UseRamPreview ? PlayControllerModel.RamPreviewRenderedWorkareaEnd : WorkareaEnd);
            AudioPlayerModel.PreviewSpeed = 1.0;
            AudioPlayerModel.SetPlayingPosition(CurrentTime);
            RealFrameRateIsUpdated = false;
            RealFrameRate = -1.0;
            AudioPlayerModel.PlayPreview();
            RealFrameRateUpdateTimer.Start();
            if (PlayControllerModel.UseRamPreview)
            {
                BoundingBoxesBuffer = [];
                IsDirtyBoundingBoxesBuffer = true;
            }
        }

        private void PlayControllerModel_Stopped(object? sender, EventArgs e)
        {
            AudioPlayerModel.StopPreview();
            RealFrameRateUpdateTimer.Stop();
            RealFrameRateIsUpdated = false;
            RealFrameRate = -1.0;
            AudioInformationModel.ClearLevel();
            ClearCachedRenderedRamPreviewFrame();
            if (PlayControllerModel.UseRamPreview)
            {
                UpdateBoundingBox();
                UpdatePropertyInteraction();
                UpdateCurrentFrame();
            }
        }

        private void PlayControllerModel_PauseChanged(object? sender, EventArgs e)
        {
            RealFrameRateIsUpdated = false;
            RealFrameRate = -1.0;
            if (PlayControllerModel.IsPaused)
            {
                AudioPlayerModel.StopPreview();
                RealFrameRateUpdateTimer.Stop();
            }
            else
            {
                AudioPlayerModel.PreviewSpeed = 1.0;
                AudioPlayerModel.PlayPreview();
                RealFrameRateUpdateTimer.Start();
            }
        }

        private void PlayControllerModel_StartRenderRamPreview(object? sender, EventArgs e)
        {
            if (IsFootage)
            {
                PlayControllerModel.Play();
            }
            else
            {
                ClearCachedRenderedRamPreviewFrame();

                RenderRamPreviewTaskCancellationTokenSource.Dispose();
                RenderRamPreviewTaskCancellationTokenSource = new CancellationTokenSource();
                var ct = RenderRamPreviewTaskCancellationTokenSource.Token;
                RenderRamPreviewTask = Task.Run(() =>
                {
                    using var propertyCache = PropertyValueCache.Start();
                    var frameCount = (int)Math.Round((double)(WorkareaEnd - WorkareaBegin) * FrameRate);
                    for (var i = 0; i < frameCount && CachedRamPreviewFrames.Sum(b => b.Length) / Const.MiB < ApplicationSetting.Setting.RamPreviewCacheLimit; i++)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            var currentTime = WorkareaBegin + new Time(i, FrameRate);
                            using var checker = CycleChecker.StartCheck();
                            using var image = PreviewModel.GetImage(currentTime);
                            if (image == null)
                            {
                                break;
                            }
                            var buffer = ArrayPool<int>.Shared.Rent(image.DataLength);
                            ConvertTo8bpcImage(AcceleratorModel, image, buffer, PreviewColorChannel);
                            CachedRamPreviewFrames.Add(buffer);
                        }
                        catch (GPUException ex)
                        {
                            ApplicationModel.CaughtGPUException(ex);
                            i--;
                            continue;
                        }
                    }

                    Application.Current?.Dispatcher.BeginInvoke(() => PlayControllerModel.Play(), DispatcherPriority.ApplicationIdle);
                }, ct).ContinueWith(task => RenderRamPreviewTask = null);
            }
        }

        private void PlayControllerModel_StopRenderRamPreview(object? sender, StopRenderRamPreviewEventArgs e)
        {
            if (IsFootage)
            {
                e.RenderedFrameCount = (int)Math.Round((double)Duration * FrameRate);
            }
            else
            {
                RenderRamPreviewTaskCancellationTokenSource.Cancel();
                RenderRamPreviewTask?.Wait();
                RenderRamPreviewTask = null;

                e.RenderedFrameCount = CachedRamPreviewFrames.Count;
            }
        }

        private void ViewState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewStateModel.LastSelectedObjectHashCode))
            {
                UpdatePropertyInteraction();
            }
        }
    }

    enum PreviewColorChannel
    {
        Rgb,
        R,
        G,
        B,
        Alpha,
        RgbStraight
    }

    enum ToolType : uint
    {
        LayerSelectableTool = 0x80000000U,
        Hand = 0,
        Select = LayerSelectableTool | 1,
        RotateAll = LayerSelectableTool | 2,
        RotateX = LayerSelectableTool | 3,
        RotateY = LayerSelectableTool | 4,
        RotateZ = LayerSelectableTool | 5,
        Scale = LayerSelectableTool | 6,
        CameraOrbit = 7,
        CameraPan = 8,
        CameraDolly = 9
    }

    class PreviewSelectArgs
    {
        public Vector2d Position { get; }

        public Vector2d PreviewScale { get; }

        public SelectPreviewResult Selected { get; set; }

        public PreviewSelectArgs(Vector2d position, Vector2d previewScale)
        {
            Position = position;
            PreviewScale = previewScale;
        }
    }

    class PropertyInteractionKeyArgs
    {
        public Key Key { get; }

        public Vector2d Position { get; }

        public Vector2d PreviewScale { get; }

        public bool Processed { get; set; }

        public PropertyInteractionKeyArgs(Key key, Vector2d position, Vector2d previewScale)
        {
            Key = key;
            Position = position;
            PreviewScale = previewScale;
        }
    }
}

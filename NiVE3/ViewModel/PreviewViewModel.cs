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

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [CommandHandling(nameof(ChangeToHandToolCommand), nameof(ShortcutKeySetting.SelectHandToolGesture), IsGlobal = true)]
    [CommandHandling(nameof(ChangeToSelectToolCommand), nameof(ShortcutKeySetting.SelectSelectToolGesture), IsGlobal = true)]
    [CommandHandling(nameof(ChangeToRotateToolCommand), nameof(ShortcutKeySetting.SelectRotateToolGesture), IsGlobal = true)]
    [CommandHandling(nameof(ChangeToScaleCommand), nameof(ShortcutKeySetting.SelectScaleGestureGesture), IsGlobal = true)]
    [CommandHandling(nameof(ChangeToCameraToolCommand), nameof(ShortcutKeySetting.SelectCameraToolGesture), IsGlobal = true)]
    partial class PreviewViewModel : PaneViewModelBase, IDropTarget
    {
        const int Black = 255 << 24;

        const double AudioShiftToleranceRate = 0.5;

        static readonly TimeSpan AudioSpeedChangeInterval = TimeSpan.FromSeconds(2.0);

        static readonly WriteableBitmap EmptyImage = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Bgra32, null);

        private string name = "";
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isFootage;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public bool IsFootage
        {
            get { return isFootage; }
            set { SetProperty(ref isFootage, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private double workareaBegin;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private double duration;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double frameRate;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double currentTime;
        [NeedWire(nameof(PreviewModel))]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private int width;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double downScaleRate;
        [NeedWire(nameof(PreviewModel))]
        public double DownScaleRate
        {
            get { return downScaleRate; }
            set { SetProperty(ref downScaleRate, value); }
        }

        private bool isLock;
        [NeedWire(nameof(PreviewModel))]
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private bool isIgnoreUpdatePreview;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public bool IsIgnoreUpdatePreview
        {
            get { return isIgnoreUpdatePreview; }
            set { SetProperty(ref isIgnoreUpdatePreview, value); }
        }

        private ObservableCollection<Guid>? selectedLayerIds;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public ObservableCollection<Guid>? SelectedLayerIds
        {
            get { return selectedLayerIds; }
            set
            {
                if (selectedLayerIds != null)
                {
                    selectedLayerIds.CollectionChanged -= SelectedLayerIds_CollectionChanged;
                }
                if (value != null)
                {
                    value.CollectionChanged += SelectedLayerIds_CollectionChanged;
                }
                SetProperty(ref selectedLayerIds, value);
            }
        }

        private Guid? currentEditingCompositionId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private bool isPlaying;
        [NeedWire(nameof(PlayControllerModel), IsOneWay = true)]
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { SetProperty(ref isPlaying, value); }
        }

        private double realFrameRate;
        public double RealFrameRate
        {
            get { return realFrameRate; }
            set { SetProperty(ref realFrameRate, value); }
        }

        private bool realFrameRateIsUpdated;
        public bool RealFrameRateIsUpdated
        {
            get { return realFrameRateIsUpdated; }
            set { SetProperty(ref realFrameRateIsUpdated, value); }
        }

        private double timeBarRange;
        public double TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private double timeBarRangeStart;
        public double TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }

        private double scale = 100.0;
        public double Scale
        {
            get { return scale; }
            set { SetProperty(ref scale, value); }
        }

        private double screenX;
        public double ScreenX
        {
            get { return screenX; }
            set { SetProperty(ref screenX, value); }
        }

        private double screenY;
        public double ScreenY
        {
            get { return screenY; }
            set { SetProperty(ref screenY, value); }
        }

        private bool isStretchPreview;
        public bool IsStretchPreview
        {
            get { return isStretchPreview; }
            set { SetProperty(ref isStretchPreview, value); }
        }

        private bool isStretchLimited;
        public bool IsStretchLimited
        {
            get { return isStretchLimited; }
            set { SetProperty(ref isStretchLimited, value); }
        }

        private bool isScrubbing;
        public bool IsScrubbing
        {
            get { return isScrubbing; }
            set { SetProperty(ref isScrubbing, value); }
        }

        private PreviewColorChannel previewColorChannel = PreviewColorChannel.Rgb;
        public PreviewColorChannel PreviewColorChannel
        {
            get { return previewColorChannel; }
            set { SetProperty(ref previewColorChannel, value); }
        }

        private WriteableBitmap currentFrame = EmptyImage;
        public WriteableBitmap CurrentFrame
        {
            get { return currentFrame; }
            set { SetProperty(ref currentFrame, value); }
        }

        private ObservableCollection<ColoredPreviewBoundingBox> boundingBoxes = [];
        public ObservableCollection<ColoredPreviewBoundingBox> BoundingBoxes
        {
            get { return boundingBoxes; }
            set { SetProperty(ref boundingBoxes, value); }
        }

        private ToolType toolType;
        public ToolType ToolType
        {
            get { return toolType; }
            set { SetProperty(ref toolType, value); }
        }

        private ToolType activeRotateTool = ToolType.RotateAll;
        public ToolType ActiveRotateTool
        {
            get { return activeRotateTool; }
            set { SetProperty(ref activeRotateTool, value); }
        }

        private ToolType activeCameraTool = ToolType.CameraOrbit;
        public ToolType ActiveCameraTool
        {
            get { return activeCameraTool; }
            set { SetProperty(ref activeCameraTool, value); }
        }

        public PreviewModelBase PreviewModel { get; }

        public ICommand ChangeCurrentTimeCommand { get; }

        public ICommand SelectLayerCommand { get; }

        public ICommand BeginUseToolCommand { get; }

        public ICommand MoveLayersByToolCommand { get; }

        public ICommand AbortUseToolCommand { get; }

        public ICommand ChangeToHandToolCommand { get; }

        public ICommand ChangeToSelectToolCommand { get; }

        public ICommand ChangeToRotateToolCommand { get; }

        public ICommand ChangeToScaleCommand { get; }

        public ICommand ChangeToCameraToolCommand { get; }

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

        ColoredPreviewBoundingBox[]? BoundingBoxesBuffer { get; set; }

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

        public PreviewViewModel(PreviewModelBase previewModel, ViewStateModel viewState, ApplicationModel applicationModel, PlayControllerModel playControllerModel, AudioPlayerModel audioPlayerModel, AudioInformationModel audioInformationModel, AcceleratorModel acceleratorModel, EventHubModel eventHubModel)
        {
            PreviewModel = previewModel;
            ViewState = viewState;
            ApplicationModel = applicationModel;
            PlayControllerModel = playControllerModel;
            AudioPlayerModel = audioPlayerModel;
            AudioInformationModel = audioInformationModel;
            AcceleratorModel = acceleratorModel;
            EventHubModel = eventHubModel;

            RealFrameRateUpdateTimer = new DispatcherTimer { Interval = AudioSpeedChangeInterval };
            RealFrameRateUpdateTimer.Tick += RealFrameRateUpdateTimer_Tick;

            ChangeCurrentTimeCommand = new DelegateCommand(() => CurrentTimeChangeByUserPublisher.Publish(this, EventArgs.Empty));

            SelectLayerCommand = new DelegateCommand<Vector2d?>(p =>
            {
                if (p == null || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var pos = p.Value / (Scale * 0.01);
                var layerId = compositionPreviewModel.Composition.FindLayerByPreviewPosition(CurrentTime, pos);
                EventHubModel.NotifySelectLayer(compositionPreviewModel.Composition.CompositionId, layerId);
            });

            BeginUseToolCommand = new DelegateCommand<Vector2d?>(p =>
            {
                if (!p.HasValue || ToolType == ToolType.Hand || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var propertyType = ToolType switch
                {
                    ToolType.Select => BeginUseToolEvent.PropertyType.Transform,
                    ToolType.RotateAll => BeginUseToolEvent.PropertyType.RotateAll,
                    ToolType.RotateX => BeginUseToolEvent.PropertyType.RotateX,
                    ToolType.RotateY => BeginUseToolEvent.PropertyType.RotateY,
                    ToolType.RotateZ => BeginUseToolEvent.PropertyType.RotateZ,
                    ToolType.Scale => BeginUseToolEvent.PropertyType.Scale,
                    ToolType.CameraOrbit => BeginUseToolEvent.PropertyType.CameraOrbit,
                    ToolType.CameraPan => BeginUseToolEvent.PropertyType.CameraPan,
                    ToolType.CameraDolly => BeginUseToolEvent.PropertyType.CameraDolly,
                    _ => throw new Exception() // bug
                };
                IsUsingTool = true;
                EventHubModel.NotifyBeginUseTool(compositionPreviewModel.Composition.CompositionId, p.Value / (Scale * 0.01), propertyType);
            });

            MoveLayersByToolCommand = new DelegateCommand<Tuple<Vector2d, bool>>(t =>
            {
                if (!IsUsingTool || PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null)
                {
                    return;
                }

                var (nextPos, isCommit) = t;
                nextPos /= Scale * 0.01;
                IsUsingTool = !isCommit;

                EventHubModel.NotifyMoveLayersByTool(compositionPreviewModel.Composition.CompositionId, nextPos, isCommit);
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

            ChangeToHandToolCommand = new DelegateCommand(() => ToolType = ToolType.Hand, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null);

            ChangeToSelectToolCommand = new DelegateCommand(() => ToolType = ToolType.Select, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null);

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

            ChangeToScaleCommand = new DelegateCommand(() => ToolType = ToolType.Scale, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null);

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
            }, () => PreviewModel is CompositionPreviewModel compositionPreviewModel && compositionPreviewModel.Composition != null);

            WiringModel();

            PreviewModel.SourceChanged += PreviewModel_SourceChanged;

            Title = $"{LanguageResourceDictionary.Dictionary.GetText(IsFootage ? LanguageResourceDictionary.PreviewView_FootageTitle : LanguageResourceDictionary.PreviewView_CompositionTitle)} {(SourceType != SourceType.None ? Name : LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PreviewView_Title_ItemEmpty))}";
            TimeBarRange = previewModel.Duration;

            BufferImageSize = new Int32Rect(0, 0, Math.Max(Width, 1), Math.Max(Height, 1));
            CurrentFrame = new WriteableBitmap(BufferImageSize.Width, BufferImageSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            PreviewModel.FrameUpdateRequest += PreviewModel_FrameUpdateRequest;
            PropertyChanged += PreviewViewModel_PropertyChanged;

            ImageBuffer = new int[BufferImageSize.Width * BufferImageSize.Height];
            FrameUpdateDebouncer = new Debouncer(1);
            FrameUpdateDebouncer.Tick += (_, _) =>
            {
                UpdateCurrentFrame();
            };
            FrameUpdateDebouncer.ResetAndStart();

            PlayControllerModel.PreviewPlay += PlayControllerModel_PreviewPlay;
            PlayControllerModel.Stopped += PlayControllerModel_Stopped;
            PlayControllerModel.PauseChanged += PlayControllerModel_PauseChanged;

            CompositionTarget.Rendering += (_, _) =>
            {
                if (IsDirtyImageBuffer)
                {
                    CurrentFrame.WritePixels(BufferImageSize, ImageBuffer, BufferImageSize.Width * 4, 0);
                    IsDirtyImageBuffer = false;
                }
                if (IsDirtyBoundingBoxesBuffer)
                {
                    BoundingBoxes.Clear();
                    if (BoundingBoxesBuffer != null)
                    {
                        foreach (var bb in BoundingBoxesBuffer)
                        {
                            BoundingBoxes.Add(bb);
                        }
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
            var layerId = compositionPreviewModel.Composition.FindLayerByPreviewPosition(CurrentTime, pos);

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
            var layerId = compositionPreviewModel.Composition.FindLayerByPreviewPosition(CurrentTime, pos);

            switch (dropInfo.Data)
            {
                case IFootageViewModel footage:
                    compositionPreviewModel.Composition.InsertLayers(footage.FootageId, 0, CurrentTime, (Vector3d)pos);
                    break;
                case IFootageViewModel[] footages:
                    compositionPreviewModel.Composition.InsertLayers([..footages.Select(f => f.FootageId)], 0, CurrentTime, (Vector3d)pos);
                    break;
                case EffectListDragData effectListData when layerId.HasValue:
                    compositionPreviewModel.Composition.AddEffectToLayer(layerId.Value, effectListData.Effects);
                    break;
            }
        }

        partial void WiringModel();

        void UpdateCurrentFrame()
        {
            if (IsDirtyImageBuffer || IsIgnoreUpdatePreview || IsCurrentFrameUpdating)
            {
                NeedUpdateFrameNextTick = true;
                return;
            }
            else if (SourceType == SourceType.Audio)
            {
                return;
            }

            IsCurrentFrameUpdating = true;

            NImage? image;
            try
            {
                image = PreviewModel.GetImage(CurrentTime);
            }
            catch (GPUException ex)
            {
                IsCurrentFrameUpdating = false;
                NeedUpdateFrameNextTick = true;
                ApplicationModel.CaughtGPUException(ex);
                return;
            }

            if (image != null && BufferImageSize.Width == image.Width && BufferImageSize.Height == image.Height)
            {
                if (image is NGPUImage gpuImage)
                {
                    try
                    {
                        var device = AcceleratorModel.CurrentDevice;
                        using var convertedImageData = device.AllocateReadWriteBuffer<int>(image.DataLength);
                        using (var context = device.CreateComputeContext())
                        {
                            context.For(gpuImage.Width, gpuImage.Height, new ConvertToPreviewImage(gpuImage.Data, convertedImageData, gpuImage.Width, (int)PreviewColorChannel));
                        }
                        convertedImageData.CopyTo(ImageBuffer);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            image.Dispose();
                        }
                        catch { }

                        IsCurrentFrameUpdating = false;
                        NeedUpdateFrameNextTick = true;
                        ApplicationModel.CaughtGPUException(new GPUException(ex));
                        return;
                    }
                }
                else
                {
                    var dataSize = image.DataLength;
                    var imageData = image.GetData();
                    var data = ImageBuffer;

                    // TODO: SDR変換を入れるかどうか
                    switch (PreviewColorChannel)
                    {
                        case PreviewColorChannel.R:
                            Parallel.For(0, dataSize, i =>
                            {
                                var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b10101010);
                                var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                                var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                                var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                                data[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                            });
                            break;
                        case PreviewColorChannel.G:
                            Parallel.For(0, dataSize, i =>
                            {
                                var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b01010101);
                                var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                                var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                                var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                                data[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                            });
                            break;
                        case PreviewColorChannel.B:
                            Parallel.For(0, dataSize, i =>
                            {
                                var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b00000000);
                                var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                                var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                                var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                                data[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                            });
                            break;
                        case PreviewColorChannel.Alpha:
                            Parallel.For(0, dataSize, i =>
                            {
                                var p = Avx.Permute(Sse41.RoundCurrentDirection(imageData[i].AsVector128() * 255.0F), 0b11111111);
                                var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                                var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                                var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                                data[i] = Sse2.ConvertToInt32(p8.AsInt32()) | Black;
                            });
                            break;
                        case PreviewColorChannel.RgbStraight:
                            ImageConversion.ConvertToBGR32(imageData, ImageBuffer, dataSize);
                            break;
                        default:
                            ImageConversion.ConvertToBGRA32(imageData, ImageBuffer, dataSize);
                            break;
                    }
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

            IsDirtyImageBuffer = true;
            UpdateBoundingBox();
            IsCurrentFrameUpdating = false;
        }

        void UpdateBoundingBox()
        {
            if (PreviewModel is not CompositionPreviewModel compositionPreviewModel || compositionPreviewModel.Composition == null || compositionPreviewModel.Composition.CompositionId != CurrentEditingCompositionId || SelectedLayerIds == null)
            {
                BoundingBoxesBuffer = null;
                return;
            }
            else
            {
                BoundingBoxesBuffer = compositionPreviewModel.Composition.GetBoundingBoxes([..SelectedLayerIds], CurrentTime);
            }
            IsDirtyBoundingBoxesBuffer = true;
        }

        void OnWorkareaChanged()
        {
            WorkareaChangedPublisher.Publish(this, EventArgs.Empty);
        }

        private void RealFrameRateUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (SourceType == SourceType.Audio)
            {
                CurrentTime = TimeCalc.AlignRound(AudioPlayerModel.GetPlayingPosition(), FrameRate);
                return;
            }

            RealFrameRate = Math.Min(PlayControllerModel.RealFrameRate, FrameRate);
            if (RealFrameRate > 0.0)
            {
                var tolerance = 1.0 / FrameRate * AudioShiftToleranceRate;
                var audioPosition = AudioPlayerModel.GetPlayingPosition();
                if (Math.Abs(CurrentTime - audioPosition) > tolerance)
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
                    UpdateCurrentFrame();
                    if (PreviewModel.IsFootage && !PlayControllerModel.IsPlaying && Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var audio = PreviewModel.GetAudio(CurrentTime, 1.0 / FrameRate);
                        if (audio != null)
                        {
                            AudioPlayerModel.AddScrubSample(audio);
                        }
                    }
                    if (PlayControllerModel.IsPlaying && !PlayControllerModel.IsPaused)
                    {
                        var startSample = (int)(CurrentTime * Const.AudioSamplingRate) * Const.AudioChannelCount;
                        var length = (int)((CurrentTime + 1.0 / FrameRate) * Const.AudioSamplingRate) * Const.AudioChannelCount - startSample;
                        AudioInformationModel.CalcAudioLevel(AudioPlayerModel.Audio.AsSpan(startSample, length));
                    }
                    break;
                case nameof(Duration):
                    if (IsUsingTool)
                    {
                        AbortUseToolCommand.Execute(null);
                    }
                    TimeBarRange = Duration;
                    TimeBarRangeStart = 0.0;
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
            previewColorChannel = PreviewColorChannel.Rgb;
            CurrentTime = 0.0;
            UpdateCurrentFrame();
            SourceChangedPublisher.Publish(this, EventArgs.Empty);
        }

        private void SelectedLayerIds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateBoundingBox();
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
                audio = compositionPreviewModel.Composition.RenderAudio(0.0, Duration);
            }
            else if (PreviewModel is FootagePreviewModel footagePreviewModel && footagePreviewModel.Footage != null && footagePreviewModel.SourceType.HasFlag(SourceType.Audio))
            {
                audio = footagePreviewModel.Footage.ReadAudio(0.0, Duration);
            }
            AudioPlayerModel.SetPreviewAudio(audio, WorkareaBegin, WorkareaEnd);
            AudioPlayerModel.PreviewSpeed = 1.0;
            AudioPlayerModel.SetPlayingPosition(CurrentTime);
            RealFrameRateIsUpdated = false;
            RealFrameRate = -1.0;
            AudioPlayerModel.PlayPreview();
            RealFrameRateUpdateTimer.Start();
        }

        private void PlayControllerModel_Stopped(object? sender, EventArgs e)
        {
            AudioPlayerModel.StopPreview();
            RealFrameRateUpdateTimer.Stop();
            RealFrameRateIsUpdated = false;
            RealFrameRate = -1.0;
            AudioInformationModel.ClearLevel();
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

    enum ToolType
    {
        Hand,
        Select,
        RotateAll,
        RotateX,
        RotateY,
        RotateZ,
        Scale,
        CameraOrbit,
        CameraPan,
        CameraDolly
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Input;
using NiVE3.Numerics;
using NiVE3.Image;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Shared.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Resource;
using NiVE3.Image.Drawing;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Buffers;
using NiVE3.Util;
using NiVE3.Mvvm;
using NiVE3.Data.Clipboard;
using System.IO.Hashing;
using NiVE3.Extension;
using NiVE3.Cache;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Util;
using System.Text.Json;
using NiVE3.Exceptions;
using ComputeSharp;
using NiVE3.InternalShader.MotionBlur;
using NiVE3.Model.UI;
using NiVE3.Image.Color;
using NiVE3.InternalShader.Mask;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.Model
{
    [UseReactiveProperty]
    partial class CompositionModel : WeakPropertyChangedBindingBase, IDisposable, ICompositionObject
    {
        public Guid CompositionId { get; }

        [ReactiveProperty]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        public partial int Width { get; set; }

        [ReactiveProperty]
        public partial int Height { get; set; }

        [ReactiveProperty]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        public partial Time FrameDuration { get; private set; }

        [ReactiveProperty]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        public partial bool IsRetentionFrameRate { get; set; }

        [ReactiveProperty]
        public partial  bool ApplyToneMappingWhenNested { get; set; }

        [ReactiveProperty]
        public partial int ShutterAngle { get; set; }

        [ReactiveProperty]
        public partial int ShutterPhase { get; set; }

        [ReactiveProperty]
        public partial int MotionBlurSampleCount { get; set; }

        [ReactiveProperty]
        public partial Time TimeBarRange { get; set; }

        [ReactiveProperty]
        public partial Time TimeBarRangeStart { get; set; }

        [ReactiveProperty]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        public partial Time WorkareaBegin { get; set; }

        [ReactiveProperty]
        public partial Time WorkareaEnd { get; set; }

        [ReactiveProperty]
        public partial bool IsEnableShy { get; set; }

        [ReactiveProperty]
        public partial bool IsEnableFrameBlend { get; set; }

        [ReactiveProperty]
        public partial bool IsEnableMotionBlur { get; set; }

        public ObservableCollection<LayerModel> Layers
        {
            get;
            set
            {
                if (field != value)
                {
                    field.CollectionChanged -= Layers_CollectionChanged;
                    value.CollectionChanged += Layers_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        } = [];

        [ReactiveProperty]
        public partial ObservableCollection<Marker> CompositionMarkers { get; set; } = [];

        public Guid RendererPluginId { get; private set; }

        public Guid ToneMapperPluginId { get; private set; }

        public object? RendererSetting { get; private set; }

        public object? ToneMapperSetting { get; private set; }

        public bool HasAudio => Layers.Any(l => l.HasAudio && l.IsEnableAudio && l.IsEnableSolo) ? Layers.Any(l => l.HasAudio && l.IsEnableAudio && l.IsEnableSolo) : Layers.Any(l => l.HasAudio && l.IsEnableAudio);

        public Int32Size Size => new Int32Size(Width, Height);

        public IReadOnlyCollection<LayerInfo> LayerIdentifiers => [..Layers.Select(l => new LayerInfo(l.LayerId, l.SourceType))];

        public bool IsRendering { get; set; }

        WeakEventPublisher<NeedHistoryChangeEventArgs> CompositionUpdatedPublisher { get; } = new WeakEventPublisher<NeedHistoryChangeEventArgs>();
        public event EventHandler<NeedHistoryChangeEventArgs> CompositionUpdated
        {
            add { CompositionUpdatedPublisher.Subscribe(value); }
            remove { CompositionUpdatedPublisher.Unsubscribe(value); }
        }

        bool IsSettingChanging { get; set; }

        ExportLifetimeContext<IRenderer> RendererContext { get; set; }

        ExportLifetimeContext<IToneMapper> ToneMapperContext { get; set; }

        FootageListModel FootageListModel { get; }

        EffectListModel EffectListModel { get; }

        RenderQueueModel RenderQueueModel { get; }

        TextPropertyModel TextPropertyModel { get; }

        RendererListModel RendererListModel { get; }

        ToneMapperListModel ToneMapperListModel { get; }

        ProjectModel ProjectModel { get; }

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        IRenderer Renderer => RendererContext.Value;

        IToneMapper ToneMapper => ToneMapperContext.Value;

        ITransformer? Transformer { get; set; }

        Int128 RendererSettingHash { get; set; }

        Int128 ToneMapperSettingHash { get; set; }

        public CompositionModel(
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            ProjectModel projectModel,
            HistoryModel historyModel,
            AcceleratorModel acceleratorModel
        ) : this(rendererPluginId, toneMapperPluginId, footageListModel, effectListModel, renderQueueModel, textPropertyModel, rendererListModel, toneMapperListModel, projectModel, historyModel, acceleratorModel, null) { }

        public CompositionModel(
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            ProjectModel projectModel,
            HistoryModel historyModel,
            AcceleratorModel acceleratorModel,
            Guid? compositionId
        )
        {
            CompositionId = compositionId ?? Guid.NewGuid();
            RendererContext = rendererListModel.CreateRenderer(rendererPluginId);
            ToneMapperContext = toneMapperListModel.CreateToneMapper(toneMapperPluginId);
            RendererPluginId = rendererPluginId;
            ToneMapperPluginId = toneMapperPluginId;
            FootageListModel = footageListModel;
            EffectListModel = effectListModel;
            RenderQueueModel = renderQueueModel;
            RendererListModel = rendererListModel;
            ToneMapperListModel = toneMapperListModel;
            ProjectModel = projectModel;
            TextPropertyModel = textPropertyModel;
            HistoryModel = historyModel;
            AcceleratorModel = acceleratorModel;
            // NOTE: プロパティの初期化はsetterの中を経由せずに直接バッキングフィールドに値が代入されるため、setterの中を経由したい場合はコンストラクタで代入する
            // SEE: https://github.com/dotnet/csharplang/blob/main/meetings/2022/LDM-2022-03-02.md#open-questions-in-field
            Layers = [];

            PropertyChanged += CompositionModel_PropertyChanged;
        }

        public void ApplyInitialSettingData(object? rendererSettingData, object? toneMapperSettingData)
        {
            Renderer.ApplySetting(rendererSettingData);
            RendererSetting = Renderer.SaveSetting();
            RendererSettingHash = CalcPluginSettingHash(RendererSetting);
            ToneMapper.ApplySetting(toneMapperSettingData);
            ToneMapperSetting = ToneMapper.SaveSetting();
            ToneMapperSettingHash = CalcPluginSettingHash(ToneMapperSetting);
        }

        public void InsertLayers(Guid footageId, int index, Vector3d? initialPosition = null)
        {
            InsertLayers(footageId, index, Time.Zero, initialPosition);
        }

        public void InsertLayers(Guid footageId, int index, Time sourceStartPoint, Vector3d? initialPosition = null)
        {
            InsertLayers([footageId], index, sourceStartPoint, initialPosition);
        }

        public void InsertLayers(Guid[] footageIds, int index, Vector3d? initialPosition = null)
        {
            InsertLayers(footageIds, index, Time.Zero, initialPosition);
        }

        public void InsertLayers(Guid[] footageIds, int index, Time sourceStartPoint, Vector3d? initialPosition = null)
        {
            var footages = footageIds.SelectMany(FootageListModel.GetFootages);
            var addedLayers = new List<LayerModel>();
            var startIndex = index;
            foreach (var f in footages)
            {
                if (f.InputModel.Input is CompositionInput compositionInput && IsCycledComposition(this, compositionInput.Composition))
                {
                    continue;
                }
                var layer = new LayerModel(ProjectModel, this, f, EffectListModel, HistoryModel, AcceleratorModel);
                if (f.InputType == SourceType.Image || f.InputType == SourceType.None)
                {
                    layer.OutPoint = Duration;
                }
                layer.SourceStartPoint = sourceStartPoint;
                if (initialPosition.HasValue && layer.TransformProperties?.FindProperty(ILayerObject.TransformPositionId) is PropertyModel position)
                {
                    position.UpdateUncommitedRawValue(initialPosition.Value);
                }

                addedLayers.Add(layer);
            }

            if (addedLayers.Count > 0)
            {
                InsertLayerInternal([..addedLayers], startIndex);
            }
        }

        public void AddCamera(int insertIndex)
        {
            var layer = new LayerModel(ProjectModel, this, FootageListModel.CameraFootage, EffectListModel, HistoryModel, AcceleratorModel)
            {
                OutPoint = Duration
            };
            InsertLayerInternal([layer], insertIndex);
        }

        public void AddLight(int insertIndex)
        {
            var layer = new LayerModel(ProjectModel, this, FootageListModel.LightFootage, EffectListModel, HistoryModel, AcceleratorModel)
            {
                OutPoint = Duration
            };
            InsertLayerInternal([layer], insertIndex);
        }

        public void AddNullObject(int insertIndex)
        {
            var layer = new LayerModel(ProjectModel, this, FootageListModel.NullObjectFootage, EffectListModel, HistoryModel, AcceleratorModel)
            {
                OutPoint = Duration
            };
            InsertLayerInternal([layer], insertIndex);
        }

        public void AddText(int insertIndex)
        {
            var layer = new LayerModel(ProjectModel, this, FootageListModel.TextFootage, EffectListModel, HistoryModel, AcceleratorModel)
            {
                OutPoint = Duration
            };

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddLayers));

            TextPropertyModel.UpdateTextProperty(layer, Time.Zero, null);
            InsertLayerInternal([layer], insertIndex);

            HistoryModel.EndGroup();
        }

        public void AddShape(int insertIndex)
        {
            var layer = new LayerModel(ProjectModel, this, FootageListModel.ShapeFootage, EffectListModel, HistoryModel, AcceleratorModel)
            {
                OutPoint = Duration
            };
            InsertLayerInternal([layer], insertIndex);
        }

        public void MoveLayer(Guid layerId, int newIndex)
        {
            MoveLayers([layerId], layerId, newIndex);
        }

        public void MoveLayers(Guid[] layerIds, Guid referenceLayerId, int newIndex)
        {
            if (Layers.Count == layerIds.Length)
            {
                return;
            }

            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var prevIndices = layers.Select(l => Layers.IndexOf(l)).ToArray();
            var startIndex = newIndex - layers.FindIndex(l => l.LayerId == referenceLayerId);
            var newOrderedLayers = new List<LayerModel>(Layers.Count);
            newOrderedLayers.AddRange(Layers.Except(layers).Take(startIndex));
            newOrderedLayers.AddRange(layers);
            newOrderedLayers.AddRange(Layers.Except([..newOrderedLayers]));

            Layers.SortBy(l => newOrderedLayers.IndexOf(l));

            if (!prevIndices.SequenceEqual(layers.Select(Layers.IndexOf)))
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveLayers));

                foreach (var layer in Layers)
                {
                    layer.UpdateCompositionDependProperties();
                    layer.ClearCacheByLayerUpdated();
                }
                HistoryModel.Add(new MoveLayersHistoryCommand(this, layers, prevIndices, [..newOrderedLayers]));

                HistoryModel.EndGroup();
            }
        }

        public void ChangeLayerSwitches(Guid[] layerIds, string switchName, object newValue)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var propertyInfo = typeof(LayerModel).GetProperty(switchName);
            Assertion.IsNotNull(propertyInfo, $"{switchName} Switch is not found");

            var oldValue = layers.Select(propertyInfo.GetValue).ToArray();
            foreach (var l in layers)
            {
                propertyInfo.SetValue(l, newValue);
            }

            HistoryModel.Add(new ChangeLayerSwitchHistoryCommand(layers, propertyInfo, oldValue, newValue));
        }

        public void ChangeBlendModes(Guid[] layerIds, BlendMode blendMode)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldValues = layers.Select(l => l.BlendMode).ToArray();
            foreach (var l in layers)
            {
                l.BlendMode = blendMode;
            }

            HistoryModel.Add(new ChangeBlendModeHistoryCommand(layers, oldValues, blendMode));
        }

        public void ChangeTrackMatteLayers(Guid[] layerIds, Guid? targetLayerId)
        {
            var layers = Layers.Where(l => layerIds.Where(id => id != targetLayerId).Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldValues = layers.Select(l => l.TrackMatteLayerId).ToArray();

            if (targetLayerId.HasValue)
            {
                var targetLayer = Layers.First(l => l.LayerId == targetLayerId);
                foreach (var l in layers)
                {
                    l.TrackMatteLayerId = targetLayerId;
                }
                var oldEnableVideo = targetLayer.IsEnableVideo;
                targetLayer.IsEnableVideo = false;

                HistoryModel.Add(new ChangeTrackMatteLayerHistoryCommand(layers, targetLayer, oldValues, targetLayerId, oldEnableVideo));
            }
            else
            {
                foreach (var l in layers)
                {
                    l.TrackMatteLayerId = null;
                }

                HistoryModel.Add(new ChangeTrackMatteLayerHistoryCommand(layers, null, oldValues, targetLayerId, false));
            }
        }

        public void ChangeTrackMatteModes(Guid[] layerIds, TrackMatteMode mode)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId) && l.TrackMatteLayerId.HasValue).OrderBy(Layers.IndexOf).ToArray();
            var oldValues = layers.Select(l => l.TrackMatteMode).ToArray();
            foreach (var l in layers)
            {
                l.TrackMatteMode = mode;
            }

            HistoryModel.Add(new ChangeTrackMatteModeHistoryCommand(layers, oldValues, mode));
        }

        public void ChangeParentLayer(Guid[] layerIds, Guid? targetLayerId)
        {
            if (targetLayerId.HasValue)
            {
                layerIds = layerIds.Except([targetLayerId.Value]).ToArray();
            }
            var changed = layerIds.ToDictionary(id => id, _ => targetLayerId);
            var layers = new List<LayerModel>();
            if (targetLayerId.HasValue)
            {
                foreach (var l in Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf))
                {
                    if (CheckCycledSimulatedParentLayer(l.LayerId, changed))
                    {
                        changed.Remove(l.LayerId);
                    }
                    else
                    {
                        layers.Add(l);
                    }
                }
            }
            else
            {
                layers.AddRange(Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf));
            }
            var oldValues = layers.Select(l => l.ParentLayerId).ToArray();

            // NOTE: プレビュー中の無限ループ回避用
            foreach (var l in layers)
            {
                l.ParentLayerId = null;
            }

            foreach (var l in layers)
            {
                l.ParentLayerId = targetLayerId;
            }

            HistoryModel.Add(new ChangeParentLayerHistoryCommand([..layers], oldValues, targetLayerId));
        }

        public bool CheckCycledParentLayer(Guid layerId, Guid targetLayerId)
        {
            if (layerId == targetLayerId)
            {
                return true;
            }

            var layer = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (layer == null || !layer.ParentLayerId.HasValue)
            {
                return false;
            }
            else
            {
                return CheckCycledParentLayer(layer.ParentLayerId.Value, layerId);
            }
        }

        public void AddSolid(int index)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddSolid));
            var solidId = FootageListModel.AddSolid();

            if (solidId.HasValue)
            {
                InsertLayers(solidId.Value, index);
                HistoryModel.EndGroup();
            }
            else
            {
                HistoryModel.AbortGroup();
            }
        }

        public void DeleteLayers(Guid[] ids)
        {
            DeleteLayersInternal(ids, false);
        }

        public void DeleteLayersByFootage(FootageModel footage)
        {
            var layerIds = Layers.Where(l => l.IsSameFootage(footage)).Select(l => l.LayerId).ToArray();
            DeleteLayers(layerIds);
        }

        public void EnqueueRender(string filePath, RenderRangeType renderRangeType, Time beginTime, Time endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput> output)
        {
            RenderQueueModel.Enqueue(this, filePath, renderRangeType, beginTime, endTime, isOutputVideo, isOutputAudio, output);
        }

        public void ChangeCompositionSetting(
            string name,
            int width,
            int height,
            double frameRate,
            Time duration,
            bool isRetentionFrameRate,
            bool applyToneMappingWhenNested,
            int shutterAngle,
            int shutterPhase,
            int motionBlurSampleCount,
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            bool rendererSettingChanged,
            object? rendererSettingData,
            bool toneMapperSettingChanged,
            object? toneMapperSettingData
        )
        {
            if (Name == name &&
                Width == width &&
                Height == height &&
                FrameRate == frameRate &&
                Duration == duration &&
                IsRetentionFrameRate == isRetentionFrameRate &&
                ApplyToneMappingWhenNested == applyToneMappingWhenNested &&
                ShutterAngle == shutterAngle &&
                ShutterPhase == shutterPhase &&
                MotionBlurSampleCount == motionBlurSampleCount &&
                RendererPluginId == rendererPluginId &&
                ToneMapperPluginId == toneMapperPluginId &&
                !rendererSettingChanged &&
                !toneMapperSettingChanged)
            {
                return;
            }

            IsSettingChanging = true;

            var prevName = Name;
            var prevWidth = Width;
            var prevHeight = Height;
            var prevFrameRate = FrameRate;
            var prevDuration = Duration;
            var prevIsRetentionFrameRate = IsRetentionFrameRate;
            var prevApplyToneMappingWhenNested = ApplyToneMappingWhenNested;
            var prevShutterAngle = ShutterAngle;
            var prevShutterPhase = ShutterPhase;
            var prevMotionBlurSampleCount = MotionBlurSampleCount;
            var prevRendererPluginId = RendererPluginId;
            var prevToneMapperPluginId = ToneMapperPluginId;
            var prevRendererSetting = RendererSetting;
            var prevRendererSettingHash = RendererSettingHash;
            var prevToneMapperSetting = ToneMapperSetting;
            var prevToneMapperSettingHash = ToneMapperSettingHash;
            var prevWorkareaBegin = WorkareaBegin;
            var prevWorkareaEnd = WorkareaEnd;

            Name = name;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            Duration = duration;
            IsRetentionFrameRate = isRetentionFrameRate;
            ApplyToneMappingWhenNested = applyToneMappingWhenNested;
            ShutterAngle = shutterAngle;
            ShutterPhase = shutterPhase;
            MotionBlurSampleCount = motionBlurSampleCount;

            Transformer?.SetSize(Width, Height);
            if (RendererPluginId != rendererPluginId)
            {
                RendererContext.Dispose();
                RendererContext = RendererListModel.CreateRenderer(rendererPluginId);
                RendererPluginId = rendererPluginId;
                RendererContext.Value.SetSize(Width, Height);
                if (rendererSettingChanged)
                {
                    RendererContext.Value.ApplySetting(rendererSettingData);
                }
                RendererSetting = RendererContext.Value.SaveSetting();
                RendererSettingHash = CalcPluginSettingHash(RendererSetting);
                Transformer = null;
            }
            else if (rendererSettingChanged && RendererContext.Value.ApplySetting(rendererSettingData))
            {
                RendererSetting = RendererContext.Value.SaveSetting();
                RendererSettingHash = CalcPluginSettingHash(RendererSetting);
            }
            if (ToneMapperPluginId != toneMapperPluginId)
            {
                ToneMapperContext.Dispose();
                ToneMapperContext = ToneMapperListModel.CreateToneMapper(toneMapperPluginId);
                ToneMapperPluginId = toneMapperPluginId;
                if (toneMapperSettingChanged)
                {
                    ToneMapperContext.Value.ApplySetting(toneMapperSettingData);
                }
                ToneMapperSetting = ToneMapperContext.Value.SaveSetting();
                ToneMapperSettingHash = CalcPluginSettingHash(ToneMapperSetting);
            }
            else if (toneMapperSettingChanged && ToneMapperContext.Value.ApplySetting(toneMapperSettingData))
            {
                ToneMapperSetting = ToneMapperContext.Value.SaveSetting();
                ToneMapperSettingHash = CalcPluginSettingHash(ToneMapperSetting);
            }

            FrameDuration = new Time(1, FrameRate);
            if (TimeBarRange > Duration)
            {
                TimeBarRange = Duration;
            }
            if (TimeBarRangeStart + TimeBarRange > Duration)
            {
                TimeBarRangeStart = Time.Max(Duration - TimeBarRangeStart, Time.Zero);
            }
            if (WorkareaBegin == 0.0 && WorkareaEnd == prevDuration)
            {
                WorkareaEnd = duration;
            }
            else
            {
                WorkareaBegin = Time.Min(WorkareaBegin, Duration - FrameDuration);
                WorkareaEnd = Time.Clamp(WorkareaEnd, WorkareaBegin + FrameDuration, Duration);
            }

            if (prevWidth != Width || prevHeight != Height)
            {
                Renderer.SetSize(Width, Height);
            }

            IsSettingChanging = false;

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeCompositionSetting));

            OnCompositionUpdated(true);

            HistoryModel.Add(
                new ChangeCompositionSettingHistoryCommand(
                    this,
                    prevName,
                    prevWidth,
                    prevHeight,
                    prevFrameRate,
                    prevDuration,
                    prevIsRetentionFrameRate,
                    prevApplyToneMappingWhenNested,
                    prevShutterAngle,
                    prevShutterPhase,
                    prevMotionBlurSampleCount,
                    prevRendererPluginId,
                    prevToneMapperPluginId,
                    prevRendererSetting,
                    prevRendererSettingHash,
                    prevToneMapperSetting,
                    prevToneMapperSettingHash,
                    prevWorkareaBegin,
                    prevWorkareaEnd,
                    name,
                    width,
                    height,
                    frameRate,
                    duration,
                    isRetentionFrameRate,
                    applyToneMappingWhenNested,
                    shutterAngle,
                    shutterPhase,
                    motionBlurSampleCount,
                    rendererPluginId,
                    toneMapperPluginId,
                    RendererSetting,
                    RendererSettingHash,
                    ToneMapperSetting,
                    ToneMapperSettingHash,
                    WorkareaBegin,
                    WorkareaEnd
                )
            );

            HistoryModel.EndGroup();
        }

        public void ChangeWorkarea(Time begin, Time end)
        {
            var prevWorkareaBegin = WorkareaBegin;
            var prevWorkareaEnd = WorkareaEnd;

            if (begin > end)
            {
                (begin, end) = (end, begin);
            }

            var newWorkareaBegin = Time.Clamp(begin, Time.Zero, Duration - FrameDuration);
            var newWorkareaEnd = Time.Clamp(end, WorkareaBegin + FrameDuration, Duration);
            if (prevWorkareaBegin == newWorkareaBegin && prevWorkareaEnd == newWorkareaEnd)
            {
                return;
            }

            WorkareaBegin = newWorkareaBegin;
            WorkareaEnd = newWorkareaEnd;

            HistoryModel.Add(new ChangeWorkareaHistoryCommand(this, prevWorkareaBegin, prevWorkareaEnd, WorkareaBegin, WorkareaEnd));
        }

        public void ChangeEnableShy()
        {
            IsEnableShy = !IsEnableShy;

            HistoryModel.Add(new ChangeEnableShyHistoryCommand(this, IsEnableShy));
        }

        public void ChangeEnableFrameBlend()
        {
            IsEnableFrameBlend = !IsEnableFrameBlend;

            HistoryModel.Add(new ChangeEnableFrameBlendHistoryCommand(this, IsEnableFrameBlend));
        }

        public void ChangeEnableMotionBlur()
        {
            IsEnableMotionBlur = !IsEnableMotionBlur;

            HistoryModel.Add(new ChangeEnableMotionBlurHistoryCommand(this, IsEnableMotionBlur));
        }

        public NImage RenderFrame(Time time, double downSamplingRate, bool applyToneMapping, bool useGpu)
        {
            IsRendering = true;

            try
            {
                if (IsEnableMotionBlur && Layers.Any(l => l.IsMotionBlurTarget()))
                {
                    var shatterStartTime = FrameDuration * ShutterPhase / 360.0F;
                    var hash = new XxHash3();
                    if (downSamplingRate == 1.0)
                    {
                        CalcCacheHash(hash, time, Time.Zero, false);
                    }

                    var cacheKey = hash.ToInt128();
                    var device = useGpu ? AcceleratorModel.CurrentDevice : null;
                    if (downSamplingRate != 1.0 || !ImageCache.TryGet(CompositionId, cacheKey, time, device, out var cachedImage))
                    {
                        var frameBlendRatio = 1.0F / MotionBlurSampleCount;
                        var subFrameInterval = (FrameDuration * ShutterAngle / 360.0) / MotionBlurSampleCount;
                        if (device != null)
                        {
                            var result = new NGPUImage(Width, Height, device, Vector4.Zero);
                            for (var i = 0; i < MotionBlurSampleCount; i++)
                            {
                                using var subFrame = RenderFrameInternal(time, shatterStartTime + subFrameInterval * i, true, downSamplingRate, applyToneMapping, useGpu);

                                var gpuImage = subFrame.ToGpu(device);

                                device.For(Width, Height, new BlendSubFrame(result.Data, gpuImage.Data, Width, frameBlendRatio));

                                if (subFrame != gpuImage)
                                {
                                    gpuImage.Dispose();
                                }
                            }

                            device.For(Width, Height, new UnPremultiply(result.Data, Width));

                            ImageCache.Add(CompositionId, cacheKey, time, result, ROI.Empty);

                            return result;
                        }
                        else
                        {
                            var result = new NManagedImage(Width, Height);
                            for (var i = 0; i < MotionBlurSampleCount; i++)
                            {
                                using var subFrame = RenderFrameInternal(time, shatterStartTime + subFrameInterval * i, true, downSamplingRate, applyToneMapping, useGpu);

                                var managedImage = subFrame.ToManaged();

                                Parallel.For(0, Height, y =>
                                {
                                    var pos = y * Width;
                                    var resultSpan = result.Data.AsSpan(pos, Width);
                                    var subFrameSpan = managedImage.Data.AsSpan(pos, Width);
                                    for (var i = 0; i < resultSpan.Length; i++)
                                    {
                                        var color = subFrameSpan[i];
                                        var alpha = color.W * frameBlendRatio;
                                        color *= alpha;
                                        color.W = alpha;
                                        resultSpan[i] += color;
                                    }
                                });

                                if (subFrame != managedImage)
                                {
                                    managedImage.Dispose();
                                }
                            }

                            Parallel.For(0, Height, y =>
                            {
                                var resultSpan = result.Data.AsSpan(y * Width, Width);
                                for (var i = 0; i < resultSpan.Length; i++)
                                {
                                    var color = resultSpan[i];
                                    if (color.W > 0.0F)
                                    {
                                        var alpha = color.W;
                                        color /= alpha;
                                        color.W = alpha;
                                        resultSpan[i] = color;
                                    }
                                }
                            });

                            ImageCache.Add(CompositionId, cacheKey, time, result, ROI.Empty);

                            return result;
                        }
                    }
                    else
                    {
                        return cachedImage.Item1;
                    }
                }
                else
                {
                    return RenderFrameInternal(time, Time.Zero, false, downSamplingRate, applyToneMapping, useGpu);
                }
            }
            finally
            {
                IsRendering = false;
            }
        }

        public float[] RenderAudio(Time time, Time length)
        {
            var vectorLength = Vector<float>.Count;

            var result = new float[(int)(length * Const.AudioSamplingRate) * 2];
            var resultVectorSpan = MemoryMarshal.Cast<float, Vector<float>>(result.AsSpan(0, (result.Length / vectorLength) * vectorLength));

            var hasSolo = Layers.Any(l => l.HasAudio && l.IsEnableAudio && l.IsEnableSolo);
            foreach (var l in Layers.Where(l => l.HasAudio && l.IsEnableAudio && (!hasSolo || l.IsEnableSolo)))
            {
                if (!l.IsContainsTimeRange(time, time + length))
                {
                    continue;
                }

                var layerAudio = l.GetAudio(time, length);
                var layerAudioVectorSpan = MemoryMarshal.Cast<float, Vector<float>>(layerAudio.AsSpan(0, (layerAudio.Length / vectorLength) * vectorLength));
                var minVectorLength = Math.Min(resultVectorSpan.Length, layerAudioVectorSpan.Length);
                for (var i = 0; i < minVectorLength; i++)
                {
                    resultVectorSpan[i] += layerAudioVectorSpan[i];
                }
                var minLength = Math.Min(result.Length, layerAudio.Length);
                for (var i = minVectorLength * vectorLength; i < minLength; i++)
                {
                    result[i] += layerAudio[i];
                }
            }

            return result;
        }

        public ColoredPreviewBoundingBox[] GetBoundingBoxes(Guid[] layerIds, Time time)
        {
            var layers = Layers.Where(l => (l.HasImage || l.IsSpecial) && layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf);
            var result = new List<ColoredPreviewBoundingBox>();

            var activeCamera = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
            var activeCameraSetting = GetActiveCameraSetting(time);

            if (Transformer == null)
            {
                Transformer = RendererListModel.CreateTransfomer(RendererPluginId);
                Transformer.SetSize(Width, Height);
            }

            foreach (var layer in layers)
            {
                if (layer.IsCamera)
                {
                    if (layer == activeCamera)
                    {
                        continue;
                    }
                    var cameraSetting = layer.GetCameraSetting(time);
                    if (cameraSetting != null)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Transformer.GetCameraBoundingBox(cameraSetting, activeCameraSetting), layer.TagColor));
                    }
                }
                else if (layer.IsLight)
                {
                    var lightSetting = layer.GetLightSetting(time);
                    if (lightSetting != null)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Transformer.GetLightBoundingBox(lightSetting, activeCameraSetting), layer.TagColor));
                    }
                }
                else
                {
                    var (origin, width, height) = layer.GetSourceFootageRect(time, false);
                    if (layer.IsEnable3D)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Transformer.GetBoundingBox3D(origin, width, height, layer.GetTransform(time), layer.GetParentTransforms(time), activeCameraSetting), layer.TagColor));
                    }
                    else
                    {
                        result.Add(new ColoredPreviewBoundingBox(Transformer.GetBoundingBox2D(origin, width, height, layer.GetTransform(time), layer.GetParentTransforms(time)), layer.TagColor));
                    }
                }
            }

            return [..result];
        }

        public CompositionData SaveData()
        {
            return new CompositionData
            {
                CompositionId = CompositionId,
                Name = Name,
                Width = Width,
                Height = Height,
                FrameRate = FrameRate,
                Duration = Duration,
                IsRetentionFrameRate = IsRetentionFrameRate,
                ApplyToneMappingWhenNested = ApplyToneMappingWhenNested,
                ShutterAngle = ShutterAngle,
                ShutterPhase = ShutterPhase,
                MotionBlurSampleCount = MotionBlurSampleCount,
                WorkareaBegin = WorkareaBegin,
                WorkareaEnd = WorkareaEnd,
                IsEnableShy = IsEnableShy,
                IsEnableFrameBlend = IsEnableFrameBlend,
                IsEnableMotionBlur = IsEnableMotionBlur,
                RendererPluginId = RendererPluginId,
                ToneMapperPluginId = ToneMapperPluginId,
                RendererSetting = Renderer.SaveSetting(),
                ToneMapperSetting = ToneMapper.SaveSetting(),
                TimeBarRange = TimeBarRange,
                TimeBarRangeStart = TimeBarRangeStart,
                CurrentTime = CurrentTime,
                Layers = [..Layers.Select(l => l.SaveData())],
                CompositionMarkers = [..CompositionMarkers.Select(m => new MarkerData { MarkerId = m.MarkerId, Time = m.Time, Name = m.Name })]
            };
        }

        public void LoadData(CompositionData data)
        {
            Name = data.Name;
            Width = data.Width;
            Height = data.Height;
            FrameRate = data.FrameRate;
            Duration = data.Duration.RoundToFrameRate(data.FrameRate);
            IsRetentionFrameRate = data.IsRetentionFrameRate;
            ApplyToneMappingWhenNested = data.ApplyToneMappingWhenNested;
            ShutterAngle = data.ShutterAngle;
            ShutterPhase = data.ShutterPhase;
            MotionBlurSampleCount = data.MotionBlurSampleCount;
            IsEnableShy = data.IsEnableShy;
            IsEnableFrameBlend = data.IsEnableFrameBlend;
            IsEnableMotionBlur = data.IsEnableMotionBlur;
            RendererSetting = data.RendererSetting;
            RendererSettingHash = CalcPluginSettingHash(data.RendererSetting);
            Renderer.LoadSetting(data.RendererSetting);
            ToneMapperSetting = data.ToneMapperSetting;
            ToneMapperSettingHash = CalcPluginSettingHash(data.ToneMapperSetting);
            ToneMapper.LoadSetting(data.ToneMapperSetting);
            WorkareaBegin = data.WorkareaBegin;
            WorkareaEnd = data.WorkareaEnd;
            TimeBarRange = data.TimeBarRange;
            TimeBarRangeStart = data.TimeBarRangeStart;
            CurrentTime = data.CurrentTime;

            foreach (var layerData in data.Layers)
            {
                var footageModels = FootageListModel.GetFootages(layerData.FootageId);
                if (footageModels.Length < 1)
                {
                    continue;
                }

                var layer = new LayerModel(ProjectModel, this, footageModels.First(), EffectListModel, HistoryModel, AcceleratorModel, layerData.LayerId);
                layer.LoadData(layerData, false);
                Layers.Add(layer);
            }

            foreach (var markerData in data.CompositionMarkers)
            {
                CompositionMarkers.Add(new Marker(markerData.MarkerId, markerData.Time, markerData.Name));
            }
        }

        public void CoerceProperties()
        {
            foreach (var layer in Layers)
            {
                if (Layers.All(l => l.LayerId != layer.TrackMatteLayerId))
                {
                    layer.TrackMatteLayerId = null;
                }
                if (Layers.All(l => l.LayerId != layer.ParentLayerId))
                {
                    layer.ParentLayerId = null;
                }
                layer.CoerceProperties();
            }
        }

        public CopyData<LayerData> CutLayers(Guid[] ids)
        {
            var result = CopyLayers(ids);
            DeleteLayersInternal(ids, true);

            return result;
        }

        public CopyData<LayerData> CopyLayers(Guid[] ids)
        {
            var layers = Layers.Where(l => ids.Contains(l.LayerId)).OrderBy(Layers.IndexOf);
            return new CopyData<LayerData>(CopyDataType.Layer, [..layers.Select(l => l.SaveData())]);
        }

        public void PasteLayers(CopyData<LayerData> data, Guid? insertTargetId)
        {
            PasteLayersInternal(data, insertTargetId, false);
        }

        public void PasteEffects(CopyData<EffectData> data, Guid[] layerIds)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteEffects));

            foreach (var l in layers)
            {
                l.PasteEffects(data, [], null);
            }

            HistoryModel.EndGroup();
        }

        public void PasteMasks(CopyData<MaskData> data, Guid[] layerIds)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteMasks));

            foreach (var l in layers)
            {
                l.PasteMasks(data, [], null);
            }

            HistoryModel.EndGroup();
        }

        public void DuplicateLayers(Guid[] ids, Guid? insertTargetId)
        {
            PasteLayersInternal(CopyLayers(ids), insertTargetId, true);
        }

        public void SplitLayers(Guid[] ids, Time splitPositionTime)
        {
            var layers = Layers.Where(l => ids.Contains(l.LayerId))
                .Where(l => l.InPoint < (splitPositionTime - l.SourceStartPoint) && l.OutPoint > (splitPositionTime - l.SourceStartPoint))
                .OrderBy(Layers.IndexOf)
                .ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            ids = [..layers.Select(l => l.LayerId)];

            var data = CopyLayers(ids);
            var addedLayer = new Dictionary<Guid, LayerModel>();
            var newEffectIds = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            var newMaskIds = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            foreach (var layerData in data.Data)
            {
                var footageModels = FootageListModel.GetFootages(layerData.FootageId);
                if (footageModels.Length < 1)
                {
                    continue;
                }

                layerData.InPoint = splitPositionTime - layerData.SourceStartPoint;
                var effectIdMap = new Dictionary<Guid, Guid>();
                var maskIdMap = new Dictionary<Guid, Guid>();
                foreach (var effectData in layerData.Effects)
                {
                    var newId = Guid.NewGuid();
                    effectIdMap.Add(effectData.EffectId, newId);
                    effectData.EffectId = newId;
                }
                foreach (var maskData in layerData.Masks)
                {
                    var newId = Guid.NewGuid();
                    maskIdMap.Add(maskData.MaskId, newId);
                    maskData.MaskId = newId;
                }

                var newLayer = new LayerModel(ProjectModel, this, footageModels.First(), EffectListModel, HistoryModel, AcceleratorModel);
                newLayer.LoadData(layerData, true);
                var index = Layers.FindIndex(l => l.LayerId == layerData.LayerId);
                Layers.Insert(index, newLayer);
                addedLayer.Add(layerData.LayerId, newLayer);

                newEffectIds.Add(newLayer.LayerId, effectIdMap);
                newMaskIds.Add(newLayer.LayerId, maskIdMap);
            }

            var oldOutPoint = new Time[layers.Length];
            var newOutPoint = new Time[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                oldOutPoint[i] = layers[i].OutPoint;
                layers[i].OutPoint = splitPositionTime - layers[i].SourceStartPoint;
                newOutPoint[i] = layers[i].OutPoint;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_SplitLayers));

            foreach (var layer in Layers)
            {
                if (newEffectIds.TryGetValue(layer.LayerId, out var effectIdMap) && newMaskIds.TryGetValue(layer.LayerId, out var maskIdMap))
                {
                    layer.ReplaceLayerDependPropertiesEffectId(effectIdMap);
                    layer.ReplaceLayerDependPropertiesMaskId(maskIdMap);
                }
                layer.UpdateCompositionDependProperties();
                layer.UpdateLayerDependProperties();
                layer.ClearCacheByLayerUpdated();
            }
            HistoryModel.Add(new SplitLayersHistoryCommand(this, layers, addedLayer, oldOutPoint, newOutPoint));

            HistoryModel.EndGroup();
        }

        public void ReplacePlaceholder(FootageModel newFootageModel)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ReplaceFootage));

            foreach (var layer in Layers.Where(l => l.FootageIsPlaceholder(newFootageModel.FootageId)))
            {
                layer.ReplaceFootage(newFootageModel);
            }

            HistoryModel.EndGroup();
        }

        public Guid? FindLayerByPreviewPosition(Time time, Vector2d pos)
        {
            var activeCamera = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
            var activeCameraSetting = activeCamera?.GetCameraSetting(time) ?? CreateDefaultCameraSetting(Width, Height);
            var hasImageSolo = Layers.Any(l => l.HasImage && l.IsEnableVideo && l.IsEnableSolo);
            var layers = Layers.Where(l => l.HasImage && l.IsEnableVideo && !l.IsLock && (!hasImageSolo || l.IsEnableSolo)).Select(l => l.GetLayerSkeleton(time)).NonNull().Reverse().ToArray();

            if (Transformer == null)
            {
                Transformer = RendererListModel.CreateTransfomer(RendererPluginId);
                Transformer.SetSize(Width, Height);
            }

            return Transformer.SelectLayer(activeCameraSetting, layers, pos);
        }

        public LayerSkeleton? GetLayerSkeleton(Guid layerId, Time time)
        {
            return Layers.FirstOrDefault(l => l.LayerId == layerId)?.GetLayerSkeleton(time);
        }

        public Vector2d Projection(CameraSetting cameraSetting, LayerSkeleton? baseLayerSkeleton, Vector3d pos)
        {
            if (Transformer == null)
            {
                Transformer = RendererListModel.CreateTransfomer(RendererPluginId);
                Transformer.SetSize(Width, Height);
            }

            if (baseLayerSkeleton != null)
            {
                return Transformer.LocalCoordToScreenCoord(cameraSetting, baseLayerSkeleton, pos);
            }
            else
            {
                return Transformer.WorldCoordToScreenCoord(cameraSetting, pos);
            }
        }

        public Vector3d Unprojection(CameraSetting cameraSetting, LayerSkeleton? baseLayerSkeleton, Vector2d pos)
        {
            if (Transformer == null)
            {
                Transformer = RendererListModel.CreateTransfomer(RendererPluginId);
                Transformer.SetSize(Width, Height);
            }

            if (baseLayerSkeleton != null)
            {
                return Transformer.ScreenCoordToLocalCoord(cameraSetting, baseLayerSkeleton, pos);
            }
            else
            {
                return Transformer.ScreenCoordToWorldCoord(cameraSetting, pos);
            }
        }

        public LayerModel? GetActiveCamera(Time time)
        {
            return Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
        }

        public CameraSetting GetActiveCameraSetting(Time globalTime)
        {
            var activeCamera = GetActiveCamera(globalTime);
            return activeCamera?.GetCameraSetting(globalTime) ?? CreateDefaultCameraSetting(Width, Height);
        }

        public void AddEffectsToLayers(Guid[] layerIds, Guid[] effectUuids)
        {
            if (layerIds.Length < 1 || effectUuids.Length < 1)
            {
                return;
            }

            if (layerIds.Length == 1)
            {
                var layerId = layerIds[0];
                var layer = Layers.FirstOrDefault(l => l.LayerId == layerId);
                layer?.AddEffects(effectUuids);
            }
            else
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddEffects));

                foreach (var l in Layers.Where(l => layerIds.Contains(l.LayerId)))
                {
                    l.AddEffects(effectUuids);
                }

                HistoryModel.EndGroup();
            }
        }

        public void AddShapedMaskToLayers(Guid[] layerIds, MaskShapeType shapeType)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            if (layerIds.Length == 1)
            {
                var layerId = layerIds[0];
                var layer = Layers.FirstOrDefault(l => l.LayerId == layerId);
                layer?.AddShapedMask(shapeType);
            }
            else
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddMask));

                foreach (var l in Layers.Where(l => layerIds.Contains(l.LayerId)))
                {
                    l.AddShapedMask(shapeType);
                }

                HistoryModel.EndGroup();
            }
        }

        public void AddBezierMaskToLayers(Guid[] layerIds)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            if (layerIds.Length == 1)
            {
                var layerId = layerIds[0];
                var layer = Layers.FirstOrDefault(l => l.LayerId == layerId);
                layer?.AddBezierMask();
            }
            else
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddMask));

                foreach (var l in Layers.Where(l => layerIds.Contains(l.LayerId)))
                {
                    l.AddBezierMask();
                }

                HistoryModel.EndGroup();
            }
        }

        public void ChangeLayerTagsRandomly(Guid[] layerIds)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeTagColor));

            var rand = Random.Shared;
            var prevColor = (Lab?)null;
            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                while (true)
                {
                    var newColor = new Hsv(rand.NextSingle() * 360.0F, rand.NextSingle() * 0.2F + 0.5F, rand.NextSingle() * 0.5F + 0.5F).ToRgb();
                    var newLab = Lab.FromRgb(newColor);

                    if (prevColor == null || newLab.Distance(prevColor.Value) >= 3.0F)
                    {
                        layer.ChangeTagColor(newColor.ToColor());
                        prevColor = newLab;
                        break;
                    }
                }
            }

            HistoryModel.EndGroup();
        }

        public void MoveLayerInPoint(Guid[] layerIds, Time targetTime)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                var newInPoint = (targetTime - layer.SourceStartPoint).RoundToFrameRate(FrameRate);
                if (layer.IsSpecial || layer.IsEnableTimeRemap || layer.IsFreezeFrame || layer.IsImage)
                {
                    newInPoint = Time.Min(newInPoint, layer.OutPoint - FrameDuration);
                }
                else
                {
                    newInPoint = Time.Min(Time.Max(newInPoint, Time.Zero), layer.OutPoint - FrameDuration);
                }
                if (layer.InPoint != newInPoint)
                {
                    layer.CommitEditDuration(layer.InPoint, newInPoint, layer.OutPoint, layer.OutPoint, layer.SourceStartPoint, layer.SourceStartPoint);
                }
            }

            HistoryModel.EndGroup();
        }

        public void MoveLayerOutPoint(Guid[] layerIds, Time targetTime)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                var newOutPoint = (targetTime - layer.SourceStartPoint + FrameDuration).RoundToFrameRate(FrameRate);
                if (layer.IsSpecial || layer.IsEnableTimeRemap || layer.IsFreezeFrame || layer.IsImage)
                {
                    newOutPoint = Time.Max(newOutPoint, layer.InPoint + FrameDuration);
                }
                else
                {
                    newOutPoint = Time.Max(Time.Min(newOutPoint, layer.Duration), layer.InPoint + FrameDuration);
                }
                if (layer.OutPoint != newOutPoint)
                {
                    layer.CommitEditDuration(layer.InPoint, layer.InPoint, layer.OutPoint, newOutPoint, layer.SourceStartPoint, layer.SourceStartPoint);
                }
            }

            HistoryModel.EndGroup();
        }

        public void MoveLayerSourceStartPointToInPoint(Guid[] layerIds, Time targetTime)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                var newSourceStartPoint = (targetTime - layer.InPoint).RoundToFrameRate(FrameRate);
                if (layer.SourceStartPoint != newSourceStartPoint)
                {
                    layer.CommitEditDuration(layer.InPoint, layer.InPoint, layer.OutPoint, layer.OutPoint, layer.SourceStartPoint, newSourceStartPoint);
                }
            }

            HistoryModel.EndGroup();
        }

        public void MoveLayerSourceStartPointToOutPoint(Guid[] layerIds, Time targetTime)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                var newSourceStartPoint = (targetTime - layer.OutPoint + FrameDuration).RoundToFrameRate(FrameRate);
                if (layer.SourceStartPoint != newSourceStartPoint)
                {
                    layer.CommitEditDuration(layer.InPoint, layer.InPoint, layer.OutPoint, layer.OutPoint, layer.SourceStartPoint, newSourceStartPoint);
                }
            }

            HistoryModel.EndGroup();
        }

        public void MoveLayerSourceStartPoint(Guid[] layerIds, Time diff)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                var newSourceStartPoint = (layer.SourceStartPoint + diff).RoundToFrameRate(FrameRate);
                if (layer.SourceStartPoint != newSourceStartPoint)
                {
                    layer.CommitEditDuration(layer.InPoint, layer.InPoint, layer.OutPoint, layer.OutPoint, layer.SourceStartPoint, newSourceStartPoint);
                }
            }

            HistoryModel.EndGroup();
        }

        public void ChangeLayerPlayRate(Guid[] layerIds, double newRate)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeLayerPlayRate));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                layer.ChangePlayRate(newRate, FrameRate);
            }

            HistoryModel.EndGroup();
        }

        public void ChangeFreezeFrame(Guid[] layerIds, bool isFreezeFrame, Time time)
        {
            if (layerIds.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeFreezeFrame));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                layer.ChangeFreezeFrame(isFreezeFrame, time, FrameDuration);
            }

            HistoryModel.EndGroup();
        }

        public void GenerateAudioLevelValueKeyFrame(Guid[] layerIds, AudioLevelValueType type)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_GenerateAudioLevelValueKeyFrame));

            foreach (var layer in Layers.Where(l => layerIds.Contains(l.LayerId)))
            {
                layer.GenerateAudioLevelValueKeyFrames(type);
            }

            HistoryModel.EndGroup();
        }

        public void ChangeTextStyle(Guid[] layerIds, Guid? targetLayerId, object? targetLayerPrevValue)
        {
            var layers = Layers.Where(l => l.IsText && layerIds.Contains(l.LayerId)).ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue));

            foreach (var l in layers)
            {
                if (l.LayerId == targetLayerId)
                {
                    TextPropertyModel.UpdateTextProperty(l, CurrentTime - l.SourceStartPoint, targetLayerPrevValue);
                }
                else
                {
                    TextPropertyModel.UpdateTextProperty(l, CurrentTime - l.SourceStartPoint, null);
                }
            }

            HistoryModel.EndGroup();
        }

        public void LoadEffectPreset(Guid[] layerIds, string filePath)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_LoadEffectPreset));

            foreach (var l in layers)
            {
                l.LoadEffectPreset(filePath, [], null);
            }

            HistoryModel.EndGroup();
        }

        public void LoadMaskPreset(Guid[] layerIds, string filePath)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_LoadMaskPreset));

            foreach (var l in layers)
            {
                l.LoadMaskPreset(filePath, [], null);
            }

            HistoryModel.EndGroup();
        }

        public void MoveMarker(Marker marker, Time newTime)
        {
            var oldMarkerIndex = CompositionMarkers.FindIndex(m => m.MarkerId == marker.MarkerId);
            if (oldMarkerIndex < 0)
            {
                return;
            }

            var oldMarker = CompositionMarkers[oldMarkerIndex];
            var newMarker = new Marker(marker.MarkerId, newTime, marker.Name);

            var sameTimeMarkerIndex = CompositionMarkers.FindIndex(m => m.Time == newTime);
            if (sameTimeMarkerIndex < 0)
            {
                CompositionMarkers[oldMarkerIndex] = newMarker;
                CompositionMarkers.Sort((a, b) => a.Time.CompareTo(b.Time));

                HistoryModel.Add(new MoveMarkerHistoryCommand(this, oldMarker, newMarker));
            }
            else
            {
                var replaceTargetMarker = CompositionMarkers[sameTimeMarkerIndex];
                CompositionMarkers[sameTimeMarkerIndex] = newMarker;
                CompositionMarkers.Remove(oldMarker);

                HistoryModel.Add(new MoveAndReplaceMarkerHistoryCommand(this, oldMarker, newMarker, replaceTargetMarker, oldMarkerIndex, sameTimeMarkerIndex));
            }
        }

        public void AddMarker(Time time)
        {
            var newMarker = new Marker(Guid.NewGuid(), time, "");

            var insertIndex = CompositionMarkers.FindIndex(m => m.Time > time);
            if (insertIndex < 0)
            {
                insertIndex = CompositionMarkers.Count;
            }
            CompositionMarkers.Insert(insertIndex, newMarker);

            HistoryModel.Add(new AddMarkerHistoryCommand(this, newMarker, insertIndex));
        }

        public void DeleteMarker(Marker marker)
        {
            var index = CompositionMarkers.FindIndex(m => m.MarkerId == marker.MarkerId);
            if (index < 0)
            {
                return;
            }

            var oldMarker = CompositionMarkers[index];
            CompositionMarkers.Remove(marker);

            HistoryModel.Add(new DeleteMarkerHistoryCommand(this, oldMarker, index));
        }

        public void ChangeMarkerName(Marker marker, string newName)
        {
            var index = CompositionMarkers.IndexOf(marker);
            if (index < 0)
            {
                return;
            }

            var oldMarker = CompositionMarkers[index];
            var newMarker = new Marker(oldMarker.MarkerId, oldMarker.Time, newName);

            CompositionMarkers[index] = newMarker;

            HistoryModel.Add(new ChangeMarkerNameHistoryCommand(this, oldMarker, newMarker, index));
        }

        public void Precompose(Guid[] layerIds, string compositionName, bool isMoveAll, bool alignDuration, bool copyParent, Guid? insertTargetId)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).ToArray();

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_Precompose));

            if (layers.Length == 1 && !isMoveAll)
            {
                var compositionData = SaveTargetLayerOnly(layers);
                compositionData.Name = compositionName;
                if (layers[0].IsVideo || layers[0].HasAudio)
                {
                    compositionData.Duration = layers[0].Duration;
                    compositionData.Layers[0].SourceStartPoint = Time.Zero;
                    compositionData.Layers[0].InPoint = Time.Zero;
                    compositionData.Layers[0].OutPoint = layers[0].Duration;
                    compositionData.WorkareaBegin = Time.Zero;
                    compositionData.WorkareaEnd = layers[0].Duration;
                }
                else
                {
                    compositionData.Layers[0].SourceStartPoint = Time.Zero;
                    compositionData.Layers[0].InPoint = Time.Zero;
                    compositionData.Layers[0].OutPoint = compositionData.Duration;
                    compositionData.WorkareaBegin = Time.Zero;
                    compositionData.WorkareaEnd = compositionData.Duration;
                }
                compositionData.TimeBarRangeStart = Time.Zero;
                compositionData.TimeBarRange = compositionData.Duration;

                if (layers[0].FootageWidth != 0)
                {
                    compositionData.Width = layers[0].FootageWidth;
                    compositionData.Height = layers[0].FootageHeight;
                }
                var compositionFootage = ProjectModel.CreateCompositionFromData(compositionData);
                if (compositionFootage != null)
                {
                    (compositionFootage.InputModel.Input as CompositionInput)?.Composition?.RemoveAllLayerAttribute();
                    layers[0].ReplaceFootage(compositionFootage);
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }
            else
            {
                CompositionData compositionData;
                if (copyParent)
                {
                    var parents = new List<LayerModel>();
                    var parentQueue = new Queue<Guid>(layers.Select(l => l.ParentLayerId).NonNull());
                    while (parentQueue.Count > 0)
                    {
                        var parentId = parentQueue.Dequeue();
                        var parent = Layers.FirstOrDefault(l => l.LayerId == parentId);
                        if (parent != null && !parents.Contains(parent))
                        {
                            parents.Add(parent);
                            if (parent.ParentLayerId.HasValue)
                            {
                                parentQueue.Enqueue(parent.ParentLayerId.Value);
                            }
                        }
                    }

                    var combinedLayers = layers.Concat(parents).OrderBy(Layers.IndexOf).ToArray();
                    compositionData = SaveTargetLayerOnly(combinedLayers);
                    foreach (var layerData in compositionData.Layers.Where(ld => parents.Any(p => p.LayerId == ld.LayerId)))
                    {
                        layerData.IsEnableVideo = false;
                        layerData.IsEnableAudio = false;
                    }
                }
                else
                {
                    compositionData = SaveTargetLayerOnly(layers);
                }
                compositionData.Name = compositionName;
                compositionData.WorkareaBegin = Time.Zero;

                var sourceStartPoint = Time.Zero;
                if (alignDuration)
                {
                    sourceStartPoint = layers.Min(l => l.SourceStartPoint + l.InPoint);
                    var duration = layers.Max(l => l.SourceStartPoint + l.OutPoint) - sourceStartPoint;
                    compositionData.Duration = duration;
                    compositionData.WorkareaEnd = duration;

                    for (var i = 0; i < compositionData.Layers.Length; i++)
                    {
                        compositionData.Layers[i].SourceStartPoint -= sourceStartPoint;
                    }
                }
                else
                {
                    compositionData.WorkareaEnd = compositionData.Duration;
                }
                compositionData.TimeBarRangeStart = Time.Zero;
                compositionData.TimeBarRange = compositionData.Duration;

                var compositionFootage = ProjectModel.CreateCompositionFromData(compositionData);
                if (compositionFootage == null)
                {
                    HistoryModel.AbortGroup();
                    return;
                }

                var insertTargetIndex = Layers.FindIndex(l => l.LayerId == insertTargetId);
                DeleteLayers(layerIds);
                insertTargetIndex = Math.Clamp(insertTargetIndex, 0, Layers.Count);
                InsertLayers(compositionFootage.FootageId, insertTargetIndex, sourceStartPoint);
            }

            HistoryModel.EndGroup();
        }

        public ILayerObject? GetLayer(Guid layerId)
        {
            return Layers.FirstOrDefault(l => l.LayerId == layerId);
        }

        public bool LayerIsChild(Guid layerId)
        {
            return Layers.Any(l => l.LayerId == layerId);
        }

        public void UpdatePropertyForImport(Dictionary<Guid, Guid> layerIdMap, Dictionary<Guid, Dictionary<Guid, Guid>> effectIdMaps, Dictionary<Guid, Dictionary<Guid, Guid>> maskIdMaps)
        {
            foreach (var layer in Layers)
            {
                if (layer.TrackMatteLayerId.HasValue)
                {
                    if (layerIdMap.TryGetValue(layer.TrackMatteLayerId.Value, out var newTrackMatteLayerId))
                    {
                        layer.TrackMatteLayerId = newTrackMatteLayerId;
                    }
                    else
                    {
                        layer.TrackMatteLayerId = null;
                    }
                }
                if (layer.ParentLayerId.HasValue)
                {
                    if (layerIdMap.TryGetValue(layer.ParentLayerId.Value, out var newParentLayerId))
                    {
                        layer.ParentLayerId = newParentLayerId;
                    }
                    else
                    {
                        layer.ParentLayerId = null;
                    }
                }

                layer.ReplaceLayerDependPropertiesEffectId(effectIdMaps[layer.LayerId]);
                layer.ReplaceLayerDependPropertiesMaskId(maskIdMaps[layer.LayerId]);
                layer.ReplaceCompositionDependPropertiesLayerId(layerIdMap);
                layer.UpdateCompositionDependProperties();
                layer.UpdateLayerDependProperties();
            }
        }

        public ICoordTransformerObject GetCoordTransformer(Time time, Guid layerId)
        {
            var layer = Layers.First(l => l.LayerId == layerId);
            if (Transformer == null)
            {
                Transformer = RendererListModel.CreateTransfomer(RendererPluginId);
                Transformer.SetSize(Width, Height);
            }

            return new CoordTransformerWrapper(Transformer, this, layer, CurrentTime);
        }

        NImage RenderFrameInternal(Time time, Time shutterTime, bool isSubFrame, double downSamplingRate, bool applyToneMapping, bool useGpu)
        {
            var hasLightSolo = Layers.Any(l => l.IsLight && l.IsEnableVideo && l.IsEnableSolo);
            var useLights = Layers.Where(l => l.IsEnableVideo && (!hasLightSolo || l.IsEnableSolo)).Select(l => l.GetLightSetting(time)).NonNull().ToArray();

            var hasImageSolo = Layers.Any(l => l.HasImage && l.IsEnableVideo && l.IsEnableSolo);
            var useLayers = Layers.Where(l => l.HasImage && l.IsEnableVideo && (!hasImageSolo || l.IsEnableSolo)).Reverse().ToArray();
            var subFrameTime = Time.Max(time + shutterTime, Time.Zero);

            var cameraSetting = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(subFrameTime))?.GetCameraSetting(subFrameTime);

            var hash = new XxHash3();
            if (downSamplingRate == 1.0)
            {
                CalcCacheHash(hash, time, shutterTime, isSubFrame);
            }

            NImage result;
            var cacheKey = hash.ToInt128();
            var device = useGpu ? AcceleratorModel.CurrentDevice : null;
            if (downSamplingRate != 1.0 || !ImageCache.TryGet(CompositionId, cacheKey, IsEnableMotionBlur ? subFrameTime : time, device, out var cachedImage))
            {
                var allImages = new List<IDisposable>();
                try
                {
                    Renderer.BeginRendering(downSamplingRate, useGpu);

                    if (cameraSetting != null)
                    {
                        Renderer.SetCamera(cameraSetting);
                    }
                    else
                    {
                        Renderer.SetCamera(CreateDefaultCameraSetting(Width, Height));
                    }

                    foreach (var light in useLights)
                    {
                        Renderer.AddLight(light);
                    }

                    var images = new List<RenderableImage>();
                    var rawImages = new List<(LayerModel, RenderableImage)>();
                    foreach (var l in useLayers)
                    {
                        var currentTime = IsEnableMotionBlur && l.IsEnableMotionBlur ? subFrameTime : time;
                        if (!l.IsContainsTime(currentTime))
                        {
                            continue;
                        }

                        if (l.IsEnableAdjustmentLayer)
                        {
                            if (images.Count > 0)
                            {
                                Renderer.Render([.. images]);
                            }
                            images.Clear();

                            using var adjustmentMaskImage = l.GetRawImage(currentTime, FrameDuration, downSamplingRate, true, useGpu, IsEnableFrameBlend);
                            if (adjustmentMaskImage == null)
                            {
                                continue;
                            }

                            using var mask = Renderer.RenderAdjustmentMask(adjustmentMaskImage);
                            var currentRenderingFrame = Renderer.GetCurrentRenderedImage();
                            var (roi, currentFrame) = l.ProcessAdjustment(currentTime, currentRenderingFrame, Width / (double)currentRenderingFrame.Width, Height / (double)currentRenderingFrame.Height, useGpu);

                            var maskedImage = (NImage)(useGpu ? ApplyMaskGpu(currentFrame, mask, roi) : ApplyMaskCpu(currentFrame, mask, roi));
                            if (maskedImage != currentFrame)
                            {
                                currentFrame.Dispose();
                                currentFrame = maskedImage;
                            }

                            Renderer.RenderAdjustmentLayer(currentFrame, roi, downSamplingRate, l.InterpolationQuality, l.BlendMode);

                            allImages.Add(currentFrame);
                        }
                        else
                        {
                            var isRawImage = l.IsImage && !l.IsCustomizableFootageSource && !l.HasNonDummyEffect;

                            var (prevLayer, rawImage) = isRawImage ? rawImages.FirstOrDefault(t => l.IsSameFootage(t.Item1)) : (null, null);
                            var image = (prevLayer != null && rawImage != null ? l.GetSameImage(currentTime, FrameDuration, downSamplingRate, true, useGpu, IsEnableFrameBlend, rawImage) : null) ?? l.GetImage(currentTime, FrameDuration, downSamplingRate, true, useGpu, IsEnableFrameBlend);
                            if (image != null)
                            {
                                images.Add(image);
                                allImages.Add(image);
                                if (isRawImage && prevLayer == null)
                                {
                                    rawImages.Add((l, image));
                                }
                            }
                        }
                    }
                    if (images.Count > 0)
                    {
                        Renderer.Render([.. images]);
                    }
                }
                catch (Exception ex)
                {
                    Renderer.AbortRendering();
                    try
                    {
                        foreach (var i in allImages)
                        {
                            i.Dispose();
                        }
                    }
                    catch { }
                    if (ex is not GPUException && useGpu)
                    {
                        throw new GPUException(ex);
                    }
                    else
                    {
                        throw;
                    }
                }

                try
                {
                    result = Renderer.FinishRendering();
                }
                catch (Exception ex)
                {
                    Renderer.AbortRendering();
                    if (useGpu)
                    {
                        throw new GPUException(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    try
                    {
                        foreach (var i in allImages)
                        {
                            i.Dispose();
                        }
                    }
                    catch { }
                }

                if (downSamplingRate == 1.0)
                {
                    ImageCache.Add(CompositionId, cacheKey, time, result, ROI.Empty);
                }
            }
            else
            {
                result = cachedImage.Item1;
            }

            if (applyToneMapping)
            {
                try
                {
                    var toneMapped = ToneMapper.ToneMapping(result, useGpu);
                    if (result != toneMapped)
                    {
                        result.Dispose();
                        result = toneMapped;
                    }
                }
                catch (Exception ex)
                {
                    if (useGpu)
                    {
                        throw new GPUException(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        bool CheckCycledSimulatedParentLayer(Guid layerId, Dictionary<Guid, Guid?> changed, HashSet<Guid>? checkedLayerIds = null)
        {
            checkedLayerIds ??= [];

            if (checkedLayerIds.Contains(layerId))
            {
                return true;
            }

            checkedLayerIds.Add(layerId);

            var layer = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (layer == null)
            {
                return false;
            }

            var parentLayerId = changed.TryGetValue(layerId, out var value) ? value : layer.ParentLayerId;
            if (!parentLayerId.HasValue)
            {
                return false;
            }

            return CheckCycledSimulatedParentLayer(parentLayerId.Value, changed, checkedLayerIds);
        }

        void InsertLayerInternal(LayerModel[] layers, int insertIndex)
        {
            var index = insertIndex;
            foreach (var layer in layers)
            {
                Layers.Insert(index, layer);
                index++;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddLayers));

            foreach (var layer in layers)
            {
                layer.UpdateCompositionDependProperties();
                layer.ClearCacheByLayerUpdated();
            }
            HistoryModel.Add(new AddLayersHistoryCommand(this, layers, insertIndex));

            HistoryModel.EndGroup();
        }

        void DeleteLayersInternal(Guid[] ids, bool isCut)
        {
            var removeLayers = Layers.Where(l => ids.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldIndices = removeLayers.Select(l => Layers.IndexOf(l)).ToArray();

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveLayers));

            var childLayers = removeLayers.SelectMany(p => Layers.Where(c => c.ParentLayerId == p.LayerId)).Select(l => l.LayerId).ToArray();
            if (childLayers.Length > 0)
            {
                ChangeParentLayer(childLayers, null);
            }

            var trackMatteChildLayers = removeLayers.SelectMany(p => Layers.Where(c => c.TrackMatteLayerId == p.LayerId)).Select(l => l.LayerId).ToArray();
            if (trackMatteChildLayers.Length > 0)
            {
                ChangeTrackMatteLayers(trackMatteChildLayers, null);
            }

            foreach (var l in removeLayers)
            {
                Layers.Remove(l);
            }

            foreach (var layer in Layers)
            {
                layer.UpdateCompositionDependProperties();
                layer.ClearCacheByLayerUpdated();
            }
            HistoryModel.Add(new DeleteLayersHistoryCommand(this, removeLayers, oldIndices, isCut));

            HistoryModel.EndGroup();
        }

        void PasteLayersInternal(CopyData<LayerData> data, Guid? insertTargetId, bool isDuplicate)
        {
            if (data.Type != CopyDataType.Layer || data.Data.Length < 1)
            {
                return;
            }

            var addedLayer = new List<LayerModel>();
            var insertStartIndex = insertTargetId.HasValue ? Layers.FindIndex(l => l.LayerId == insertTargetId) : -1;
            if (insertStartIndex < 0)
            {
                insertStartIndex = Layers.Count;
            }
            else
            {
                insertStartIndex++;
            }

            var index = insertStartIndex;
            var newLayerIds = new Dictionary<Guid, Guid>();
            var newEffectIds = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            var newMaskIds = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            foreach (var layerData in data.Data)
            {
                var footageModels = FootageListModel.GetFootages(layerData.FootageId);
                if (footageModels.Length < 1)
                {
                    continue;
                }

                var effectIdMap = new Dictionary<Guid, Guid>();
                var maskIdMap = new Dictionary<Guid, Guid>();
                foreach (var effectData in layerData.Effects)
                {
                    var newId = Guid.NewGuid();
                    effectIdMap.Add(effectData.EffectId, newId);
                    effectData.EffectId = newId;
                }
                foreach (var maskData in layerData.Masks)
                {
                    var newId = Guid.NewGuid();
                    maskIdMap.Add(maskData.MaskId, newId);
                    maskData.MaskId = newId;
                }

                var newLayer = new LayerModel(ProjectModel, this, footageModels.First(), EffectListModel, HistoryModel, AcceleratorModel);
                newLayer.LoadData(layerData, true);
                Layers.Insert(index, newLayer);
                addedLayer.Add(newLayer);
                index++;

                newLayerIds.Add(layerData.LayerId, newLayer.LayerId);
                newEffectIds.Add(newLayer.LayerId, effectIdMap);
                newMaskIds.Add(newLayer.LayerId, maskIdMap);
            }

            foreach (var layer in addedLayer)
            {
                if (layer.TrackMatteLayerId.HasValue && Layers.All(l => l.LayerId != layer.TrackMatteLayerId))
                {
                    if (newLayerIds.TryGetValue(layer.TrackMatteLayerId.Value, out var newTrackMatteLayerId))
                    {
                        layer.TrackMatteLayerId = newTrackMatteLayerId;
                    }
                    else
                    {
                        layer.TrackMatteLayerId = null;
                    }
                }
                if (layer.ParentLayerId.HasValue && Layers.All(l => l.LayerId != layer.ParentLayerId))
                {
                    if (newLayerIds.TryGetValue(layer.ParentLayerId.Value, out var newParentLayerId))
                    {
                        layer.ParentLayerId = newParentLayerId;
                    }
                    else
                    {
                        layer.ParentLayerId = null;
                    }
                }
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(isDuplicate ? LanguageResourceDictionary.History_DuplicateLayers : LanguageResourceDictionary.History_PasteLayers));

            foreach (var layer in Layers)
            {
                if (newEffectIds.ContainsKey(layer.LayerId))
                {
                    layer.ReplaceLayerDependPropertiesEffectId(newEffectIds[layer.LayerId]);
                    layer.ReplaceLayerDependPropertiesMaskId(newMaskIds[layer.LayerId]);
                    layer.ReplaceCompositionDependPropertiesLayerId(newLayerIds);
                }
                layer.UpdateCompositionDependProperties();
                layer.UpdateLayerDependProperties();
                layer.ClearCacheByLayerUpdated();
            }
            HistoryModel.Add(new PasteLayersHistoryCommand(this, [.. addedLayer], insertStartIndex, isDuplicate));

            HistoryModel.EndGroup();
        }

        void CalcCacheHash(XxHash3 hash, Time time, Time shutterTime, bool isSubFrame)
        {
            if (IsEnableMotionBlur)
            {
                time = Time.Max(time + shutterTime, Time.Zero);
                hash.Append(isSubFrame);
            }

            var cameraSetting = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time))?.GetCameraSetting(time);

            var hasLightSolo = Layers.Any(l => l.IsLight && l.IsEnableVideo && l.IsEnableSolo);
            var useLights = Layers.Where(l => l.IsEnableVideo && (!hasLightSolo || l.IsEnableSolo)).Select(l => l.GetLightSetting(time)).NonNull().ToArray();

            var hasImageSolo = Layers.Any(l => l.HasImage && l.IsEnableVideo && l.IsEnableSolo);
            var useLayers = Layers.Where(l => l.HasImage && l.IsEnableVideo && (!hasImageSolo || l.IsEnableSolo)).Reverse().ToArray();

            hash.Append(Width);
            hash.Append(Height);
            hash.Append(FrameRate);
            hash.Append(ShutterAngle);
            hash.Append(ShutterPhase);
            hash.Append(MotionBlurSampleCount);
            hash.Append(IsEnableFrameBlend);
            hash.Append(IsEnableMotionBlur);
            hash.Append(RendererPluginId);
            hash.Append(ToneMapperPluginId);
            hash.Append(RendererSettingHash);
            hash.Append(ToneMapperSettingHash);

            hash.Append(time);

            if (cameraSetting != null)
            {
                hash.Append(cameraSetting.PointOfInterest);
                hash.Append(cameraSetting.Position);
                hash.Append(cameraSetting.Orientation);
                hash.Append(cameraSetting.AngleX);
                hash.Append(cameraSetting.AngleY);
                hash.Append(cameraSetting.AngleZ);
                hash.Append(cameraSetting.Zoom);
                foreach (var pt in cameraSetting.ParentTransforms)
                {
                    hash.Append(pt.ParentType);
                    pt.Transform.CalcHash(hash);
                }
            }
            foreach (var light in useLights)
            {
                hash.Append(light.LightType);
                hash.Append(light.PointOfInterest);
                hash.Append(light.Position);
                hash.Append(light.Orientation);
                hash.Append(light.AngleX);
                hash.Append(light.AngleY);
                hash.Append(light.AngleZ);
                hash.Append(light.Color);
                hash.Append(light.Intensity);
                hash.Append(light.ConeAngle);
                hash.Append(light.ConeAttenuation);
                hash.Append(light.FalloffType);
                hash.Append(light.FalloffStart);
                hash.Append(light.FalloffLength);
                hash.Append(light.IsEnableShadow);
                hash.Append(light.ShadowStrength);
                hash.Append(light.ShadowScatterSize);
                foreach (var pt in light.ParentTransforms)
                {
                    hash.Append(pt.ParentType);
                    pt.Transform.CalcHash(hash);
                }
            }
            foreach (var layer in useLayers)
            {
                layer.CalcCacheKeyHash(hash, time, true, IsEnableFrameBlend);
            }
        }

        void OnCompositionUpdated(bool needHistoryChange)
        {
            ImageCache.Clear(CompositionId);
            CompositionUpdatedPublisher.Publish(this, new NeedHistoryChangeEventArgs(needHistoryChange));
        }

        NManagedImage ApplyMaskCpu(NImage image, RasterizedMaskImage mask, ROI roi)
        {
            var managedMask = mask.ToManaged();
            var managedImage = image.ToManaged();

            Parallel.For(roi.OriginalImagePosition.Y, roi.OriginalImagePosition.Y + roi.OriginalImageSize.Height, y =>
            {
                var maskSpan = managedMask.GetDataSpan().Slice((y - roi.OriginalImagePosition.Y) * managedMask.Width, managedMask.Width);
                var currentFrameSpan = managedImage.GetDataSpan().Slice(y * managedImage.Width, managedImage.Width);
                for (int x = roi.OriginalImagePosition.X, limit = x + roi.OriginalImageSize.Width, maskPos = 0, framePos = x; x < limit; x++, maskPos++, framePos++)
                {
                    currentFrameSpan[framePos].W *= maskSpan[maskPos];
                }
            });

            if (mask != managedMask)
            {
                managedImage.Dispose();
            }

            return managedImage;
        }

        NGPUImage ApplyMaskGpu(NImage image, RasterizedMaskImage mask, ROI roi)
        {
            var device = AcceleratorModel.CurrentDevice;
            var gpuMask = mask.ToGpu(device);
            var gpuImage = image.ToGpu(device);

            device.For(
                roi.OriginalImagePosition.X + roi.OriginalImageSize.Width,
                roi.OriginalImagePosition.Y + roi.OriginalImageSize.Height,
                new MaskImage(
                    gpuImage.Data,
                    gpuImage.Width,
                    gpuMask.Data,
                    gpuMask.Width,
                    roi.OriginalImagePosition.X,
                    roi.OriginalImagePosition.Y
                )
            );

            if (mask != gpuMask)
            {
                gpuMask.Dispose();
            }

            return gpuImage;
        }

        CompositionData SaveTargetLayerOnly(LayerModel[] layers)
        {
            return new CompositionData
            {
                CompositionId = CompositionId,
                Name = Name,
                Width = Width,
                Height = Height,
                FrameRate = FrameRate,
                Duration = Duration,
                IsRetentionFrameRate = IsRetentionFrameRate,
                ApplyToneMappingWhenNested = ApplyToneMappingWhenNested,
                ShutterAngle = ShutterAngle,
                ShutterPhase = ShutterPhase,
                MotionBlurSampleCount = MotionBlurSampleCount,
                WorkareaBegin = WorkareaBegin,
                WorkareaEnd = WorkareaEnd,
                IsEnableShy = IsEnableShy,
                IsEnableFrameBlend = IsEnableFrameBlend,
                IsEnableMotionBlur = IsEnableMotionBlur,
                RendererPluginId = RendererPluginId,
                ToneMapperPluginId = ToneMapperPluginId,
                RendererSetting = Renderer.SaveSetting(),
                ToneMapperSetting = ToneMapper.SaveSetting(),
                TimeBarRange = TimeBarRange,
                TimeBarRangeStart = TimeBarRangeStart,
                CurrentTime = CurrentTime,
                Layers = [..layers.Select(l => l.SaveData())],
                CompositionMarkers = [..CompositionMarkers.Select(m => new MarkerData { MarkerId = m.MarkerId, Time = m.Time, Name = m.Name })]
            };
        }

        void RemoveAllLayerAttribute()
        {
            foreach (var layer in Layers)
            {
                layer.TransformProperties?.ClearAllChildren();
                layer.AudioOptionProperties?.ClearAllChildren();
                layer.DeleteMask([..layer.Masks.Select(m => m.MaskId)]);
                layer.DeleteEffect([..layer.Effects.Select(e => e.EffectId)]);
            }
        }

        public static CompositionDataImportConvertionResult ConvertDataForImport(CompositionData compositionData, Dictionary<Guid, Guid> footageIdMap)
        {
            var oldId = compositionData.CompositionId;
            compositionData.CompositionId = Guid.NewGuid();

            var layerIdMap = new Dictionary<Guid, Guid>();
            var effectIdMaps = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            var maskIdMaps = new Dictionary<Guid, Dictionary<Guid, Guid>>();
            foreach (var l in compositionData.Layers)
            {
                var (oldLayerId, newLayerId, effectIdMap, maskIdMap) = LayerModel.ConvertDataForImport(l, footageIdMap);
                layerIdMap.Add(oldLayerId, newLayerId);
                effectIdMaps.Add(newLayerId, effectIdMap);
                maskIdMaps.Add(newLayerId, maskIdMap);
            }
            foreach (var m in compositionData.CompositionMarkers)
            {
                m.MarkerId = Guid.NewGuid();
            }

            return new CompositionDataImportConvertionResult(oldId, compositionData.CompositionId, layerIdMap, effectIdMaps, maskIdMaps);
        }

        static CameraSetting CreateDefaultCameraSetting(int width, int height)
        {
            var zoom = width / Const.DefaultCameraFov * 0.5;

            return new CameraSetting(
                new Vector3d(width * 0.5, height * 0.5, 0),
                new Vector3d(width * 0.5, height * 0.5, -zoom),
                new Vector3d(),
                0.0, 0.0, 0.0,
                zoom,
                []
            );
        }

        static bool IsCycledComposition(CompositionModel target, CompositionModel input)
        {
            if (input == target)
            {
                return true;
            }
            foreach (var i in input.Layers.Select(l => l.GetNestedComposition()).NonNull())
            {
                if (IsCycledComposition(target, i))
                {
                    return true;
                }
            }
            return false;
        }

        static Int128 CalcPluginSettingHash(object? pluginSetting)
        {
            if (pluginSetting == null)
            {
                return 0;
            }
            else
            {
                var hash = new XxHash3();
                hash.Append(JsonSerializer.Serialize(pluginSetting));
                return hash.ToInt128();
            }
        }

        private void CompositionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (IsSettingChanging)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(FrameRate):
                    FrameDuration = new Time(1, FrameRate);
                    break;
                case nameof(Duration):
                    if (TimeBarRange < Duration)
                    {
                        TimeBarRange = Duration;
                    }
                    if (TimeBarRangeStart + TimeBarRange > Duration)
                    {
                        TimeBarRangeStart = Time.Max(Duration - TimeBarRangeStart, Time.Zero);
                    }
                    WorkareaBegin = Time.Zero;
                    WorkareaEnd = Duration;
                    break;
                case nameof(Width):
                case nameof(Height):
                    if (Width > 0 && Height > 0)
                    {
                        Renderer.SetSize(Width, Height);
                    }
                    break;
            }

            if (e.PropertyName != nameof(TimeBarRange) &&
                e.PropertyName != nameof(TimeBarRangeStart) &&
                e.PropertyName != nameof(WorkareaBegin) &&
                e.PropertyName != nameof(WorkareaEnd) &&
                e.PropertyName != nameof(CurrentTime) &&
                e.PropertyName != nameof(IsEnableShy))
            {
                OnCompositionUpdated(false);
            }
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var oldLayer in (e.OldItems?.Cast<LayerModel>() ?? []))
            {
                oldLayer.LayerUpdated -= Layer_LayerUpdated;
            }
            foreach (var newLayer in (e.NewItems?.Cast<LayerModel>() ?? []))
            {
                newLayer.LayerUpdated += Layer_LayerUpdated;
            }

            OnCompositionUpdated(false);
        }

        private void Layer_LayerUpdated(object? sender, EventArgs e)
        {
            if (sender is LayerModel updateLayer)
            {
                foreach (var layer in Layers)
                {
                    layer.ClearCacheByLayerUpdated();
                }
            }
            OnCompositionUpdated(false);
        }

        public void Dispose()
        {
            RendererContext.Value.Dispose();
            RendererContext.Dispose();
            ToneMapperContext.Value.Dispose();
            ToneMapperContext.Dispose();
        }
    }

    record CompositionDataImportConvertionResult(
        Guid OldCompositionId,
        Guid NewCompositionId,
        Dictionary<Guid, Guid> LayerIdMap,
        Dictionary<Guid, Dictionary<Guid, Guid>> EffectIdMaps,
        Dictionary<Guid, Dictionary<Guid, Guid>> MaskIdMaps
    );

    file class CoordTransformerWrapper : ICoordTransformerObject
    {
        ITransformer Transformer { get; }

        CompositionModel CompositionModel { get; }

        LayerModel Layer { get; }

        Time CurrentTime { get; }

        public CoordTransformerWrapper(ITransformer transformer, CompositionModel composition, LayerModel layer, Time currentTime)
        {
            Transformer = transformer;
            CompositionModel = composition;
            Layer = layer;
            CurrentTime = currentTime;
        }

        public Vector3d ScreenCoordToLocalCoord(Vector2d screenPosition, Time? time = null)
        {
            var targetTime = time ?? CurrentTime;
            var layerSkeleton = Layer.GetLayerSkeletonWithoutContainsTime(targetTime);
            if (layerSkeleton == null)
            {
                return Vector3d.Zero;
            }

            return Transformer.ScreenCoordToLocalCoord(CompositionModel.GetActiveCameraSetting(targetTime), layerSkeleton, screenPosition);
        }

        public Vector3d ScreenCoordToWorldCoord(Vector2d screenPosition, Time? time = null)
        {
            return Transformer.ScreenCoordToWorldCoord(CompositionModel.GetActiveCameraSetting(time ?? CurrentTime), screenPosition);
        }

        public Vector2d LocalCoordToScreenCoord(Vector3d localPosition, Time? time = null)
        {
            var targetTime = time ?? CurrentTime;
            var layerSkeleton = Layer.GetLayerSkeletonWithoutContainsTime(targetTime);

            if (layerSkeleton != null)
            {
                return Transformer.LocalCoordToScreenCoord(CompositionModel.GetActiveCameraSetting(targetTime), layerSkeleton, localPosition);
            }
            else
            {
                return Transformer.WorldCoordToScreenCoord(CompositionModel.GetActiveCameraSetting(targetTime), localPosition);
            }
        }

        public Vector2d WorldCoordToScreenCoord(Vector3d worldPosition, Time? time = null)
        {
            return Transformer.WorldCoordToScreenCoord(CompositionModel.GetActiveCameraSetting(time ?? CurrentTime), worldPosition);
        }
    }
}

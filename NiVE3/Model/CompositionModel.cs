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

namespace NiVE3.Model
{
    partial class CompositionModel : WeakPropertyChangedBindingBase, IDisposable, ICompositionObject
    {
        public Guid CompositionId { get; }

        string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double frameDuration;
        public double FrameDuration
        {
            get { return frameDuration; }
            private set { SetProperty(ref frameDuration, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private bool isRetentionFrameRate;
        public bool IsRetentionFrameRate
        {
            get { return isRetentionFrameRate; }
            set { SetProperty(ref isRetentionFrameRate, value); }
        }

        private bool applyToneMappingWhenNested;
        public bool ApplyToneMappingWhenNested
        {
            get { return applyToneMappingWhenNested; }
            set { SetProperty(ref applyToneMappingWhenNested, value); }
        }

        private int shutterAngle;
        public int ShutterAngle
        {
            get { return shutterAngle; }
            set { SetProperty(ref shutterAngle, value); }
        }

        private int shutterPhase;
        public int ShutterPhase
        {
            get { return shutterPhase; }
            set { SetProperty(ref shutterPhase, value); }
        }

        private int motionBlurSampleCount;
        public int MotionBlurSampleCount
        {
            get { return motionBlurSampleCount; }
            set { SetProperty(ref motionBlurSampleCount, value); }
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

        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private double workareaBegin;
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private bool isEnableShy;
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableFrameBlend;
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private ObservableCollection<LayerModel> layers = [];
        public ObservableCollection<LayerModel> Layers
        {
            get { return layers; }
            set
            {
                if (layers != value)
                {
                    layers.CollectionChanged -= Layers_CollectionChanged;
                    value.CollectionChanged += Layers_CollectionChanged;
                }
                SetProperty(ref layers, value);
            }
        }

        public Guid RendererPluginId { get; private set; }

        public Guid ToneMapperPluginId { get; private set; }

        public bool HasAudio => Layers.Any(l => l.IsEnableSolo) ? Layers.Any(l => l.HasAudio && l.IsEnableAudio && l.IsEnableSolo) : Layers.Any(l => l.HasAudio && l.IsEnableAudio);

        public event EventHandler<EventArgs>? CompositionUpdated;

        bool IsSettingChanging { get; set; }

        ExportLifetimeContext<IRenderer> RendererContext { get; set; }

        ExportLifetimeContext<IToneMapper> ToneMapperContext { get; set; }

        FootageListModel FootageListModel { get; }

        EffectListModel EffectListModel { get; }

        RenderQueueModel RenderQueueModel { get; }

        TextPropertyModel TextPropertyModel { get; }

        RendererListModel RendererListModel { get; }

        ToneMapperListModel ToneMapperListModel { get; }

        HistoryModel HistoryModel { get; }

        IRenderer Renderer => RendererContext.Value;

        IToneMapper ToneMapper => ToneMapperContext.Value;

        public CompositionModel(
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            HistoryModel historyModel
        ) : this(rendererPluginId, toneMapperPluginId, footageListModel, effectListModel, renderQueueModel, textPropertyModel, rendererListModel, toneMapperListModel, historyModel, null) { }

        public CompositionModel(
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            HistoryModel historyModel,
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
            TextPropertyModel = textPropertyModel;
            HistoryModel = historyModel;
            Layers = [];

            PropertyChanged += CompositionModel_PropertyChanged;
        }

        public void InsertLayers(Guid footageId, int index)
        {
            InsertLayers([footageId], index);
        }

        public void InsertLayers(Guid[] footageIds, int index)
        {
            var footages = footageIds.SelectMany(FootageListModel.GetFootages);
            var addedLayers = new List<LayerModel>();
            var startIndex = index;
            foreach (var f in footages)
            {
                if (f.InputModel.Input is CompositionInput compositionInput && IsCycledComposition(this, compositionInput))
                {
                    continue;
                }
                var layer = new LayerModel(this, f, EffectListModel, HistoryModel);
                if (f.InputType == SourceType.Image || f.InputType == SourceType.None)
                {
                    layer.OutPoint = Duration;
                }
                Layers.Insert(index, layer);
                addedLayers.Add(layer);
                index++;
            }

            if (addedLayers.Count > 0)
            {
                HistoryModel.Add(new AddLayersHistoryCommand(this, [..addedLayers], startIndex));
            }
        }

        public void AddCamera()
        {
            var layer = new LayerModel(this, FootageListModel.CameraFootage, EffectListModel, HistoryModel)
            {
                OutPoint = Duration
            };
            Layers.Insert(0, layer);

            HistoryModel.Add(new AddLayersHistoryCommand(this, [layer], 0));
        }

        public void AddLight()
        {
            var layer = new LayerModel(this, FootageListModel.LightFootage, EffectListModel, HistoryModel)
            {
                OutPoint = Duration
            };
            Layers.Insert(0, layer);

            HistoryModel.Add(new AddLayersHistoryCommand(this, [layer], 0));
        }

        public void AddNullObject()
        {
            var layer = new LayerModel(this, FootageListModel.NullObjectFootage, EffectListModel, HistoryModel)
            {
                OutPoint = Duration
            };
            Layers.Insert(0, layer);

            HistoryModel.Add(new AddLayersHistoryCommand(this, [layer], 0));
        }

        public void AddText()
        {
            var layer = new LayerModel(this, FootageListModel.TextFootage, EffectListModel, HistoryModel)
            {
                OutPoint = Duration
            };

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddLayers));

            TextPropertyModel.UpdateTextProperty(layer, 0.0);
            Layers.Insert(0, layer); // TODO: 選択位置の上に挿入
            HistoryModel.Add(new AddLayersHistoryCommand(this, [layer], 0));

            HistoryModel.EndGroup();
        }

        public void AddShape()
        {
            var layer = new LayerModel(this, FootageListModel.ShapeFootage, EffectListModel, HistoryModel)
            {
                OutPoint = Duration
            };
            Layers.Insert(0, layer); // TODO: 選択位置の上に挿入

            HistoryModel.Add(new AddLayersHistoryCommand(this, [layer], 0));
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
            var startIndex = newIndex - layers.IndexOf(l => l.LayerId == referenceLayerId);
            var newOrderedLayers = new List<LayerModel>(Layers.Count);
            newOrderedLayers.AddRange(Layers.Except(layers).Take(startIndex));
            newOrderedLayers.AddRange(layers);
            newOrderedLayers.AddRange(Layers.Except([..newOrderedLayers]));

            Layers.SortBy(l => newOrderedLayers.IndexOf(l));

            if (!prevIndices.SequenceEqual(layers.Select(l => Layers.IndexOf(l))))
            {
                // TODO: 古いインデックスを保持するのでは無く、古いLayersを配列にしたものを渡す
                HistoryModel.Add(new MoveLayersHistoryCommand(this, layers, prevIndices, [..newOrderedLayers]));
            }
        }

        public void ChangeLayerSwitches(Guid[] layerIds, string switchName, object newValue)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var propertyInfo = typeof(LayerModel).GetProperty(switchName);
            if (propertyInfo == null)
            {
                throw new Exception($"{switchName} Switch is not found"); // bug
            }

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
            foreach (var l in layers)
            {
                l.TrackMatteLayerId = targetLayerId;
            }

            HistoryModel.Add(new ChangeTrackMatteLayerHistoryCommand(layers, oldValues, targetLayerId));
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
            var layerIds = Layers.Where(l => l.FootageModel == footage).Select(l => l.LayerId).ToArray();
            DeleteLayers(layerIds);
        }

        public void EnqueueRender(string filePath, RenderRangeType renderRangeType, double beginTime, double endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput> output)
        {
            RenderQueueModel.Enqueue(this, filePath, renderRangeType, beginTime, endTime, isOutputVideo, isOutputAudio, output);
        }

        public void ChangeCompositionSetting(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, bool applyToneMappingWhenNested, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Guid rendererPluginId, Guid toneMapperPluginId)
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
                ToneMapperPluginId == toneMapperPluginId)
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

            if (RendererPluginId != rendererPluginId)
            {
                RendererContext.Dispose();
                RendererContext = RendererListModel.CreateRenderer(rendererPluginId);
                RendererPluginId = rendererPluginId;
                RendererContext.Value.SetSize(Width, Height);
            }
            if (ToneMapperPluginId != toneMapperPluginId)
            {
                ToneMapperContext.Dispose();
                ToneMapperContext = ToneMapperListModel.CreateToneMapper(toneMapperPluginId);
                ToneMapperPluginId = toneMapperPluginId;
            }

            FrameDuration = 1.0 / FrameRate;
            if (TimeBarRange > Duration)
            {
                TimeBarRange = Duration;
            }
            if (TimeBarRangeStart + TimeBarRange > Duration)
            {
                TimeBarRangeStart = Math.Max(Duration - TimeBarRangeStart, 0.0);
            }
            if (WorkareaBegin == 0.0 && WorkareaEnd == prevDuration)
            {
                WorkareaEnd = duration;
            }
            else
            {
                WorkareaBegin = Math.Min(WorkareaBegin, Duration - FrameDuration);
                WorkareaEnd = Math.Clamp(WorkareaEnd, WorkareaBegin + FrameDuration, Duration);
            }

            IsSettingChanging = false;
            OnCompositionUpdated();

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
                    WorkareaBegin,
                    WorkareaEnd
                )
            );
        }

        public void ChangeWorkarea(double begin, double end)
        {
            var prevWorkareaBegin = WorkareaBegin;
            var prevWorkareaEnd = WorkareaEnd;

            if (begin > end)
            {
                (begin, end) = (end, begin);
            }

            WorkareaBegin = Math.Clamp(begin, 0.0, Duration - FrameDuration);
            WorkareaEnd = Math.Clamp(end, WorkareaBegin + FrameDuration, Duration);

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

        public NImage RenderFrame(double time, double downSamplingRate, bool applyToneMapping, bool useGpu)
        {
            var cameraSetting = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time))?.GetCameraSetting(time);

            var hasLightSolo = Layers.Any(l => l.IsLight && l.IsEnableVideo && l.IsEnableSolo);
            var useLights = Layers.Where(l => l.IsEnableVideo && (!hasLightSolo || l.IsEnableSolo)).Select(l => l.GetLightSetting(time)).NonNull().ToArray();

            var hasImageSolo = Layers.Any(l => l.HasImage && l.IsEnableVideo && l.IsEnableSolo);
            var useLayers = Layers.Where(l => l.HasImage && l.IsEnableVideo && (!hasImageSolo || l.IsEnableSolo)).Reverse().ToArray();

            var hash = new XxHash3();
            if (downSamplingRate == 1.0)
            {
                hash.Append(Width);
                hash.Append(Height);
                hash.Append(FrameRate);
                hash.Append(ShutterAngle);
                hash.Append(ShutterPhase);
                hash.Append(motionBlurSampleCount);
                hash.Append(IsEnableFrameBlend);
                hash.Append(IsEnableMotionBlur);
                hash.Append(RendererPluginId);
                hash.Append(ToneMapperPluginId);

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
                    layer.CalcCacheKeyHash(hash, time, true);
                }
            }

            NImage result;
            var cacheKey = hash.ToInt128();
            if (downSamplingRate != 1.0 || !ImageCache.TryGet(CompositionId, cacheKey, time, out var cachedImage))
            {
                var allImages = new List<IDisposable>();

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
                foreach (var l in useLayers)
                {
                    if (!l.IsContainsTime(time))
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

                        using var adjustmentMaskImage = l.GetRawImage(time, downSamplingRate, true, useGpu);
                        if (adjustmentMaskImage == null)
                        {
                            continue;
                        }

                        var mask = Renderer.RenderAdjustmentMask(adjustmentMaskImage);
                        var currentRenderingFrame = Renderer.GetCurrentRenderedImage();
                        var (roi, currentFrame) = l.ProcessAdjustment(time, currentRenderingFrame, Width / (double)currentRenderingFrame.Width, Height / (double)currentRenderingFrame.Height, useGpu);

                        // TODO: GPU対応
                        if (mask is GPURasterizedMaskImage gpuMaskImage)
                        {
                            var managedImage = gpuMaskImage.CopyToCpu();
                            mask.Dispose();
                            mask = managedImage;
                        }
                        if (currentFrame is NGPUImage gpuCurrentFrame)
                        {
                            var managedImage = gpuCurrentFrame.CopyToCpu();
                            currentFrame.Dispose();
                            currentFrame = managedImage;
                        }
                        Parallel.For(roi.OriginalImagePosition.Y, roi.OriginalImagePosition.Y + roi.OriginalImageSize.Height, y =>
                        {
                            var maskSpan = ((ManagedRasterizedMaskImage)mask).GetDataSpan().Slice((y - roi.OriginalImagePosition.Y) * mask.Width, mask.Width);
                            var currentFrameSpan = ((NManagedImage)currentFrame).GetDataSpan().Slice(y * currentFrame.Width, currentFrame.Width);
                            for (int x = roi.OriginalImagePosition.X, limit = x + roi.OriginalImageSize.Width, maskPos = 0, framePos = x; x < limit; x++, maskPos++, framePos++)
                            {
                                currentFrameSpan[framePos].W *= maskSpan[maskPos];
                            }
                        });

                        Renderer.RenderAdjustmentLayer(currentFrame, roi, downSamplingRate, l.InterpolationQuality, l.BlendMode);

                        allImages.Add(currentFrame);
                    }
                    else
                    {
                        var image = l.GetImage(time, downSamplingRate, true, useGpu);
                        if (image != null)
                        {
                            images.Add(image);
                            allImages.Add(image);
                        }
                    }
                }
                if (images.Count > 0)
                {
                    Renderer.Render([.. images]);
                }

                result = Renderer.FinishRendering();

                foreach (var i in allImages)
                {
                    i.Dispose();
                }

                if (downSamplingRate == 1.0)
                {
                    if (result is NGPUImage gpuImage)
                    {
                        using var managedImage = gpuImage.CopyToCpu();
                        ImageCache.Add(CompositionId, cacheKey, time, managedImage, ROI.Empty);
                    }
                    else
                    {
                        ImageCache.Add(CompositionId, cacheKey, time, (NManagedImage)result, ROI.Empty);
                    }
                }
            }
            else
            {
                result = cachedImage.Item1;
            }

            if (applyToneMapping)
            {
                result = ToneMapper.ToneMapping(result, useGpu);
            }

            return result;
        }

        public float[] RenderAudio(double time, double length)
        {
            var vectorLength = Vector<float>.Count;

            var result = new float[(int)(length * Const.AudioSamplingRate) * 2];
            var resultVectorSpan = MemoryMarshal.Cast<float, Vector<float>>(result.AsSpan(0, (result.Length / vectorLength) * vectorLength));

            var hasSolo = Layers.Any(l => l.HasAudio && l.IsEnableSolo);
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

        public ColoredPreviewBoundingBox[] GetBoundingBoxes(Guid[] layerIds, double time)
        {
            var layers = Layers.Where(l => (l.HasImage || l.IsSpecial) && layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf);
            var result = new List<ColoredPreviewBoundingBox>();

            var activeCamera = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
            var activeCameraSetting = GetActiveCameraSetting(time);

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
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetCameraBoundingBox(cameraSetting, activeCameraSetting), layer.TagColor));
                    }
                }
                else if (layer.IsLight)
                {
                    var lightSetting = layer.GetLightSetting(time);
                    if (lightSetting != null)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetLightBoundingBox(lightSetting, activeCameraSetting), layer.TagColor));
                    }
                }
                else
                {
                    var (origin, width, height) = layer.GetSourceFootageRect(time);
                    if (layer.IsEnable3D)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetBoundingBox3D(origin, width, height, layer.GetTransform(time), layer.GetParentTransforms(time), activeCameraSetting), layer.TagColor));
                    }
                    else
                    {
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetBoundingBox2D(origin, width, height, layer.GetTransform(time), layer.GetParentTransforms(time)), layer.TagColor));
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
                RendererPluginId = RendererPluginId,
                ToneMapperPluginId = ToneMapperPluginId,
                TimeBarRange = TimeBarRange,
                TimeBarRangeStart = TimeBarRangeStart,
                CurrentTime = CurrentTime,
                Layers = Layers.Select(l => l.SaveData()).ToArray()
            };
        }

        public void LoadData(CompositionData data)
        {
            Name = data.Name;
            Width = data.Width;
            Height = data.Height;
            FrameRate = data.FrameRate;
            Duration = data.Duration;
            IsRetentionFrameRate = data.IsRetentionFrameRate;
            ApplyToneMappingWhenNested = data.ApplyToneMappingWhenNested;
            ShutterAngle = data.ShutterAngle;
            ShutterPhase = data.ShutterPhase;
            MotionBlurSampleCount = data.MotionBlurSampleCount;
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

                var layer = new LayerModel(this, footageModels.First(), EffectListModel, HistoryModel, layerData.LayerId);
                layer.LoadData(layerData);
                Layers.Add(layer);
            }

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

        public void DuplicateLayers(Guid[] ids, Guid? insertTargetId)
        {
            PasteLayersInternal(CopyLayers(ids), insertTargetId, true);
        }

        public void SplitLayers(Guid[] ids, double splitPositionTime)
        {
            var layers = Layers.Where(l => ids.Contains(l.LayerId))
                .Where(l => l.InPoint < TimeCalc.RoundTimeDigit(splitPositionTime - l.SourceStartPoint) && l.OutPoint > TimeCalc.RoundTimeDigit(splitPositionTime - l.SourceStartPoint))
                .OrderBy(Layers.IndexOf)
                .ToArray();
            if (layers.Length < 1)
            {
                return;
            }

            ids = [..layers.Select(l => l.LayerId)];

            var data = CopyLayers(ids);
            var addedLayer = new Dictionary<Guid, LayerModel>();
            foreach (var layerData in data.Data)
            {
                var footageModels = FootageListModel.GetFootages(layerData.FootageId);
                if (footageModels.Length < 1)
                {
                    continue;
                }

                layerData.InPoint = TimeCalc.RoundTimeDigit(splitPositionTime - layerData.SourceStartPoint);
                var newLayer = new LayerModel(this, footageModels.First(), EffectListModel, HistoryModel);
                newLayer.LoadData(layerData);
                var index = Layers.IndexOf(l => l.LayerId == layerData.LayerId);
                Layers.Insert(index, newLayer);
                addedLayer.Add(layerData.LayerId, newLayer);
            }

            var oldOutPoint = new double[layers.Length];
            var newOutPoint = new double[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                oldOutPoint[i] = layers[i].OutPoint;
                layers[i].OutPoint = splitPositionTime - layers[i].SourceStartPoint;
                newOutPoint[i] = layers[i].OutPoint;
            }

            HistoryModel.Add(new SplitLayersHistoryCommand(this, layers, addedLayer, oldOutPoint, newOutPoint));
        }

        public void ReplacePlaceholder(FootageModel newFootageModel)
        {
            foreach (var layer in Layers.Where(l => l.FootageModel.IsPlaceholder && l.FootageModel.FootageId == newFootageModel.FootageId))
            {
                layer.ReplaceFootage(newFootageModel);
            }
        }

        public Guid? FindLayerByPreviewPosition(double time, Vector2d pos)
        {
            var activeCamera = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
            var activeCameraSetting = activeCamera?.GetCameraSetting(time) ?? CreateDefaultCameraSetting(Width, Height);
            var hasImageSolo = Layers.Any(l => l.HasImage && l.IsEnableVideo && l.IsEnableSolo);
            var layers = Layers.Where(l => l.HasImage && l.IsEnableVideo && (!hasImageSolo || l.IsEnableSolo)).Select(l => l.GetLayerSkeleton(time)).NonNull().Reverse().ToArray();

            return Renderer.SelectLayer(activeCameraSetting, layers, pos);
        }

        public LayerSkeleton? GetLayerSkeleton(Guid layerId, double time)
        {
            return Layers.FirstOrDefault(l => l.LayerId == layerId)?.GetLayerSkeleton(time);
        }

        public Vector2d Projection(CameraSetting cameraSetting, LayerSkeleton? baseLayerSkeleton, Vector3d pos)
        {
            return Renderer.WorldCoordToScreenCoord(cameraSetting, baseLayerSkeleton, pos);
        }

        public Vector3d Unprojection(CameraSetting cameraSetting, LayerSkeleton? baseLayerSkeleton, Vector2d pos)
        {
            return Renderer.ScreenCoordToWorldCoord(cameraSetting, baseLayerSkeleton, pos);
        }

        public LayerModel? GetActiveCamera(double time)
        {
            return Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time));
        }

        public CameraSetting GetActiveCameraSetting(double time)
        {
            var activeCamera = GetActiveCamera(time);
            return activeCamera?.GetCameraSetting(time) ?? CreateDefaultCameraSetting(Width, Height);
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

        void DeleteLayersInternal(Guid[] ids, bool isCut)
        {
            var layers = Layers.Where(l => ids.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldIndices = layers.Select(l => Layers.IndexOf(l)).ToArray();

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveLayers));

            var childLayers = layers.SelectMany(p => Layers.Where(c => c.ParentLayerId == p.LayerId)).Select(l => l.LayerId).ToArray();
            if (childLayers.Length > 0)
            {
                ChangeParentLayer(childLayers, null);
            }

            var trackMatteChildLayers = layers.SelectMany(p => Layers.Where(c => c.TrackMatteLayerId == p.LayerId)).Select(l => l.LayerId).ToArray();
            if (trackMatteChildLayers.Length > 0)
            {
                ChangeTrackMatteLayers(trackMatteChildLayers, null);
            }

            foreach (var l in layers)
            {
                Layers.Remove(l);
            }

            HistoryModel.Add(new DeleteLayersHistoryCommand(this, layers, oldIndices, isCut));

            HistoryModel.EndGroup();
        }

        void PasteLayersInternal(CopyData<LayerData> data, Guid? insertTargetId, bool isDuplicate)
        {
            if (data.Type != CopyDataType.Layer || data.Data.Length < 1)
            {
                return;
            }

            var addedLayer = new List<LayerModel>();
            var insertStartIndex = insertTargetId.HasValue ? Layers.IndexOf(l => l.LayerId == insertTargetId) : -1;
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
            foreach (var layerData in data.Data)
            {
                var footageModels = FootageListModel.GetFootages(layerData.FootageId);
                if (footageModels.Length < 1)
                {
                    continue;
                }

                var newLayer = new LayerModel(this, footageModels.First(), EffectListModel, HistoryModel);
                newLayer.LoadData(layerData);
                Layers.Insert(index, newLayer);
                addedLayer.Add(newLayer);
                index++;

                newLayerIds.Add(layerData.LayerId, newLayer.LayerId);
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

            HistoryModel.Add(new PasteLayersHistoryCommand(this, [.. addedLayer], insertStartIndex, isDuplicate));
        }

        void OnCompositionUpdated()
        {
            ImageCache.Clear(CompositionId);
            CompositionUpdated?.Invoke(this, EventArgs.Empty);
        }

        static CameraSetting CreateDefaultCameraSetting(int width, int height)
        {
            // TODO: LayerModelの方と合わせてどこかに定義する
            const double DefaultCameraFov = 0.360000466176267;// Math.Tan(39.5978 * 0.5 * (Math.PI / 180.0))
            var zoom = width / DefaultCameraFov * 0.5;

            return new CameraSetting(
                new Vector3d(width * 0.5, height * 0.5, 0),
                new Vector3d(width * 0.5, height * 0.5, -zoom),
                new Vector3d(),
                0.0, 0.0, 0.0,
                zoom,
                []
            );
        }

        static bool IsCycledComposition(CompositionModel target, CompositionInput input)
        {
            if (input.Composition == target)
            {
                return true;
            }
            foreach (var i in input.Composition.Layers.Select(l => l.FootageModel.InputModel.Input).OfType<CompositionInput>())
            {
                if (IsCycledComposition(target, i))
                {
                    return true;
                }
            }
            return false;
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
                    FrameDuration = 1.0 / FrameRate;
                    break;
                case nameof(Duration):
                    if (TimeBarRange < Duration)
                    {
                        TimeBarRange = Duration;
                    }
                    if (TimeBarRangeStart + TimeBarRange > Duration)
                    {
                        TimeBarRangeStart = Math.Max(Duration - TimeBarRangeStart, 0.0);
                    }
                    WorkareaBegin = 0.0;
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
                OnCompositionUpdated();
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
                newLayer.LayerUpdated += Layer_LayerUpdated; ;
            }

            OnCompositionUpdated();
        }

        private void Layer_LayerUpdated(object? sender, EventArgs e)
        {
            OnCompositionUpdated();
        }

        public void Dispose()
        {
            RendererContext.Value.Dispose();
            RendererContext.Dispose();
            ToneMapperContext.Value.Dispose();
            ToneMapperContext.Dispose();
        }
    }
}

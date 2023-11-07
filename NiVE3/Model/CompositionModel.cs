using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NiVE3.Extension;
using NiVE3.Input;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class CompositionModel : BindableBase, IDisposable, ICompositionObject
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

        private bool hasAudio;
        public bool HasAudio
        {
            get { return hasAudio; }
            set { SetProperty(ref hasAudio, value); }
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

        private ObservableCollection<LayerModel> layers = new ObservableCollection<LayerModel>();
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

        public event EventHandler<EventArgs>? CompositionUpdated;

        ExportLifetimeContext<IRenderer> RendererContext { get; }

        FootageListModel FootageListModel { get; }

        EffectListModel EffectListModel { get; }

        HistoryModel HistoryModel { get; }

        IRenderer Renderer => RendererContext.Value;

        public CompositionModel(ExportLifetimeContext<IRenderer> renderer, FootageListModel footageListModel, EffectListModel effectListModel, HistoryModel historyModel) : this(renderer, footageListModel, effectListModel, historyModel, null) { }

        public CompositionModel(ExportLifetimeContext<IRenderer> renderer, FootageListModel footageListModel, EffectListModel effectListModel, HistoryModel historyModel, Guid? compositionId)
        {
            if (compositionId == null)
            {
                compositionId = Guid.NewGuid();
            }
            CompositionId = compositionId.Value;
            RendererContext = renderer;
            FootageListModel = footageListModel;
            EffectListModel = effectListModel;
            HistoryModel = historyModel;
            Layers = new ObservableCollection<LayerModel>();

            PropertyChanged += CompositionModel_PropertyChanged;
        }

        public void InsertLayers(Guid footageId, int index)
        {
            InsertLayers(new Guid[] { footageId }, index);
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
                HistoryModel.Add(new AddLayersHistoryCommand(this, addedLayers.ToArray(), startIndex));
            }
        }

        public void AddCamera()
        {
            var layer = new LayerModel(this, FootageListModel.CameraFootage, EffectListModel, HistoryModel);
            layer.OutPoint = Duration;
            Layers.Insert(0, layer);

            HistoryModel.Add(new AddLayersHistoryCommand(this, new LayerModel[] { layer }, 0));
        }

        public void MoveLayer(Guid layerId, int newIndex)
        {
            MoveLayers(new Guid[] { layerId }, layerId, newIndex);
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
            newOrderedLayers.AddRange(Layers.Except(newOrderedLayers.ToArray()));

            Layers.SortBy(l => newOrderedLayers.IndexOf(l));

            if (!prevIndices.SequenceEqual(layers.Select(l => Layers.IndexOf(l))))
            {
                HistoryModel.Add(new MoveLayersHistoryCommand(this, layers, prevIndices, newOrderedLayers.ToArray()));
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
                layerIds = layerIds.Except(new Guid[] { targetLayerId.Value }).ToArray();
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

            HistoryModel.Add(new ChangeParentLayerHistoryCommand(layers.ToArray(), oldValues, targetLayerId));
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

        public void DeleteLayers(Guid[] layerIds)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldIndices = layers.Select(l => Layers.IndexOf(l)).ToArray();

            foreach (var l in layers)
            {
                Layers.Remove(l);
            }

            HistoryModel.Add(new DeleteLayersHistoryCommand(this, layers, oldIndices));
        }

        public void DeleteLayersByFootage(FootageModel footage)
        {
            var layerIds = Layers.Where(l => l.FootageModel == footage).Select(l => l.LayerId).ToArray();
            DeleteLayers(layerIds);
        }

        public NImage Render(double time, double downSamplingRate, bool useGpu)
        {
            var allImages = new List<RenderableImage>();

            Renderer.BeginRendering(downSamplingRate, useGpu);

            var cameraSetting = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time))?.GetCameraSetting(time);
            if (cameraSetting != null)
            {
                Renderer.SetCamera(cameraSetting);
            }
            else
            {
                Renderer.SetCamera(CreateDefaultCameraSetting(Width, Height));
            }

            var images = new List<RenderableImage>();
            var hasSolo = Layers.Any(l => l.IsEnableSolo);
            foreach (var l in Layers.Where(l => l.HasImage && l.IsEnableVideo && (!hasSolo || l.IsEnableSolo)).Reverse())
            {
                if (!l.IsContainsTime(time))
                {
                    continue;
                }

                if (l.IsEnableAdjustmentLayer)
                {
                    if (images.Count > 0)
                    {
                        Renderer.Render(images.ToArray());
                    }
                    images.Clear();

                    // TODO: 調整レイヤーの適用
                }
                else
                {
                    var image = l.GetImage(time, downSamplingRate, useGpu);
                    if (image != null)
                    {
                        images.Add(image);
                        allImages.Add(image);
                    }
                }
            }
            if (images.Count > 0)
            {
                Renderer.Render(images.ToArray());
            }

            var result = Renderer.FinishRendering();

            foreach (var i in allImages)
            {
                i.Dispose();
            }

            return result;
        }

        public ColoredPreviewBoundingBox[] GetBoundingBoxes(Guid[] layerIds, double time)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf);
            var result = new List<ColoredPreviewBoundingBox>();

            var cameraSetting = Layers.FirstOrDefault(l => l.IsEnableVideo && l.IsCamera && l.IsContainsTime(time))?.GetCameraSetting(time);
            if (cameraSetting == null)
            {
                cameraSetting = CreateDefaultCameraSetting(Width, Height);
            }

            foreach (var layer in layers)
            {
                // TODO: テキストレイヤーやシェイプレイヤー等のサイズが可変な入力への対応
                var width = layer.FootageModel.Width;
                var height = layer.FootageModel.Height;
                if (layer.IsEnable3D)
                {
                    result.Add(new ColoredPreviewBoundingBox(Renderer.CalcBoundingBox3D(width, height, Width, Height, layer.GetTransform(time), layer.GetParentTransforms(time), cameraSetting), layer.TagColor));
                }
                else
                {
                    result.Add(new ColoredPreviewBoundingBox(Renderer.CalcBoundingBox2D(width, height, Width, Height, layer.GetTransform(time), layer.GetParentTransforms(time)), layer.TagColor));
                }
            }

            return result.ToArray();
        }

        bool CheckCycledSimulatedParentLayer(Guid layerId, Dictionary<Guid, Guid?> changed, HashSet<Guid>? checkedLayerIds = null)
        {
            if (checkedLayerIds == null)
            {
                checkedLayerIds = new HashSet<Guid>();
            }

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

            var parentLayerId = changed.ContainsKey(layerId) ? changed[layerId] : layer.ParentLayerId;
            if (!parentLayerId.HasValue)
            {
                return false;
            }

            return CheckCycledSimulatedParentLayer(parentLayerId.Value, changed, checkedLayerIds);
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
                Array.Empty<ParentTransform>()
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

            CompositionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var oldLayer in (e.OldItems?.Cast<LayerModel>() ?? Enumerable.Empty<LayerModel>()))
            {
                oldLayer.LayerUpdated -= ayer_LayerUpdated;
            }
            foreach (var newLayer in (e.NewItems?.Cast<LayerModel>() ?? Enumerable.Empty<LayerModel>()))
            {
                newLayer.LayerUpdated += ayer_LayerUpdated; ;
            }

            CompositionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void ayer_LayerUpdated(object? sender, EventArgs e)
        {
            CompositionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            RendererContext.Value.Dispose();
            RendererContext.Dispose();
        }
    }
}

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
using NiVE3.Data.Json.Project;
using NiVE3.Extension;
using NiVE3.Input;
using NiVE3.Numerics;
using NiVE3.Image;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Resource;
using Prism.Mvvm;
using NiVE3.Image.Drawing;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Buffers;
using NiVE3.Util;

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

        public bool HasAudio => Layers.Any(l => l.IsEnableSolo) ? Layers.Any(l => l.HasAudio && l.IsEnableAudio && l.IsEnableSolo) : Layers.Any(l => l.HasAudio && l.IsEnableAudio);

        public event EventHandler<EventArgs>? CompositionUpdated;

        ExportLifetimeContext<IRenderer> RendererContext { get; }

        ExportLifetimeContext<IToneMapper> ToneMapperContext { get; }

        Guid RendererPluginId { get; }

        Guid ToneMapperPluginId { get; }

        FootageListModel FootageListModel { get; }

        EffectListModel EffectListModel { get; }

        TextPropertyModel TextPropertyModel { get; }

        HistoryModel HistoryModel { get; }

        IRenderer Renderer => RendererContext.Value;

        IToneMapper ToneMapper => ToneMapperContext.Value;

        public CompositionModel(
            ExportLifetimeContext<IRenderer> renderer,
            ExportLifetimeContext<IToneMapper> toneMapper,
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            TextPropertyModel textPropertyModel,
            HistoryModel historyModel
        ) : this(renderer, toneMapper, rendererPluginId, toneMapperPluginId, footageListModel, effectListModel, textPropertyModel, historyModel, null) { }

        public CompositionModel(
            ExportLifetimeContext<IRenderer> renderer,
            ExportLifetimeContext<IToneMapper> toneMapper,
            Guid rendererPluginId,
            Guid toneMapperPluginId,
            FootageListModel footageListModel,
            EffectListModel effectListModel,
            TextPropertyModel textPropertyModel,
            HistoryModel historyModel,
            Guid? compositionId
        )
        {
            CompositionId = compositionId ?? Guid.NewGuid();
            RendererContext = renderer;
            ToneMapperContext = toneMapper;
            RendererPluginId = rendererPluginId;
            ToneMapperPluginId = toneMapperPluginId;
            FootageListModel = footageListModel;
            EffectListModel = effectListModel;
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

        public void DeleteLayers(Guid[] layerIds)
        {
            var layers = Layers.Where(l => layerIds.Contains(l.LayerId)).OrderBy(Layers.IndexOf).ToArray();
            var oldIndices = layers.Select(l => Layers.IndexOf(l)).ToArray();

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteLayers));

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

            HistoryModel.Add(new DeleteLayersHistoryCommand(this, layers, oldIndices));

            HistoryModel.EndGroup();
        }

        public void DeleteLayersByFootage(FootageModel footage)
        {
            var layerIds = Layers.Where(l => l.FootageModel == footage).Select(l => l.LayerId).ToArray();
            DeleteLayers(layerIds);
        }

        public NImage RenderFrame(double time, double downSamplingRate, bool applyToneMapping, bool useGpu)
        {
            var allImages = new List<IDisposable>();

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

            foreach (var light in Layers.Where(l => l.IsEnableVideo).Select(l => l.GetLightSetting(time)).NonNull())
            {
                Renderer.AddLight(light);
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
                        Renderer.Render([..images]);
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
                        for (int x = roi.OriginalImagePosition.X, limit = x + roi.OriginalImageSize.Width, maskPos = 0, framePos = x; x < limit; x++,  maskPos++, framePos++)
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
                Renderer.Render([..images]);
            }

            var result = Renderer.FinishRendering();

            if (applyToneMapping)
            {
                result = ToneMapper.ToneMapping(result, useGpu);
            }

            foreach (var i in allImages)
            {
                i.Dispose();
            }

            return result;
        }

        public float[] RenderAudio(double time, double length)
        {
            var vectorLength = Vector<float>.Count;

            var result = new float[(int)(length * Const.AudioSamplingRate) * 2];
            var resultVectorSpan = MemoryMarshal.Cast<float, Vector<float>>(result.AsSpan(0, (result.Length / vectorLength) * vectorLength));

            var hasSolo = Layers.Any(l => l.IsEnableSolo);
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
            var activeCameraSetting = activeCamera?.GetCameraSetting(time) ?? CreateDefaultCameraSetting(Width, Height);

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
                    // TODO: テキストレイヤーやシェイプレイヤー等のサイズが可変な入力への対応
                    var width = layer.FootageModel.Width;
                    var height = layer.FootageModel.Height;
                    if (layer.IsEnable3D)
                    {
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetBoundingBox3D(width, height, layer.GetTransform(time), layer.GetParentTransforms(time), activeCameraSetting), layer.TagColor));
                    }
                    else
                    {
                        result.Add(new ColoredPreviewBoundingBox(Renderer.GetBoundingBox2D(width, height, layer.GetTransform(time), layer.GetParentTransforms(time)), layer.TagColor));
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

        public void ReplacePlaceholder(FootageModel newFootageModel)
        {
            foreach (var layer in Layers.Where(l => l.FootageModel.IsPlaceholder && l.FootageModel.FootageId == newFootageModel.FootageId))
            {
                layer.ReplaceFootage(newFootageModel);
            }
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
                e.PropertyName != nameof(CurrentTime))
            {
                CompositionUpdated?.Invoke(this, EventArgs.Empty);
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

            CompositionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Layer_LayerUpdated(object? sender, EventArgs e)
        {
            CompositionUpdated?.Invoke(this, EventArgs.Empty);
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

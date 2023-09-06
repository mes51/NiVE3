using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NiVE3.Extension;
using NiVE3.Input;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class CompositionModel : BindableBase, IDisposable
    {
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

        IRenderer Renderer { get; }

        FootageListModel FootageListModel { get; }

        HistoryModel HistoryModel { get; }

        public CompositionModel(IRenderer renderer, FootageListModel footageListModel, HistoryModel historyModel)
        {
            Renderer = renderer;
            FootageListModel = footageListModel;
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
                var layer = new LayerModel(f, HistoryModel);
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

        public NImage Render(double time, bool useGpu)
        {
            // TODO:
            return new NManagedImage(Width, Height, true);
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
            }
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public void Dispose()
        {
            Renderer.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Exceptions;
using NiVE3.Image;
using NiVE3.Input;
using NiVE3.Mvvm;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial interface IFootageModel : INotifyPropertyChanged
    {
        Guid FootageId { get; }

        string Name { get; set; }

        int Width { get; }

        int Height { get; }

        double FrameRate { get; }

        Time Duration { get; }

        string FilePath { get; }

        string Comment { get; set; }

        string FileName { get; }

        SourceType InputType { get; }

        FootageSortKey SortKey { get; set; }

        bool SortIsAscending { get; set; }

        ObservableCollection<IFootageModel>? Children { get; }

        void AddFootage(IFootageModel footage);

        void RemoveFootage(IFootageModel footage);

        void ChangeName(string name);

        void ChangeComment(string name);

        FootageData SaveData();

        public static (Guid oldId, Guid newId) ConvertDataForImport(FootageData footageData, Dictionary<Guid, Guid> inputIdMap)
        {
            var oldId = footageData.FootageId;
            footageData.FootageId = Guid.NewGuid();
            footageData.InputId = footageData.InputId.HasValue ? inputIdMap[footageData.InputId.Value] : null;

            return (oldId, footageData.FootageId);
        }
    }

    [UseReactiveProperty]
    partial class FootageModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; }

        public string Name
        {
            get;
            set
            {
                SetProperty(ref field, value);
                IsNameChanged = value != Source.Name;
            }
        }

        [ReactiveProperty]
        public partial int Width { get; private set; }

        [ReactiveProperty]
        public partial int Height { get; private set; }

        [ReactiveProperty]
        public partial double FrameRate { get; private set; }

        [ReactiveProperty]
        public partial Time Duration { get; private set; }

        [ReactiveProperty]
        public partial string FilePath { get; private set; } = "";

        [ReactiveProperty]
        public partial string Comment { get; set; } = "";

        [ReactiveProperty]
        public partial SourceType InputType { get; set; }

        [ReactiveProperty]
        public partial FootageSortKey SortKey { get; set; }

        [ReactiveProperty]
        public partial bool SortIsAscending { get; set; }

        // TODO: フッテージの再読み込み実装時に更新するようにする
        public DateTime LastUpdated
        {
            get;
            set { SetProperty(ref field, value); }
        }

        public string FileName => Path.GetFileName(InputModel.FilePath);

        public bool IsPlaceholder => InputModel.IsPlaceholder;

        public bool IsCustomizableFootageSource => Source is ICustomizableFootageSource;

        public InputModel InputModel { get; }

        public ObservableCollection<IFootageModel>? Children => null;

        bool IsNameChanged { get; set; }

        WeakEventPublisher<EventArgs> UpdateSampleImageRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> UpdateSampleImageRequest
        {
            add { UpdateSampleImageRequestPublisher.Subscribe(value); }
            remove { UpdateSampleImageRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<NeedHistoryChangeEventArgs> FootageUpdatedPublisher { get; } = new WeakEventPublisher<NeedHistoryChangeEventArgs>();
        public event EventHandler<NeedHistoryChangeEventArgs> FootageUpdated
        {
            add { FootageUpdatedPublisher.Subscribe(value); }
            remove { FootageUpdatedPublisher.Unsubscribe(value); }
        }

        IFootageSource Source { get; }

        HistoryModel HistoryModel { get; }

        bool IsSupportLoadToGpu { get; }

        public FootageModel(InputModel input, IFootageSource source, HistoryModel historyModel) : this(input, source, historyModel, null) { }

        public FootageModel(InputModel input, IFootageSource source, HistoryModel historyModel, Guid? footageId)
        {
            HistoryModel = historyModel;
            FootageId = footageId ?? Guid.NewGuid();
            InputModel = input;
            Source = source;
            IsSupportLoadToGpu = input.IsSupportLoadToGpu;
            Name = string.IsNullOrEmpty(source.Name) ? Path.GetFileName(input.FilePath) : source.Name;
            if (source.SourceType.HasFlag(SourceType.Video))
            {
                Width = source.Width;
                Height = source.Height;
                FrameRate = source.FrameRate;
                Duration = source.Duration;
            }
            else if (source.SourceType.HasFlag(SourceType.Image))
            {
                Width = source.Width;
                Height = source.Height;
            }
            else if (source.SourceType.HasFlag(SourceType.Audio))
            {
                Duration = source.Duration;
                FrameRate = 30.0;
            }
            FilePath = input.FilePath;
            InputType = source.SourceType;
            LastUpdated = DateTime.Now;

            if (input.Input is CompositionInput compositionInput)
            {
                compositionInput.Composition.CompositionUpdated += Composition_CompositionUpdated;
            }
        }

        public void AddFootage(IFootageModel footage) { }

        public void RemoveFootage(IFootageModel footage) { }

        public PropertyBase[] GetOptionProperties()
        {
            if (Source is ICustomizableFootageSource customizableFootageSource)
            {
                return customizableFootageSource.GetOptionProperties();
            }
            else
            {
                throw new Exception();
            }
        }

        public SourceFootageRect CalcSize(Time time, int compositionWidth, int compositionHeight, bool withInvisible, LayerModel layer, PropertyValueGroup? properties)
        {
            if (properties != null && Source is ICustomizableFootageSource customizableFootageSource)
            {
                return customizableFootageSource.CalcSize(time, compositionWidth, compositionHeight, withInvisible, layer, properties);
            }
            else
            {
                return new SourceFootageRect(Vector2d.Zero, Width, Height);
            }
        }

        public NImage ReadImage(Time time, double downSamplingRate, int compositionWidth, int compositionHeight, LayerModel? layer, PropertyValueGroup? properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            try
            {
                if (properties != null && Source is ICustomizableFootageSource customizableFootageSource && layer != null)
                {
                    return customizableFootageSource.ReadFrame(time, downSamplingRate, compositionWidth, compositionHeight, layer, properties, imageInterpolationQuality, toGpu && IsSupportLoadToGpu);
                }
                else
                {
                    return Source.ReadFrame(time, downSamplingRate, toGpu && IsSupportLoadToGpu);
                }
            }
            catch (GPUException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (toGpu && IsSupportLoadToGpu)
                {
                    throw new GPUException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public float[] ReadAudio(Time time, Time length)
        {
            return Source.ReadAudio(time, length);
        }

        public void ChangeName(string name)
        {
            if (Name != name)
            {
                var prevName = Name;
                Name = name;
                HistoryModel.Add(IFootageModel.CreateChangeNameHistory(this, prevName, name));
            }
        }

        public void ChangeComment(string comment)
        {
            if (Comment != comment)
            {
                var prevComment = Comment;
                Comment = comment;
                HistoryModel.Add(IFootageModel.CreateChangeCommentHistoryCommand(this, prevComment, comment));
            }
        }

        public FootageData SaveData()
        {
            return new FootageData
            {
                DataType = FootageDataType.Source,
                FootageId = FootageId,
                Name = Name,
                Width = Width,
                Height = Height,
                FrameRate = FrameRate,
                Duration = Duration,
                FilePath = FilePath,
                Comment = Comment,
                InputType = InputType,
                InputId = InputModel.InputId,
                InputPluginId = InputModel.PluginId,
                InputOption = InputModel.Input.SaveSetting(),
                SourceId = Source.SourceId
            };
        }

        private void Composition_CompositionUpdated(object? sender, NeedHistoryChangeEventArgs e)
        {
            Width = Source.Width;
            Height = Source.Height;
            FrameRate = Source.FrameRate;
            Duration = Source.Duration;
            LastUpdated = DateTime.Now;

            UpdateSampleImageRequestPublisher.Publish(this, EventArgs.Empty);
            FootageUpdatedPublisher.Publish(this, e);

            if (!IsNameChanged)
            {
                Name = Source.Name ?? InputModel.FilePath;
            }
        }
    }

    [UseReactiveProperty]
    partial class FootageFolderModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; }

        [ReactiveProperty]
        public partial string Name { get; set; } = "";

        public int Width => 0;

        public int Height => 0;

        public double FrameRate => 0.0;

        public Time Duration => Time.Zero;

        public string FilePath => "";

        [ReactiveProperty]
        public partial string Comment { get; set; } = "";

        public string FileName => "";

        public SourceType InputType => SourceType.None;

        [ReactiveProperty]
        public partial FootageSortKey SortKey { get; set; }

        [ReactiveProperty]
        public partial bool SortIsAscending { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<IFootageModel> Children { get; set; } = [];

        HistoryModel HistoryModel { get; }

        public FootageFolderModel(HistoryModel historyModel) : this(historyModel, null) { }

        public FootageFolderModel(HistoryModel historyModel, Guid? footageId)
        {
            HistoryModel = historyModel;
            FootageId = footageId ?? Guid.NewGuid();
            Name = "New Folder";
            PropertyChanged += FootageFolderModel_PropertyChanged;
        }

        public void AddFootage(IFootageModel footage)
        {
            footage.PropertyChanged += Footage_PropertyChanged;
            Children.Add(footage);
            Children.Sort(new FootageComparer(SortKey, SortIsAscending));
        }

        public void RemoveFootage(IFootageModel footage)
        {
            if (Children.Contains(footage))
            {
                footage.PropertyChanged -= Footage_PropertyChanged;
                Children.Remove(footage);
            }
        }

        private void FootageFolderModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SortKey) || e.PropertyName == nameof(SortIsAscending))
            {
                foreach (var child in Children)
                {
                    child.SortKey = SortKey;
                    child.SortIsAscending = SortIsAscending;
                }
                Children.Sort(new FootageComparer(SortKey, SortIsAscending));
            }
        }

        private void Footage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Enum.TryParse(typeof(FootageSortKey), e.PropertyName, out var changed) && SortKey == (FootageSortKey)changed)
            {
                Children.Sort(new FootageComparer(SortKey, SortIsAscending));
            }
        }

        public void ChangeName(string name)
        {
            if (Name != name)
            {
                var prevName = Name;
                Name = name;
                HistoryModel.Add(IFootageModel.CreateChangeNameHistory(this, prevName, name));
            }
        }

        public void ChangeComment(string comment)
        {
            if (Comment != comment)
            {
                var prevComment = Comment;
                Comment = comment;
                HistoryModel.Add(IFootageModel.CreateChangeCommentHistoryCommand(this, prevComment, comment));
            }
        }

        public FootageData SaveData()
        {
            return new FootageData
            {
                DataType = FootageDataType.Folder,
                FootageId = FootageId,
                Name = Name,
                Comment = Comment,
                Children = Children.Select(m => m.SaveData()).ToArray()
            };
        }
    }
}

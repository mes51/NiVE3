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

        double Duration { get; }

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
    }

    class FootageModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; }

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            private set { SetProperty(ref width, value); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            private set { SetProperty(ref height, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            private set { SetProperty(ref frameRate, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            private set { SetProperty(ref duration, value); }
        }

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            private set { SetProperty(ref filePath, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private SourceType inputType;
        public SourceType InputType
        {
            get { return inputType; }
            set { SetProperty(ref inputType, value); }
        }

        private FootageSortKey sortKey;
        public FootageSortKey SortKey
        {
            get { return sortKey; }
            set { SetProperty(ref sortKey, value); }
        }

        private bool sortIsAscending;
        public bool SortIsAscending
        {
            get { return sortIsAscending; }
            set { SetProperty(ref sortIsAscending, value); }
        }

        public string FileName => Path.GetFileName(InputModel.FilePath);

        public bool IsPlaceholder => InputModel.IsPlaceholder;

        public bool IsCustomizableFootageSource => Source is ICustomizableFootageSource;

        public InputModel InputModel { get; }

        public ObservableCollection<IFootageModel>? Children => null;

        WeakEventPublisher<EventArgs> UpdateSampleImageRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> UpdateSampleImageRequest
        {
            add { UpdateSampleImageRequestPublisher.Subscribe(value); }
            remove { UpdateSampleImageRequestPublisher.Unsubscribe(value); }
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

        public SourceFootageRect CalcSize(double time, int compositionWidth, int compositionHeight, PropertyValueGroup? properties)
        {
            if (properties != null && Source is ICustomizableFootageSource customizableFootageSource)
            {
                return customizableFootageSource.CalcSize(time, compositionWidth, compositionHeight, properties);
            }
            else
            {
                return new SourceFootageRect(Vector2d.Zero, Width, Height);
            }
        }

        public NImage ReadImage(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup? properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            try
            {
                if (properties != null && Source is ICustomizableFootageSource customizableFootageSource)
                {
                    return customizableFootageSource.ReadFrame(time, downSamplingRate, compositionWidth, compositionHeight, properties, imageInterpolationQuality, toGpu && IsSupportLoadToGpu);
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

        public float[] ReadAudio(double time, double length)
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
                InputOption = InputModel.Input.SaveData(),
                SourceId = Source.SourceId
            };
        }

        private void Composition_CompositionUpdated(object? sender, EventArgs e)
        {
            UpdateSampleImageRequestPublisher.Publish(this, EventArgs.Empty);
        }
    }

    class FootageFolderModel : BindableBase, IFootageModel
    {
        public Guid FootageId { get; private set; }

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public int Width => 0;

        public int Height => 0;

        public double FrameRate => 0.0;

        public double Duration => 0.0;

        public string FilePath => "";

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        public string FileName => "";

        public SourceType InputType => SourceType.None;

        private FootageSortKey sortKey;
        public FootageSortKey SortKey
        {
            get { return sortKey; }
            set { SetProperty(ref sortKey, value); }
        }

        private bool sortIsAscending;
        public bool SortIsAscending
        {
            get { return sortIsAscending; }
            set { SetProperty(ref sortIsAscending, value); }
        }

        private ObservableCollection<IFootageModel> children = [];
        public ObservableCollection<IFootageModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

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

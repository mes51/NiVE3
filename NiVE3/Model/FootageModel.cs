using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
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

        public InputModel InputModel { get; }

        public ObservableCollection<IFootageModel>? Children => null;

        IFootageSource Source { get; }

        HistoryModel HistoryModel { get; }

        public FootageModel(InputModel input, IFootageSource source, HistoryModel historyModel) : this(input, source, historyModel, null) { }

        public FootageModel(InputModel input, IFootageSource source, HistoryModel historyModel, Guid? footageId)
        {
            HistoryModel = historyModel;
            FootageId = footageId ?? Guid.NewGuid();
            InputModel = input;
            Source = source;
            Name = Path.GetFileName(input.FilePath);
            Width = source.Width;
            Height = source.Height;
            FrameRate = source.FrameRate;
            Duration = source.Duration;
            FilePath = input.FilePath;
            InputType = source.SourceType;
        }

        public void AddFootage(IFootageModel footage) { }

        public void RemoveFootage(IFootageModel footage) { }

        public NImage ReadImage(double time, bool toGpu)
        {
            return Source.Read(time, toGpu);
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

        private ObservableCollection<IFootageModel> children = new ObservableCollection<IFootageModel>();
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
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class LayerModel : BindableBase, IDisposable
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double sourceStartPoint;
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double inPoint;
        public double InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private double outPoint;
        public double OutPoint
        {
            get { return outPoint; }
            set { SetProperty(ref outPoint, value); }
        }

        private bool isEnableTimeRemap;
        public bool IsEnableTimeRemap
        {
            get { return isEnableTimeRemap; }
            set { SetProperty(ref isEnableTimeRemap, value); }
        }

        private SourceType sourceType;
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private Color tagColor = Colors.Red;
        public Color TagColor
        {
            get { return tagColor; }
            set { SetProperty(ref tagColor, value); }
        }

        private bool isEnableVideo;
        public bool IsEnableVideo
        {
            get { return isEnableVideo; }
            set { SetProperty(ref isEnableVideo, value); }
        }

        private bool isEnableAudio;
        public bool IsEnableAudio
        {
            get { return isEnableAudio; }
            set { SetProperty(ref isEnableAudio, value); }
        }

        private bool isEnableSolo;
        public bool IsEnableSolo
        {
            get { return isEnableSolo; }
            set { SetProperty(ref isEnableSolo, value); }
        }

        private bool isLock;
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        public Guid LayerId { get; }

        public string SourceName => FootageModel.Name;

        private ObservableCollection<EffectModel> effects = new ObservableCollection<EffectModel>();
        public ObservableCollection<EffectModel> Effects
        {
            get { return effects; }
            set
            {
                if (effects!= value)
                {
                    effects.CollectionChanged -= Effects_CollectionChanged;
                    value.CollectionChanged += Effects_CollectionChanged;
                }
                SetProperty(ref effects, value);
            }
        }

        public FootageModel FootageModel { get; }

        HistoryModel HistoryModel { get; set; }

        double PrevInPoint { get; set; }

        double PrevOutPoint { get; set; }

        double PrevSourceStartPoint { get; set; }

        public LayerModel(FootageModel footageModel, HistoryModel historyModel) : this(footageModel, historyModel,null) { }

        public LayerModel(FootageModel footageModel, HistoryModel historyModel, Guid? layerId)
        {
            Effects = new ObservableCollection<EffectModel>();
            FootageModel = footageModel;
            HistoryModel = historyModel;
            Name = footageModel.Name;
            Duration = footageModel.Duration;
            OutPoint = footageModel.Duration;
            SourceType = footageModel.InputType;
            LayerId = layerId ?? Guid.NewGuid();

            IsEnableVideo = SourceType.HasFlag(SourceType.Video) || SourceType.HasFlag(SourceType.Image);
            IsEnableAudio = SourceType.HasFlag(SourceType.Audio);
        }

        public void BeginEditDuration()
        {
            PrevInPoint = InPoint;
            PrevOutPoint = OutPoint;
            PrevSourceStartPoint = SourceStartPoint;
        }

        public void CommitEditDuration()
        {
            if (PrevInPoint != inPoint || PrevOutPoint != OutPoint || PrevSourceStartPoint != SourceStartPoint)
            {
                HistoryModel.Add(new EditDurationHistoryCommand(this, PrevInPoint, PrevOutPoint, PrevSourceStartPoint, InPoint, OutPoint, SourceStartPoint));
            }
        }

        public void ChangeName(string name)
        {
            if (Name != name)
            {
                var prevName = Name;
                Name = name;
                HistoryModel.Add(new ChangeNameHistoryCommand(this, prevName, name));
            }
        }

        public void ChangeComment(string comment)
        {
            if (Comment != comment)
            {
                var prevComment = Comment;
                Comment = comment;
                HistoryModel.Add(new ChangeCommentHistoryCommand(this, prevComment, comment));
            }
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public void Dispose()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class RenderQueueItemModel : BindableBase, IDisposable
    {
        private bool isRenderSelected;
        public bool IsRenderSelected
        {
            get { return isRenderSelected; }
            set { SetProperty(ref isRenderSelected, value); }
        }

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private RenderRangeType renderRangeType;
        public RenderRangeType RenderRangeType
        {
            get { return renderRangeType; }
            set { SetProperty(ref renderRangeType, value); }
        }

        private double beginTime;
        public double BeginTime
        {
            get { return beginTime; }
            set { SetProperty(ref beginTime, value); }
        }

        private double endTime;
        public double EndTime
        {
            get { return endTime; }
            set { SetProperty(ref endTime, value); }
        }

        private double fixedBeginTime;
        public double FixedBeginTime
        {
            get { return fixedBeginTime; }
            set { SetProperty(ref fixedBeginTime, value); }
        }

        private double fixedEndTime;
        public double FixedEndTime
        {
            get { return fixedEndTime; }
            set { SetProperty(ref fixedEndTime, value); }
        }

        private bool isOutputVideo;
        public bool IsOutputVideo
        {
            get { return isOutputVideo; }
            set { SetProperty(ref isOutputVideo, value); }
        }

        private bool isOutputAudio;
        public bool IsOutputAudio
        {
            get { return isOutputAudio; }
            set { SetProperty(ref isOutputAudio, value); }
        }

        private Guid outputPluginId;
        public Guid OutputPluginId
        {
            get { return outputPluginId; }
            set { SetProperty(ref outputPluginId, value); }
        }

        private string outputPluginName = "";
        public string OutputPluginName
        {
            get { return outputPluginName; }
            set { SetProperty(ref outputPluginName, value); }
        }

        private ExportLifetimeContext<IOutput>? output;
        public ExportLifetimeContext<IOutput>? Output
        {
            get { return output; }
            set { SetProperty(ref output, value); }
        }

        private RenderQueueItemState state;
        public RenderQueueItemState State
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        private string compositionName = "";
        public string CompositionName
        {
            get { return compositionName; }
            set { SetProperty(ref compositionName, value); }
        }

        private double compositionWorkareaBegin;
        public double CompositionWorkareaBegin
        {
            get { return compositionWorkareaBegin; }
            set { SetProperty(ref compositionWorkareaBegin, value); }
        }

        private double compositionWorkareaEnd;
        public double CompositionWorkareaEnd
        {
            get { return compositionWorkareaEnd; }
            set { SetProperty(ref compositionWorkareaEnd, value); }
        }

        private double compositionDuration;
        public double CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private double compositionFrameRate;
        public double CompositionFrameRate
        {
            get { return compositionFrameRate; }
            set { SetProperty(ref compositionFrameRate, value); }
        }

        public CompositionModel CompositionModel { get; }

        ProjectModel ProjectModel { get; }

        OutputListModel OutputListModel { get; }

        public RenderQueueItemModel(CompositionModel compositionModel, ProjectModel projectModel, OutputListModel outputListModel, ExportLifetimeContext<IOutput>? output)
        {
            CompositionModel = compositionModel;
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            Output = output;
            if (output != null)
            {
                OutputPluginId = outputListModel.GetId(output.Value.GetType());
                var metadata = outputListModel.GetMetadata(OutputPluginId);
                OutputPluginName = metadata?.Name ?? "";
            }

            CompositionName = compositionModel.Name;
            CompositionWorkareaBegin = compositionModel.WorkareaBegin;
            CompositionWorkareaEnd = compositionModel.WorkareaEnd;
            CompositionDuration = compositionModel.Duration;
            CompositionFrameRate = compositionModel.FrameRate;
            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;

            PropertyChanged += RenderQueueItemModel_PropertyChanged;
        }

        public void UpdateSetting(string filePath, RenderRangeType renderRangeType, double beginTime, double endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput> output)
        {
            FilePath = filePath;
            RenderRangeType = renderRangeType;
            BeginTime = beginTime;
            EndTime = endTime;
            IsOutputVideo = isOutputVideo;
            IsOutputAudio = isOutputAudio;
            if (output != Output)
            {
                Output?.Dispose();
                Output = output;

                OutputPluginId = OutputListModel.GetId(output.Value.GetType());
                var metadata = OutputListModel.GetMetadata(OutputPluginId);
                OutputPluginName = metadata?.Name ?? "";
            }
        }

        public string GetSaveFileFilter()
        {
            if (OutputPluginId != Guid.Empty)
            {
                var supportedExtensions = OutputListModel.GetMetadata(OutputPluginId)?.SupportedFileType ?? "*.*";
                return string.Join("|", supportedExtensions.Split(',').Select(e => e + "|" + e));
            }
            else
            {
                return "*.*|*.*";
            }
        }

        (double beginTime, double endTime) GetTimeRange()
        {
            return RenderRangeType switch
            {
                RenderRangeType.All => (0.0, CompositionDuration),
                RenderRangeType.Workarea => (CompositionWorkareaBegin, CompositionWorkareaEnd),
                _ => (BeginTime, EndTime)
            };
        }

        private void CompositionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CompositionModel.Name):
                    CompositionName = CompositionModel.Name;
                    break;
                case nameof(CompositionModel.WorkareaBegin):
                    CompositionWorkareaBegin = CompositionModel.WorkareaBegin;
                    break;
                case nameof(CompositionModel.WorkareaEnd):
                    CompositionWorkareaEnd = CompositionModel.WorkareaEnd;
                    break;
                case nameof(CompositionModel.Duration):
                    CompositionDuration = CompositionModel.Duration;
                    break;
                case nameof(CompositionModel.FrameRate):
                    CompositionFrameRate = CompositionModel.FrameRate;
                    break;
            }
        }

        private void RenderQueueItemModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(State) when State != RenderQueueItemState.NotReady && State != RenderQueueItemState.Ready:
                    var (beginTime, endTime) = GetTimeRange();
                    FixedBeginTime = beginTime;
                    FixedEndTime = endTime;
                    break;
            }

            if (State != RenderQueueItemState.NotReady && State != RenderQueueItemState.Ready)
            {
                return;
            }

            State = !string.IsNullOrEmpty(FilePath) && Output != null ? RenderQueueItemState.Ready : RenderQueueItemState.NotReady;
        }

        public void Dispose()
        {
            Output?.Dispose();
        }

        ~RenderQueueItemModel()
        {
            Dispose();
        }
    }

    public enum RenderRangeType
    {
        All,
        Workarea,
        Specific
    }

    public enum RenderQueueItemState
    {
        NotReady,
        Ready,
        Rendering,
        Completed,
        Aborted,
        Error
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Data.Json.Project;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class RenderQueueItemModel : BindableBase, IDisposable
    {
        private Guid queueId;
        public Guid QueueId
        {
            get { return queueId; }
            set { SetProperty(ref queueId, value); }
        }

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

        private Time beginTime;
        public Time BeginTime
        {
            get { return beginTime; }
            set { SetProperty(ref beginTime, value); }
        }

        private Time endTime;
        public Time EndTime
        {
            get { return endTime; }
            set { SetProperty(ref endTime, value); }
        }

        private Time fixedBeginTime;
        public Time FixedBeginTime
        {
            get { return fixedBeginTime; }
            set { SetProperty(ref fixedBeginTime, value); }
        }

        private Time fixedEndTime;
        public Time FixedEndTime
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

        private TimeSpan renderingTime;
        public TimeSpan RenderingTime
        {
            get { return renderingTime; }
            set { SetProperty(ref renderingTime, value); }
        }

        private string compositionName = "";
        public string CompositionName
        {
            get { return compositionName; }
            set { SetProperty(ref compositionName, value); }
        }

        private Time compositionWorkareaBegin;
        public Time CompositionWorkareaBegin
        {
            get { return compositionWorkareaBegin; }
            set { SetProperty(ref compositionWorkareaBegin, value); }
        }

        private Time compositionWorkareaEnd;
        public Time CompositionWorkareaEnd
        {
            get { return compositionWorkareaEnd; }
            set { SetProperty(ref compositionWorkareaEnd, value); }
        }

        private Time compositionDuration;
        public Time CompositionDuration
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

        HistoryModel HistoryModel { get; }

        public RenderQueueItemModel(CompositionModel compositionModel, ProjectModel projectModel, OutputListModel outputListModel, HistoryModel historyModel, ExportLifetimeContext<IOutput>? output, Guid? queueId = null)
        {
            CompositionModel = compositionModel;
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            HistoryModel = historyModel;
            Output = output;
            if (output != null)
            {
                OutputPluginId = outputListModel.GetId(output.Value.GetType());
                var metadata = outputListModel.GetMetadata(OutputPluginId);
                OutputPluginName = metadata?.Name ?? "";
            }
            QueueId = queueId ?? Guid.NewGuid();

            CompositionName = compositionModel.Name;
            CompositionWorkareaBegin = compositionModel.WorkareaBegin;
            CompositionWorkareaEnd = compositionModel.WorkareaEnd;
            CompositionDuration = compositionModel.Duration;
            CompositionFrameRate = compositionModel.FrameRate;
            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;

            PropertyChanged += RenderQueueItemModel_PropertyChanged;
        }

        public void UpdateSetting(string filePath, RenderRangeType renderRangeType, Time beginTime, Time endTime, bool isOutputVideo, bool isOutputAudio, object? prevOutputSetting, ExportLifetimeContext<IOutput> output)
        {
            var prevFilePath = FilePath;
            var prevRenderRangeType = RenderRangeType;
            var prevBeginTime = BeginTime;
            var prevEndTime = EndTime;
            var prevIsOutputVideo = IsOutputVideo;
            var prevIsOutputAudio = IsOutputAudio;

            FilePath = filePath;
            RenderRangeType = renderRangeType;
            BeginTime = beginTime;
            EndTime = endTime;
            IsOutputVideo = isOutputVideo;
            IsOutputAudio = isOutputAudio;
            if (output != Output)
            {
                var prevOutput = Output;
                var prevOutputMetadata = Output != null ? OutputListModel.GetMetadata(OutputPluginId) : null;

                Output = output;
                OutputPluginId = OutputListModel.GetId(output.Value.GetType());
                var metadata = OutputListModel.GetMetadata(OutputPluginId);
                OutputPluginName = metadata?.Name ?? "";

                HistoryModel.Add(
                    new ChangeSettingWithNewOutputHistoryCommand(
                        this,
                        prevFilePath,
                        prevRenderRangeType,
                        prevBeginTime,
                        prevEndTime,
                        prevIsOutputVideo,
                        prevIsOutputAudio,
                        prevOutputMetadata,
                        prevOutput,
                        filePath,
                        renderRangeType,
                        beginTime,
                        endTime,
                        isOutputVideo,
                        isOutputAudio,
                        metadata,
                        output
                    )
                );
            }
            else
            {
                HistoryModel.Add(
                    new ChangeSettingHistoryCommand(
                        this,
                        prevFilePath,
                        prevRenderRangeType,
                        prevBeginTime,
                        prevEndTime,
                        prevIsOutputVideo,
                        prevIsOutputAudio,
                        prevOutputSetting,
                        filePath,
                        renderRangeType,
                        beginTime,
                        endTime,
                        isOutputVideo,
                        isOutputAudio,
                        output?.Value?.SaveSetting()
                    )
                );
            }
        }

        public void ChangeFilePath(string newFilePath)
        {
            var prevFilePath = FilePath;
            if (Output != null)
            {
                FilePath = Output.Value.ProcessOutputFilePath(newFilePath);
            }
            else
            {
                FilePath = newFilePath;
            }

            HistoryModel.Add(new ChangeFilePathHistoryCommand(this, prevFilePath, FilePath));
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

        public void ExecuteRender(Action<int> setTotalFrameCount, Action<int, TimeSpan> setProgress, Func<bool> isPaused, Func<bool> isAborting)
        {
            if (Output == null)
            {
                return;
            }

            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => State = RenderQueueItemState.Rendering);

            var plugin = Output.Value;
            var size = isOutputVideo ? new Int32Size(CompositionModel.Width, CompositionModel.Height) : (Int32Size?)null;
            var sourceTypes = (isOutputVideo ? SourceType.Video : SourceType.None) | (CompositionModel.HasAudio && isOutputAudio ? SourceType.Audio : SourceType.None);
            var frameRate = CompositionModel.FrameRate;
            var frameDuration = CompositionModel.FrameDuration;
            var (beginTime, endTime) = GetTimeRange();

            var startRenderingTimestamp = Stopwatch.GetTimestamp();
            try
            {
                plugin.BeginOutput(filePath, beginTime, endTime - beginTime, frameRate, size, sourceTypes);

                var lastProcessedDuration = Time.Zero;
                if (sourceTypes.HasFlag(SourceType.Video))
                {
                    var passCount = plugin.GetPassCount();
                    var frameCount = (int)Math.Ceiling((double)(endTime - beginTime) * frameRate);
                    var totalFrameCount = frameCount * passCount;
                    setTotalFrameCount(totalFrameCount);
                    for (var pass = 0; pass < passCount && !isAborting(); pass++)
                    {
                        plugin.BeginPass(pass);
                        for (var i = 0; i < frameCount; i++)
                        {
                            while (isPaused() && !isAborting())
                            {
                                Thread.Sleep(100);
                            }
                            if (isAborting())
                            {
                                break;
                            }

                            var startTimestamp = Stopwatch.GetTimestamp();
                            var useGpu = ProjectModel.UseGpu;
                            var time = beginTime + new Time(i, frameRate);
                            using var checker = CycleChecker.StartCheck();
                            using var image = CompositionModel.RenderFrame(time, 1.0, true, useGpu);
                            plugin.ProcessFrame(pass, time, image, useGpu);
                            setProgress(i + 1 + frameCount * pass, Stopwatch.GetElapsedTime(startTimestamp));
                            lastProcessedDuration = new Time(i + 1, frameRate);
                        }
                        plugin.EndPass();
                    }
                }
                else
                {
                    lastProcessedDuration = endTime - beginTime;
                }

                if (sourceTypes.HasFlag(SourceType.Audio))
                {
                    var audio = CompositionModel.RenderAudio(beginTime, lastProcessedDuration);
                    plugin.ProcessAudio(audio);
                }

                plugin.EndOutput();

                if (isAborting())
                {
                    dispatcher.Invoke(() => UpdateState(RenderQueueItemState.Aborted));
                }
                else
                {
                    dispatcher.Invoke(() => UpdateState(RenderQueueItemState.Completed));
                }
            }
            catch
            {
                dispatcher.Invoke(() => UpdateState(RenderQueueItemState.Error));
                throw;
            }
            finally
            {
                RenderingTime = Stopwatch.GetElapsedTime(startRenderingTimestamp);
            }
        }

        public RenderQueueItemData SaveData()
        {
            return new RenderQueueItemData
            {
                QueueId = QueueId,
                IsRenderSelected = IsRenderSelected,
                FilePath = FilePath,
                RenderRangeType = RenderRangeType,
                BeginTime = BeginTime,
                EndTime = EndTime,
                FixedBeginTime = FixedBeginTime,
                FixedEndTime = FixedEndTime,
                IsOutputVideo = IsOutputVideo,
                IsOutputAudio = IsOutputAudio,
                OutputPluginId = OutputPluginId,
                OutputSetting = Output?.Value?.SaveSetting(),
                State = State,
                RenderingTime = RenderingTime,
                CompositionId = CompositionModel.CompositionId
            };
        }

        public void LoadData(RenderQueueItemData data)
        {
            IsRenderSelected = data.IsRenderSelected;
            FilePath = data.FilePath;
            RenderRangeType = data.RenderRangeType;
            BeginTime = data.BeginTime;
            EndTime = data.EndTime;
            FixedBeginTime = data.FixedBeginTime;
            FixedEndTime = data.FixedEndTime;
            IsOutputVideo = data.IsOutputVideo;
            IsOutputAudio = data.IsOutputAudio;
            OutputPluginId = data.OutputPluginId;
            State = data.State;
            RenderingTime = data.RenderingTime;

            var output = OutputListModel.CreateOutput(OutputPluginId);
            if (output != null)
            {
                output.Value.LoadSetting(data.OutputSetting);
                Output = output;
                var metadata = OutputListModel.GetMetadata(OutputPluginId);
                OutputPluginName = metadata?.Name ?? "";
            }
        }

        void UpdateState(RenderQueueItemState state)
        {
            State = state;

            HistoryModel.Add(new UpdateStateFromReadyHistoryCommand(this, state));
        }

        (Time beginTime, Time endTime) GetTimeRange()
        {
            switch (RenderRangeType)
            {
                case RenderRangeType.All:
                    return (Time.Zero, CompositionDuration);
                case RenderRangeType.Workarea:
                    return (CompositionWorkareaBegin, CompositionWorkareaEnd);
                default:
                    {
                        var beginTime = Time.Clamp(BeginTime, Time.Zero, CompositionDuration - CompositionModel.FrameDuration);
                        var endTime = Time.Clamp(EndTime, beginTime + CompositionModel.FrameDuration, CompositionDuration);
                        return (beginTime, endTime);
                    }
            }
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public Guid QueueId { get; }

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
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

        private double compositionDuration;
        public double CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private double frameDuration;
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private bool useRenderQueueItemTimeRange;
        public bool UseRenderQueueItemTimeRange
        {
            get { return useRenderQueueItemTimeRange; }
            set { SetProperty(ref useRenderQueueItemTimeRange, value); }
        }

        private string compositionName = "";
        public string CompositionName
        {
            get { return compositionName; }
            set { SetProperty(ref compositionName, value); }
        }

        private RenderQueueItemState state;
        public RenderQueueItemState State
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        private ObservableCollection<Tuple<Guid, string>> outputPlugins = [];
        public ObservableCollection<Tuple<Guid, string>> OutputPlugins
        {
            get { return outputPlugins; }
            set { SetProperty(ref outputPlugins, value); }
        }

        private Guid selectedOutputPluginId;
        public Guid SelectedOutputPluginId
        {
            get { return selectedOutputPluginId; }
            set { SetProperty(ref selectedOutputPluginId, value); }
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

        private bool hasOutputSetting;
        public bool HasOutputSetting
        {
            get { return hasOutputSetting; }
            set { SetProperty(ref hasOutputSetting, value); }
        }

        public CompositionModel CompositionModel { get; }

        OutputListModel OutputListModel { get; }

        HistoryModel HistoryModel { get; }

        ExportLifetimeContext<IOutput> Output { get; set; }

        object? OutputData { get; set; }

        public RenderQueueItemModel(OutputListModel outputListModel, CompositionModel compositionModel, HistoryModel historyModel, string baseFilePath, Guid? queueId)
        {
            OutputListModel = outputListModel;
            CompositionModel = compositionModel;
            HistoryModel = historyModel;
            QueueId = queueId ?? Guid.NewGuid();
            BeginTime = compositionModel.WorkareaBegin;
            EndTime = CompositionModel.WorkareaEnd;
            State = RenderQueueItemState.Ready;
            CompositionName = compositionModel.Name;
            CompositionDuration = compositionModel.Duration;
            FrameRate = compositionModel.FrameRate;
            FrameDuration = compositionModel.FrameDuration;

            OutputPlugins = [..outputListModel.OutputMetadatas.Values.Select(m => Tuple.Create(Guid.Parse(m.OutputUuid), m.Name))];
            SelectedOutputPluginId = OutputPlugins.FirstOrDefault()?.Item1 ?? Guid.Empty;

            compositionModel.PropertyChanged += CompositionModel_PropertyChanged;

            var output = OutputListModel.CreateOutput(SelectedOutputPluginId);
            if (output == null)
            {
                // NOTE: 出力プラグイン0個の場合はQueue側で作らせないようにする
                throw new InvalidOperationException();
            }

            Output = output;
            FilePath = Output.Value.ProcessOutputFilePath(baseFilePath);
            var metadata = outputListModel.OutputMetadatas[output.Value.GetType()];
            HasOutputSetting = metadata.HasSettingView;
        }

        public string GetSaveFileFilter()
        {
            var supportedExtensions = OutputListModel.GetMetadata(SelectedOutputPluginId)?.SupportedFileType ?? "*.*";
            return string.Join("|", supportedExtensions.Split(',').Select(e => e + "|" + e));
        }

        public void ChangeOutputPlugin(Guid newOutputPluginId)
        {
            Output?.Dispose();
            SelectedOutputPluginId = newOutputPluginId;
            var output = OutputListModel.CreateOutput(newOutputPluginId);
            if (output == null)
            {
                throw new InvalidOperationException();
            }
            Output = output;
            HasOutputSetting = OutputListModel.GetMetadata(newOutputPluginId)?.HasSettingView ?? false;

            // TODO: ヒストリに積む
        }

        public void ChangeFilePath(string newFilePath)
        {
            FilePath = Output.Value.ProcessOutputFilePath(newFilePath);

            // TODO: ヒストリに積む
        }

        private void CompositionModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CompositionModel.Name):
                    CompositionName = CompositionModel.Name;
                    break;
                case nameof(CompositionModel.WorkareaBegin) when !UseRenderQueueItemTimeRange:
                case nameof(CompositionModel.WorkareaEnd) when !UseRenderQueueItemTimeRange:
                    BeginTime = CompositionModel.WorkareaBegin;
                    EndTime = CompositionModel.WorkareaEnd;
                    break;
                case nameof(CompositionModel.Duration):
                    CompositionDuration = CompositionModel.Duration;
                    break;
                case nameof(CompositionModel.FrameDuration):
                    FrameDuration = CompositionModel.FrameDuration;
                    break;
                case nameof(CompositionModel.FrameRate):
                    FrameRate = CompositionModel.FrameRate;
                    break;
            }
        }

        public void Dispose()
        {
            Output?.Dispose();
        }
    }

    enum RenderQueueItemState
    {
        NotReady,
        Ready,
        Completed,
        Processing,
        Aborted
    }
}

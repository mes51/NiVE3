using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class RenderQueueModel : BindableBase
    {
        private ObservableCollection<RenderQueueItemModel> items = [];
        public ObservableCollection<RenderQueueItemModel> Items
        {
            get { return items; }
            set { SetProperty(ref items, value); }
        }

        private double progress;
        public double Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        private bool isRendering;
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        private bool isAborting;
        public bool IsAborting
        {
            get { return isAborting; }
            set { SetProperty(ref isAborting, value); }
        }

        private TimeSpan eta;
        public TimeSpan Eta
        {
            get { return eta; }
            set { SetProperty(ref eta, value); }
        }

        Lazy<ProjectModel> ProjectModel { get; }

        OutputListModel OutputListModel { get; }

        HistoryModel HistoryModel { get; }

        public RenderQueueModel(Lazy<ProjectModel> projectModel, OutputListModel outputListModel, HistoryModel historyModel)
        {
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            HistoryModel = historyModel;
        }

        public void Enqueue(CompositionModel compositionModel, string filePath, RenderRangeType renderRangeType, double beginTime, double endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput>? output)
        {
            var queue = new RenderQueueItemModel(compositionModel, ProjectModel.Value, OutputListModel, HistoryModel, output);
            queue.FilePath = filePath;
            queue.RenderRangeType = renderRangeType;
            queue.BeginTime = beginTime;
            queue.EndTime = endTime;
            queue.IsOutputVideo = isOutputVideo;
            queue.IsOutputAudio = isOutputAudio;

            Items.Add(queue);

            HistoryModel.Add(new EnqueueHistoryCommand(this, queue));
        }

        public void RemoveQueue(Guid id)
        {
            RemoveQueues([id]);
        }

        public void RemoveQueues(Guid[] ids)
        {
            var targets = Items.Where(m => ids.Contains(m.QueueId)).OrderBy(Items.IndexOf).ToArray();
            var indices = targets.Select(Items.IndexOf).ToArray();

            foreach (var i in targets)
            {
                Items.Remove(i);
            }

            HistoryModel.Add(new RemoveQueuesHistoryCommand(this, targets, indices));
        }
    }
}

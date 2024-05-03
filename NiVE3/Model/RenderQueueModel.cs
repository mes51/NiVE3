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
    class RenderQueueModel : BindableBase
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

        public RenderQueueModel(Lazy<ProjectModel> projectModel, OutputListModel outputListModel)
        {
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
        }

        public void Enqueue(CompositionModel compositionModel, string filePath, RenderRangeType renderRangeType, double beginTime, double endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput>? output)
        {
            var queue = new RenderQueueItemModel(compositionModel, ProjectModel.Value, OutputListModel, output);
            queue.FilePath = filePath;
            queue.RenderRangeType = renderRangeType;
            queue.BeginTime = beginTime;
            queue.EndTime = endTime;
            queue.IsOutputVideo = isOutputVideo;
            queue.IsOutputAudio = isOutputAudio;

            Items.Add(queue);
        }
    }
}

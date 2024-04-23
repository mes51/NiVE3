using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class RenderQueueModel : BindableBase
    {
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

        private double progress;
        public double Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        private ObservableCollection<RenderQueueItemModel> queue = [];
        public ObservableCollection<RenderQueueItemModel> Queue
        {
            get { return queue; }
            set { SetProperty(ref queue, value); }
        }

        OutputListModel OutputListModel { get; }

        HistoryModel HistoryModel { get; }

        Lazy<ProjectModel> LazyProjectModel { get; }

        Task? RenderingTask { get; set; }

        public RenderQueueModel(OutputListModel outputListModel, HistoryModel historyModel, Lazy<ProjectModel> lazyProjectModel)
        {
            OutputListModel = outputListModel;
            HistoryModel = historyModel;
            LazyProjectModel = lazyProjectModel;
        }

        public void Enqueue(CompositionModel compositionModel)
        {
            var projectModel = LazyProjectModel.Value;
            var basePath = string.IsNullOrEmpty(projectModel.ProjectPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(projectModel.ProjectPath);
            basePath = Path.Combine(basePath ?? Path.GetFullPath("."), compositionModel.Name + ".avi");
            var item = new RenderQueueItemModel(OutputListModel, compositionModel, HistoryModel, LazyProjectModel.Value, basePath, null);
            Queue.Add(item);

            HistoryModel.Add(new EnqueueHistoryCommand(this, item));
        }

        public void DeleteQueues(Guid[] queueIds)
        {
            var items = Queue.Where(q => queueIds.Contains(q.QueueId)).OrderBy(Queue.IndexOf).ToArray();
            var indices = items.Select(Queue.IndexOf).ToArray();

            foreach (var item in items)
            {
                Queue.Remove(item);
            }

            HistoryModel.Add(new DeleteRenderQueueHistoryCommand(this, items, indices));
        }

        public void DeleteByComposition(CompositionModel compositionModel)
        {
            var targetIds = Queue.Where(q => q.CompositionModel == compositionModel).Select(q =>q.QueueId).ToArray();
            DeleteQueues(targetIds);
        }

        public void StartRendering()
        {
            IsRendering = true;
            RenderingTask = Task.Run(() =>
            {
                foreach (var item in Queue.Where(q => q.State == RenderQueueItemState.Ready))
                {
                    if (!IsRendering)
                    {
                        break;
                    }
                    try
                    {
                        item.Rendering(() => IsPaused, () => IsAborting || !IsRendering, p => Application.Current.Dispatcher.Invoke(() => Progress = p));
                    }
                    catch
                    {
                        Application.Current.Dispatcher.Invoke(() => item.State = RenderQueueItemState.Error);
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsRendering = false;
                    Progress = 0.0;
                });
            });
        }

        public void StopRendering()
        {
            if (IsRendering)
            {
                IsAborting = true;
                RenderingTask = RenderingTask?.ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Progress = 0;
                        IsPaused = false;
                        IsRendering = false;
                        IsAborting = false;
                    });
                });
            }
        }

        public void PauseRendering()
        {
            if (IsRendering)
            {
                IsPaused = true;
            }
        }

        public void ContinueRendering()
        {
            if (IsPaused)
            {
                IsPaused = false;
            }
        }

        public void Clear()
        {
            Queue.Clear();
        }
    }
}

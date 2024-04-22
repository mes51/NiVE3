using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class RenderQueueModel : BindableBase
    {
        private ObservableCollection<RenderQueueItemModel> queue = [];
        public ObservableCollection<RenderQueueItemModel> Queue
        {
            get { return queue; }
            set { SetProperty(ref queue, value); }
        }

        public event EventHandler<GetProjectPathRequestEventArgs>? GetProjectPathRequest;

        OutputListModel OutputListModel { get; }

        HistoryModel HistoryModel { get; }

        public RenderQueueModel(OutputListModel outputListModel, HistoryModel historyModel)
        {
            OutputListModel = outputListModel;
            HistoryModel = historyModel;
        }

        public void Enqueue(CompositionModel compositionModel)
        {
            var getProjectPathRequestEventArgs = new GetProjectPathRequestEventArgs();
            GetProjectPathRequest?.Invoke(this, getProjectPathRequestEventArgs);
            var basePath = string.IsNullOrEmpty(getProjectPathRequestEventArgs.ProjectPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(getProjectPathRequestEventArgs.ProjectPath);
            basePath = Path.Combine(basePath ?? Path.GetFullPath("."), compositionModel.Name + ".avi");
            var item = new RenderQueueItemModel(OutputListModel, compositionModel, HistoryModel, basePath, null);
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

        public void Clear()
        {
            Queue.Clear();
        }
    }
}

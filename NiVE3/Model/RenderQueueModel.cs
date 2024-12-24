using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Data.Json.Project;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class RenderQueueModel : BindableBase
    {
        const int EtaRingBufferSize = 30;

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

        private int renderedFrameCount;
        public int RenderedFrameCount
        {
            get { return renderedFrameCount; }
            set { SetProperty(ref renderedFrameCount, value); }
        }

        private int totalFrameCount;
        public int TotalFrameCount
        {
            get { return totalFrameCount; }
            set { SetProperty(ref totalFrameCount, value); }
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

        public event EventHandler? RenderQueueItemAdded;

        Lazy<ProjectModel> ProjectModel { get; }

        OutputListModel OutputListModel { get; }

        HistoryModel HistoryModel { get; }

        Task? RenderingTask { get; set; }

        public RenderQueueModel(Lazy<ProjectModel> projectModel, OutputListModel outputListModel, HistoryModel historyModel)
        {
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            HistoryModel = historyModel;
        }

        public void Enqueue(CompositionModel compositionModel, string filePath, RenderRangeType renderRangeType, Time beginTime, Time endTime, bool isOutputVideo, bool isOutputAudio, ExportLifetimeContext<IOutput>? output)
        {
            var duplicateCount = 1;
            while (Items.Any(i => i.FilePath == filePath) || File.Exists(filePath))
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetFileNameWithoutExtension(filePath) + $"_{duplicateCount}" + Path.GetExtension(filePath));
                duplicateCount++;
            }

            var queue = new RenderQueueItemModel(compositionModel, ProjectModel.Value, OutputListModel, HistoryModel, output)
            {
                FilePath = filePath,
                RenderRangeType = renderRangeType,
                BeginTime = beginTime,
                EndTime = endTime,
                IsOutputVideo = isOutputVideo,
                IsOutputAudio = isOutputAudio
            };

            Items.Add(queue);

            HistoryModel.Add(new EnqueueHistoryCommand(this, queue));

            RenderQueueItemAdded?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveQueue(Guid id)
        {
            RemoveQueues([id]);
        }

        public void RemoveQueues(Guid[] ids)
        {
            var targets = Items.Where(i => ids.Contains(i.QueueId)).OrderBy(Items.IndexOf).ToArray();
            var indices = targets.Select(Items.IndexOf).ToArray();

            foreach (var i in targets)
            {
                Items.Remove(i);
            }

            HistoryModel.Add(new RemoveQueuesHistoryCommand(this, targets, indices));
        }

        public void RemoveQueuesByComposition(CompositionModel compositionModel)
        {
            RemoveQueues([..Items.Where(i => i.CompositionModel == compositionModel).Select(i => i.QueueId)]);
        }

        public void Clear()
        {
            foreach (var item in Items)
            {
                item.Dispose();
            }
            Items.Clear();
        }

        public bool HasSameOutputFilePathQueue()
        {
            return Items.Select(i => i.FilePath).Distinct().Count() != Items.Count;
        }

        public void StartRender()
        {
            IsRendering = true;
            ProjectModel.Value.IsRendering = true;

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ExecuteRendering));

            var dispatcher = Application.Current.Dispatcher;
            RenderingTask = Task.Run(() =>
            {
                var frameRenderTimes = new RingBuffer<TimeSpan>(EtaRingBufferSize);

                foreach (var item in (Items.Any(i => i.IsRenderSelected) ? Items.Where(i => i.IsRenderSelected) : Items).Where(i => i.State == RenderQueueItemState.Ready))
                {
                    dispatcher.Invoke(() => TotalFrameCount = 0);
                    frameRenderTimes.Clear();

                    dispatcher.Invoke(() =>
                    {
                        Progress = 0.0;
                        Eta = TimeSpan.Zero;
                    });

                    item.ExecuteRender(
                        f => dispatcher.Invoke(() => TotalFrameCount = f),
                        (renderedFrames, renderTime) =>
                        {
                            frameRenderTimes.Append(renderTime);
                            var eta = TimeSpan.FromSeconds((TotalFrameCount - renderedFrames) * frameRenderTimes.Sum(t => t.TotalSeconds) / frameRenderTimes.Count);
                            dispatcher.Invoke(() =>
                            {
                                RenderedFrameCount = renderedFrames;
                                Progress = renderedFrames / (double)TotalFrameCount * 100.0;
                                Eta = eta;
                            });
                        },
                        () => IsPaused,
                        () => isAborting
                    );

                    if (IsAborting)
                    {
                        break;
                    }
                }
            }).ContinueWith(t =>
            {
                dispatcher.Invoke(() =>
                {
                    IsRendering = false;
                    IsPaused = false;
                    IsAborting = false;
                    Progress = 0.0;
                    Eta = TimeSpan.Zero;
                    TotalFrameCount = 0;
                    RenderedFrameCount = 0;
                    ProjectModel.Value.IsRendering = false;
                    HistoryModel.EndGroup();
                });

                if (t.Exception != null)
                {
                    // TODO: エラーダイアログ表示
                }
            });
        }

        public void AbortRendering()
        {
            IsAborting = true;
        }

        public RenderQueueItemData[] SaveData()
        {
            return [..Items.Select(i => i.SaveData())];
        }

        public void LoadData(RenderQueueItemData[] data, CompositionModel[] compositionModels)
        {
            foreach (var queueData in data)
            {
                var composition = compositionModels.FirstOrDefault(c => c.CompositionId == queueData.CompositionId);
                if (composition == null)
                {
                    continue;
                }

                var item = new RenderQueueItemModel(composition, ProjectModel.Value, OutputListModel, HistoryModel, null, queueData.QueueId);
                item.LoadData(queueData);
                Items.Add(item);
            }
        }
    }
}

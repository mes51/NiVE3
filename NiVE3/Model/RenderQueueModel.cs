using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Data.Json.Project;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.Model
{
    [UseReactiveProperty]
    partial class RenderQueueModel : BindableBase
    {
        [GeneratedRegex("_([0-9]+)", RegexOptions.Compiled)]
        private static partial Regex GenerateDuplicateCountRegex();

        const int EtaRingBufferSize = 30;

        [ReactiveProperty]
        public partial ObservableCollection<RenderQueueItemModel> Items { get; set; } = [];

        [ReactiveProperty]
        public partial double Progress { get; set; }

        [ReactiveProperty]
        public partial int RenderedFrameCount { get; set; }

        [ReactiveProperty]
        public partial int TotalFrameCount { get; set; }

        [ReactiveProperty]
        public partial bool IsRendering { get; set; }

        [ReactiveProperty]
        public partial bool IsPaused { get; set; }

        [ReactiveProperty]
        public partial bool IsAborting { get; set; }

        [ReactiveProperty]
        public partial TimeSpan Eta { get; set; }

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
            var queue = new RenderQueueItemModel(compositionModel, ProjectModel.Value, OutputListModel, HistoryModel, output)
            {
                FilePath = GetDuplicateMarkedFilePath(filePath),
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

        public void DuplicateQueue(Guid[] ids)
        {
            var targets = Items.Where(i => ids.Contains(i.QueueId)).OrderBy(Items.IndexOf).ToArray();
            if (targets.Length < 1)
            {
                return;
            }

            var newQueues = new RenderQueueItemModel[targets.Length];
            for (var i = 0; i < newQueues.Length; i++)
            {
                var target = targets[i];
                var newQueue = new RenderQueueItemModel(target.CompositionModel, ProjectModel.Value, OutputListModel, HistoryModel, OutputListModel.CreateOutput(target.OutputPluginId))
                {
                    FilePath = GetDuplicateMarkedFilePath(target.FilePath),
                    RenderRangeType = target.RenderRangeType,
                    BeginTime = target.BeginTime,
                    EndTime = target.EndTime,
                    IsOutputVideo = target.IsOutputVideo,
                    IsOutputAudio = target.IsOutputAudio
                };
                if (target.Output != null && newQueue.Output != null)
                {
                    newQueue.Output.Value.LoadSetting(target.Output.Value.SaveSetting());
                }

                newQueues[i] = newQueue;
                Items.Add(newQueue);
            }

            HistoryModel.Add(new DuplicateQueuesHistoryCommand(this, newQueues));
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
                        () => IsAborting
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

        string GetDuplicateMarkedFilePath(string filePath)
        {
            var realFileName = Path.GetFileNameWithoutExtension(filePath);
            var duplicateMark = GenerateDuplicateCountRegex().Match(realFileName);
            if (!duplicateMark.Success || !int.TryParse(duplicateMark.Value, out var duplicateCount))
            {
                duplicateCount = 1;
            }
            else
            {
                realFileName = GenerateDuplicateCountRegex().Replace(realFileName, "");
            }

            while (Items.Any(i => i.FilePath == filePath) || File.Exists(filePath))
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", realFileName + $"_{duplicateCount}" + Path.GetExtension(filePath));
                duplicateCount++;
            }

            return filePath;
        }
    }
}

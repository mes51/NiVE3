using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ILGPU.Runtime.Cuda;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class RenderingProgressViewModel : BindableBase, IDialogAware
    {
        public const string CompositionParameterName = nameof(CompositionParameterName);

        public const string FilePathParameterName = nameof(FilePathParameterName);

        public const string BeginTimeParameterName = nameof(BeginTimeParameterName);

        public const string EndTimeParameterName = nameof(EndTimeParameterName);

        public const string IsOutputVideoParameterName = nameof(IsOutputVideoParameterName);

        public const string IsOutputAudioParameterName = nameof(IsOutputAudioParameterName);

        public const string OutputParameterName = nameof(OutputParameterName);

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

        private bool showEta;
        public bool ShowEta
        {
            get { return showEta; }
            set { SetProperty(ref showEta, value); }
        }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.RenderingProgressView_Title);

        public ICommand PauseCommand { get; }

        public ICommand AbortCommand { get; }

        public event Action<IDialogResult>? RequestClose;

        ProjectModel ProjectModel { get; }

        Task? RenderingProcess { get; set; }

        RingBuffer<TimeSpan> FrameRenderTimes { get; } = new RingBuffer<TimeSpan>(30);

        public RenderingProgressViewModel(ProjectModel projectModel)
        {
            ProjectModel = projectModel;

            PauseCommand = new RequerySuggestedCommand(() => IsPaused = !IsPaused, () => !IsAborting);

            AbortCommand = new RequerySuggestedCommand(() => IsAborting = true, () => !IsAborting);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            IsAborting = true;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var compositionModel = parameters.GetValue<CompositionModel>(CompositionParameterName);
            var filePath = parameters.GetValue<string>(FilePathParameterName);
            var beginTime = parameters.GetValue<double>(BeginTimeParameterName);
            var endTime = parameters.GetValue<double>(EndTimeParameterName);
            var isOutputVideo = parameters.GetValue<bool>(IsOutputVideoParameterName);
            var isOutputAudio = parameters.GetValue<bool>(IsOutputAudioParameterName);
            var output = parameters.GetValue<ExportLifetimeContext<IOutput>>(OutputParameterName);

            var plugin = output.Value;
            var size = isOutputVideo ? new Int32Size(compositionModel.Width, compositionModel.Height) : (Int32Size?)null;
            var sourceTypes = (isOutputVideo ? SourceType.Video : SourceType.None) | (compositionModel.HasAudio && isOutputAudio ? SourceType.Audio : SourceType.None);
            var frameRate = compositionModel.FrameRate;
            var frameDuration = compositionModel.FrameDuration;
            var dispatcher = Application.Current.Dispatcher;

            RenderingProcess = Task.Run(() =>
            {
                plugin.BeginOutput(filePath, beginTime, endTime - beginTime, frameRate, size, sourceTypes);

                var lastProcessedDuration = 0.0;
                if (sourceTypes.HasFlag(SourceType.Video))
                {
                    var passCount = plugin.GetPassCount();
                    var frameCount = (int)Math.Ceiling((endTime - beginTime) * frameRate);
                    var totalFrameCount = frameCount * passCount;
                    dispatcher.Invoke(() => TotalFrameCount = totalFrameCount);
                    for (var pass = 0; pass < passCount && !IsAborting; pass++)
                    {
                        plugin.BeginPass(pass);
                        for (var i = 0; i < frameCount; i++)
                        {
                            while (IsPaused && !IsAborting)
                            {
                                Thread.Sleep(100);
                            }
                            if (IsAborting)
                            {
                                break;
                            }

                            var startTimestamp = Stopwatch.GetTimestamp();
                            var useGpu = ProjectModel.UseGpu;
                            var time = TimeCalc.RoundTimeDigit(beginTime + i * frameDuration);
                            using var image = compositionModel.RenderFrame(time, 1.0, true, useGpu);
                            plugin.ProcessFrame(pass, time, image, useGpu);
                            FrameRenderTimes.Append(Stopwatch.GetElapsedTime(startTimestamp));
                            dispatcher.Invoke(() =>
                            {
                                var renderedFrames = i + 1 + frameCount * pass;
                                Progress = renderedFrames / (double)totalFrameCount * 100.0;
                                RenderedFrameCount = renderedFrames;
                                Eta = TimeSpan.FromSeconds((totalFrameCount - renderedFrames) * FrameRenderTimes.Sum(t => t.TotalSeconds) / FrameRenderTimes.Count);
                                ShowEta = true;
                            });
                            lastProcessedDuration = (i + 1) * frameDuration;
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
                    var audio = compositionModel.RenderAudio(beginTime, lastProcessedDuration);
                    plugin.ProcessAudio(audio);
                }

                plugin.EndOutput();
            }).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    // TODO: エラーダイアログ表示
                }

                output.Dispose();
                dispatcher.BeginInvoke(() => RequestClose?.Invoke(new DialogResult(ButtonResult.OK, null)));
            });
        }
    }
}

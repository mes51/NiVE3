using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Mvvm;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using NiVE3.ViewModel;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.Model.UI
{
    [UseReactiveProperty]
    partial class PlayControllerModel : BindableBase, IDisposable
    {
        const int RealFrameRateAvgCount = 5;

        [ReactiveProperty]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        public partial Time WorkareaBegin { get; set; }

        [ReactiveProperty]
        public partial Time WorkareaEnd { get; set; }

        [ReactiveProperty]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        public partial bool IsPlaying { get; set; }

        [ReactiveProperty]
        public partial bool IsPaused { get; set; }

        [ReactiveProperty]
        public partial bool IsRenderingRamPreview { get; set; }

        [ReactiveProperty]
        public partial bool UseRamPreview { get; set; }

        [ReactiveProperty]
        public partial Time RamPreviewRenderedWorkareaEnd { get; set; }

        [ReactiveProperty]
        public partial double RealFrameRate { get; set; } = -1.0;

        public Time FrameDuration => FrameRate > 0.0 ? new Time(1, FrameRate) : Time.Zero;

        public bool CanPreview => FrameRate > 0.0 && Duration > 0.0 && WorkareaEnd - WorkareaBegin > FrameDuration;

        WeakEventPublisher<EventArgs> ChangeFrameRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> ChangeFrameRequest
        {
            add { ChangeFrameRequestPublisher.Subscribe(value); }
            remove { ChangeFrameRequestPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> PreviewPlayPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> PreviewPlay
        {
            add { PreviewPlayPublisher.Subscribe(value); }
            remove { PreviewPlayPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> StoppedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> Stopped
        {
            add { StoppedPublisher.Subscribe(value); }
            remove { StoppedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> PauseChangedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> PauseChanged
        {
            add { PauseChangedPublisher.Subscribe(value); }
            remove { PauseChangedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> StartRenderRamPreviewPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> StartRenderRamPreview
        {
            add { StartRenderRamPreviewPublisher.Subscribe(value); }
            remove { StartRenderRamPreviewPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<StopRenderRamPreviewEventArgs> StopRenderRamPreviewPublisher { get; } = new WeakEventPublisher<StopRenderRamPreviewEventArgs>();
        public event EventHandler<StopRenderRamPreviewEventArgs> StopRenderRamPreview
        {
            add { StopRenderRamPreviewPublisher.Subscribe(value); }
            remove { StopRenderRamPreviewPublisher.Unsubscribe(value); }
        }

        ViewStateModel ViewState { get; }

        RingBuffer<TimeSpan> FrameRenderingTimes { get; } = new RingBuffer<TimeSpan>(RealFrameRateAvgCount);

        MultiMediaTimer Timer { get; }

        long? LastUpdateCurrentTime { get; set; }

        public PlayControllerModel(HistoryModel historyModel, ViewStateModel viewState)
        {
            ViewState = viewState;

            Timer = new MultiMediaTimer();
            Timer.Tick += Timer_Tick;

            PropertyChanged += PlayControllerModel_PropertyChanged;
            historyModel.HistoryChanged += HistoryModel_HistoryChanged;
            historyModel.HistoryGroupChanging += HistoryModel_HistoryGroupChanging;
        }

        public void Play()
        {
            if (IsPlaying && !IsPaused || !CanPreview)
            {
                return;
            }

            if (UseRamPreview)
            {
                if (!IsRenderingRamPreview)
                {
                    IsRenderingRamPreview = true;
                    StartRenderRamPreviewPublisher.Publish(this, EventArgs.Empty);
                    return;
                }
                else
                {
                    var eventArgs = new StopRenderRamPreviewEventArgs();
                    StopRenderRamPreviewPublisher.Publish(this, eventArgs);
                    IsRenderingRamPreview = false;
                    RamPreviewRenderedWorkareaEnd = WorkareaBegin + eventArgs.RenderedFrameCount * FrameDuration;

                    if (RamPreviewRenderedWorkareaEnd - WorkareaBegin < FrameDuration * 2.0)
                    {
                        return;
                    }

                    IsPlaying = true;
                    IsPaused = false;
                    LastUpdateCurrentTime = null;
                    FrameRenderingTimes.Clear();
                    RealFrameRate = -1.0;
                    PreviewPlayPublisher.Publish(this, EventArgs.Empty);
                    Timer.Interval = (double)FrameDuration * 1000.0;
                    Timer.Start();
                }
            }
            else
            {
                IsPlaying = true;
                IsPaused = false;
                LastUpdateCurrentTime = null;
                FrameRenderingTimes.Clear();
                RealFrameRate = -1.0;
                PreviewPlayPublisher.Publish(this, EventArgs.Empty);
                Timer.Interval = (double)FrameDuration * 1000.0;
                Timer.Start();
            }
        }

        public void Pause()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPaused = !IsPaused;
            if (IsPaused)
            {
                Timer.Stop();
            }
            else
            {
                Timer.Start();
                LastUpdateCurrentTime = null;
                FrameRenderingTimes.Clear();
            }

            PauseChangedPublisher.Publish(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (IsRenderingRamPreview)
            {
                IsRenderingRamPreview = false;
                StopRenderRamPreviewPublisher.Publish(this, new StopRenderRamPreviewEventArgs());
            }
            else
            {
                IsPlaying = false;
                IsPaused = false;
                Timer.Stop();
                StoppedPublisher.Publish(this, EventArgs.Empty);
            }
        }

        public void MoveToNextFrame()
        {
            CurrentTime += FrameDuration;
            ChangeFrameRequestPublisher.Publish(this, EventArgs.Empty);
        }

        public void MoveToPrevFrame()
        {
            CurrentTime -= FrameDuration;
            ChangeFrameRequestPublisher.Publish(this, EventArgs.Empty);
        }

        public void AbortRenderRamPreview()
        {
            IsRenderingRamPreview = false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // 前回のタイマー発火時間から次のタイマー発火までの時間を計測する
            var timestamp = Stopwatch.GetTimestamp();
            if (LastUpdateCurrentTime.HasValue)
            {
                FrameRenderingTimes.Append(new TimeSpan(timestamp - LastUpdateCurrentTime.Value));
                RealFrameRate = 1000.0 / (FrameRenderingTimes.Sum(t => t.TotalMilliseconds) / FrameRenderingTimes.Count);
            }
            LastUpdateCurrentTime = timestamp;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var workareaEnd = UseRamPreview ? RamPreviewRenderedWorkareaEnd : WorkareaEnd;
                var time = CurrentTime;
                if (time < WorkareaBegin || time > workareaEnd)
                {
                    CurrentTime = (CurrentTime + FrameDuration) % Duration;
                }
                else
                {
                    var workarea = (workareaEnd - WorkareaBegin).RoundToFrameRate(FrameRate);
                    CurrentTime = (CurrentTime - WorkareaBegin + FrameDuration) % workarea + WorkareaBegin;
                }
                ChangeFrameRequestPublisher.Publish(this, EventArgs.Empty);
            });
        }

        private void PlayControllerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UseRamPreview))
            {
                Stop();
            }
            else if (e.PropertyName == nameof(IsPlaying))
            {
                ViewState.IsPreviewPlaying = IsPlaying;
            }
        }

        private void HistoryModel_HistoryChanged(object? sender, EventArgs e)
        {
            Stop();
        }

        private void HistoryModel_HistoryGroupChanging(object? sender, EventArgs e)
        {
            Stop();
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}

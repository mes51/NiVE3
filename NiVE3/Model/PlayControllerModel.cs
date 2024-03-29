using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Mvvm;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class PlayControllerModel : BindableBase, IDisposable
    {
        const int RealFrameRateAvgCount = 5;

        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double workareaBegin;
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { SetProperty(ref isPlaying, value); }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        private double realFrameRate = -1.0;
        public double RealFrameRate
        {
            get { return realFrameRate; }
            private set { SetProperty(ref realFrameRate, value); }
        }

        public double FrameDuration => FrameRate > 0.0 ? 1.0 / FrameRate : 0.0;

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

        RingBuffer<TimeSpan> FrameRenderingTimes { get; } = new RingBuffer<TimeSpan>(RealFrameRateAvgCount);

        MultiMediaTimer Timer { get; }

        long? LastUpdateCurrentTime { get; set; }

        public PlayControllerModel(HistoryModel historyModel)
        {
            Timer = new MultiMediaTimer();
            Timer.Tick += Timer_Tick;

            historyModel.HistoryChanged += HistoryModel_HistoryChanged;
            historyModel.HistoryGroupChanging += HistoryModel_HistoryGroupChanging;
        }

        public void Play()
        {
            if ((IsPlaying && !IsPaused) || !CanPreview)
            {
                return;
            }

            IsPlaying = true;
            IsPaused = false;
            LastUpdateCurrentTime = null;
            FrameRenderingTimes.Clear();
            RealFrameRate = -1.0;
            PreviewPlayPublisher.Publish(this, EventArgs.Empty);
            Timer.Interval = FrameDuration * 1000.0;
            Timer.Start();
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
            IsPlaying = false;
            IsPaused = false;
            Timer.Stop();
            StoppedPublisher.Publish(this, EventArgs.Empty);
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
                var time = CurrentTime;
                if (time < WorkareaBegin || time > WorkareaEnd)
                {
                    CurrentTime = ((int)Math.Round(CurrentTime * FrameRate + 1) / FrameRate) % Duration;
                }
                else
                {
                    var workarea = WorkareaEnd - WorkareaBegin;
                    CurrentTime = (((int)Math.Round((CurrentTime - WorkareaBegin) * FrameRate + 1) / FrameRate) % workarea) + WorkareaBegin;
                }
                ChangeFrameRequestPublisher.Publish(this, EventArgs.Empty);
            });
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

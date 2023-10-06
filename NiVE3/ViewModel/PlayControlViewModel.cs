using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Util;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.LeftBottom, Size = 75)]
    class PlayControlViewModel : SingletonePaneViewModelBase, IDisposable
    {
        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double currentTime;
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
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

        private bool canPreview;
        public bool CanPreview
        {
            get { return canPreview; }
            set { SetProperty(ref canPreview, value); }
        }

        public ICommand PlayCommand { get; }

        public ICommand PauseCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand NextFrameCommand { get; }

        public ICommand PrevFrameCommand { get; }

        MultiMediaTimer Timer { get; }

        public event EventHandler<EventArgs>? ChangeFrameRequest;

        public PlayControlViewModel()
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayControlView_Title);
            Timer = new MultiMediaTimer();
            Timer.Tick += Timer_Tick;

            PlayCommand = new RequerySuggestedCommand(() =>
            {
                Timer.Interval = 1.0 / FrameRate * 1000.0;
                Timer.Start();
                IsPlaying = true;
                IsPaused = false;
            }, () => CanPreview &&(!IsPlaying || IsPaused));

            PauseCommand = new RequerySuggestedCommand(() =>
            {
                if (!IsPaused)
                {
                    Timer.Stop();
                }
                else
                {
                    Timer.Start();
                }
                IsPaused = !IsPaused;
            }, () => CanPreview && IsPlaying);

            StopCommand = new RequerySuggestedCommand(() =>
            {
                Timer.Stop();
                IsPlaying = false;
                IsPaused = false;
            }, () => CanPreview && IsPlaying);

            NextFrameCommand = new RequerySuggestedCommand(() =>
            {
                CurrentTime = (int)(CurrentTime * FrameRate + 1) / FrameRate;
            }, () => CanPreview && !IsPlaying);

            PrevFrameCommand = new RequerySuggestedCommand(() =>
            {
                CurrentTime = (int)(CurrentTime * FrameRate - 1) / FrameRate;
            }, () => CanPreview && !IsPlaying);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTime = ((int)Math.Round(CurrentTime * FrameRate + 1) / FrameRate) % Duration;
                ChangeFrameRequest?.Invoke(this, EventArgs.Empty);
            });
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}

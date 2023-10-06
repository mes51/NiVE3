using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.Util;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using NiVE3.Mvvm;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.LeftBottom, Size = 75)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PlayControllerViewModel : SingletonePaneViewModelBase, IDisposable
    {
        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double currentTime;
        [NeedWire(nameof(PlayControllerModel))]
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
        [NeedWire(nameof(PlayControllerModel))]
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { SetProperty(ref isPlaying, value); }
        }

        private bool isPaused;
        [NeedWire(nameof(PlayControllerModel))]
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

        PlayControllerModel PlayControllerModel { get; }

        WeakEventPublisher<EventArgs> ChangeFrameRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> ChangeFrameRequest
        {
            add { ChangeFrameRequestPublisher.Subscribe(value); }
            remove { ChangeFrameRequestPublisher.Unsubscribe(value); }
        }

        public PlayControllerViewModel(PlayControllerModel playControllerModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayControlView_Title);
            Timer = new MultiMediaTimer();
            PlayControllerModel = playControllerModel;
            Timer.Tick += Timer_Tick;

            PlayCommand = new RequerySuggestedCommand(() =>
            {
                IsPlaying = true;
                IsPaused = false;
            }, () => CanPreview &&(!IsPlaying || IsPaused));

            PauseCommand = new RequerySuggestedCommand(() =>
            {
                IsPaused = !IsPaused;
            }, () => CanPreview && IsPlaying);

            StopCommand = new RequerySuggestedCommand(() =>
            {
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

            WiringModel();

            PropertyChanged += PlayControllerViewModel_PropertyChanged;
        }

        partial void WiringModel();

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTime = ((int)Math.Round(CurrentTime * FrameRate + 1) / FrameRate) % Duration;
                ChangeFrameRequestPublisher.Publish(this, EventArgs.Empty);
            });
        }

        private void PlayControllerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IsPlaying):
                    if (IsPlaying)
                    {
                        Timer.Interval = 1.0 / FrameRate * 1000.0;
                        Timer.Start();
                    }
                    else
                    {
                        Timer.Stop();
                    }
                    break;
                case nameof(IsPaused) when IsPlaying:
                    if (IsPaused)
                    {
                        Timer.Start();
                    }
                    else
                    {
                        Timer.Stop();
                    }
                    break;
                case nameof(CanPreview) when !CanPreview:
                    IsPlaying = false;
                    IsPaused = false;
                    Duration = 0.0;
                    FrameRate = 1.0;
                    CurrentTime = 0.0;
                    break;
            }
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}

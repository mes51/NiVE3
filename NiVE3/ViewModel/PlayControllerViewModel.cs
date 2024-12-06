using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.Model.UI;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left1Bottom, Size = 75)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PlayControllerViewModel : SingletonePaneViewModelBase
    {
        private double frameRate;
        [NeedWire(nameof(PlayControllerModel))]
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

        private double workareaBegin;
        [NeedWire(nameof(PlayControllerModel))]
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        [NeedWire(nameof(PlayControllerModel))]
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private double duration;
        [NeedWire(nameof(PlayControllerModel))]
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

        private bool isRenderingRamPreview;
        [NeedWire(nameof(PlayControllerModel), IsOneWay = true)]
        public bool IsRenderingRamPreview
        {
            get { return isRenderingRamPreview; }
            set { SetProperty(ref isRenderingRamPreview, value); }
        }

        private bool useRamPreview;
        [NeedWire(nameof(PlayControllerModel))]
        public bool UseRamPreview
        {
            get { return useRamPreview; }
            set { SetProperty(ref useRamPreview, value); }
        }

        public ICommand PlayCommand { get; }

        public ICommand PauseCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand NextFrameCommand { get; }

        public ICommand PrevFrameCommand { get; }

        PlayControllerModel PlayControllerModel { get; }

        bool CanPlay => ((int)(WorkareaEnd - WorkareaBegin) * FrameRate) > 1;

        public PlayControllerViewModel(PlayControllerModel playControllerModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayControlView_Title);
            PlayControllerModel = playControllerModel;

            PlayCommand = new RequerySuggestedCommand(() =>
            {
                PlayControllerModel.Play();
            }, () => PlayControllerModel.CanPreview && (!IsPlaying || IsPaused));

            PauseCommand = new RequerySuggestedCommand(() =>
            {
                PlayControllerModel.Pause();
            }, () => PlayControllerModel.CanPreview && IsPlaying);

            StopCommand = new RequerySuggestedCommand(() =>
            {
                PlayControllerModel.Stop();
            }, () => PlayControllerModel.CanPreview && (IsPlaying || IsRenderingRamPreview));

            NextFrameCommand = new RequerySuggestedCommand(() => PlayControllerModel.MoveToNextFrame(), () => PlayControllerModel.CanPreview && !IsPlaying);

            PrevFrameCommand = new RequerySuggestedCommand(() => PlayControllerModel.MoveToPrevFrame(), () => PlayControllerModel.CanPreview && !IsPlaying);

            WiringModel();
        }

        partial void WiringModel();
    }
}

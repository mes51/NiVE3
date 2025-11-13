using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.UI.Command;
using NiVE3.Model.UI;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left1Bottom, Size = 80)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class PlayControllerViewModel : SingletonePaneViewModelBase
    {
        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial double FrameRate { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial Time CurrentTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial Time WorkareaBegin { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial Time WorkareaEnd { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial bool IsPlaying { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial bool IsPaused { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel), IsOneWay = true)]
        public partial bool IsRenderingRamPreview { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(PlayControllerModel))]
        public partial bool UseRamPreview { get; set; }

        public ICommand PlayCommand { get; }

        public ICommand PauseCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand NextFrameCommand { get; }

        public ICommand PrevFrameCommand { get; }

        PlayControllerModel PlayControllerModel { get; }

        EventHubModel EventHubModel { get; }

        bool CanPlay => ((int)(WorkareaEnd - WorkareaBegin) * FrameRate) > 1;

        public PlayControllerViewModel(PlayControllerModel playControllerModel, EventHubModel eventHubModel)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayControlView_Title);
            PlayControllerModel = playControllerModel;
            EventHubModel = eventHubModel;

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

            EventHubModel.PlayOrStopRequest += EventHubModel_PlayOrStopRequest;

            WiringModel();
        }

        partial void WiringModel();

        private void EventHubModel_PlayOrStopRequest(object? sender, EventArgs e)
        {
            if (IsPlaying && !IsPaused)
            {
                PlayControllerModel.Stop();
            }
            else
            {
                PlayControllerModel.Play();
            }
        }
    }
}

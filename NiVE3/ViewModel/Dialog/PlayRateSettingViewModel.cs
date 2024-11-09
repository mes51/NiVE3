using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    class PlayRateSettingViewModel : BindableBase, IDialogAware
    {
        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayRateSettingView_Title);

        private double playRate;
        public double PlayRate
        {
            get { return playRate; }
            set { SetProperty(ref playRate, value); }
        }

        private double sourceDuration;
        public double SourceDuration
        {
            get { return sourceDuration; }
            set { SetProperty(ref sourceDuration, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double compositionFrameRate = 30.0;
        public double CompositionFrameRate
        {
            get { return compositionFrameRate; }
            set { SetProperty(ref compositionFrameRate, value); }
        }

        private double minPlayRate = double.MinValue;
        public double MinPlayRate
        {
            get { return minPlayRate; }
            set { SetProperty(ref minPlayRate, value); }
        }

        private double maxPlayRate = double.MaxValue;
        public double MaxPlayRate
        {
            get { return maxPlayRate; }
            set { SetProperty(ref maxPlayRate, value); }
        }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        double FrameDuration => 1.0 / CompositionFrameRate;

        bool IsChangingRate { get; set; }

        public PlayRateSettingViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                var result = new DialogParameters
                {
                    { nameof(PlayRate), PlayRate }
                };
                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = result });
            }, () => PlayRate != 0.0).ObservesProperty(() => PlayRate);

            CancelCommand = new DelegateCommand(() =>
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
            });

            PropertyChanged += PlayRateSettingViewModel_PropertyChanged;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            IsChangingRate = true;

            PlayRate = parameters.GetValue<double>(nameof(PlayRate));
            SourceDuration = parameters.GetValue<double>(nameof(SourceDuration));
            CompositionFrameRate = parameters.GetValue<double>(nameof(CompositionFrameRate));
            MinPlayRate = SourceDuration / FrameDuration * -100.0;
            MaxPlayRate = -MinPlayRate;
            Duration = Math.Abs(SourceDuration / (PlayRate * 0.01));

            RaisePropertyChanged(nameof(FrameDuration));

            IsChangingRate = false;
        }

        private void PlayRateSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayRate) when !IsChangingRate:
                    IsChangingRate = true;
                    Duration = Math.Abs(SourceDuration / (PlayRate * 0.01));
                    IsChangingRate = false;
                    break;
                case nameof(Duration) when !IsChangingRate:
                    IsChangingRate = true;
                    PlayRate = Duration != 0.0 ? SourceDuration / Duration * 100.0 : MaxPlayRate;
                    IsChangingRate = false;
                    break;
            }
        }
    }
}

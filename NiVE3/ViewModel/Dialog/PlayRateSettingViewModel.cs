using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class PlayRateSettingViewModel : BindableBase, IDialogAware
    {
        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PlayRateSettingView_Title);

        [ReactiveProperty]
        public partial double PlayRate { get; set; }

        [ReactiveProperty]
        public partial Time SourceDuration { get; set; }

        [ReactiveProperty]
        public partial Time Duration { get; set; }

        [ReactiveProperty]
        public partial double CompositionFrameRate { get; set; } = 30.0;

        [ReactiveProperty]
        public partial double MinPlayRate { get; set; } = double.MinValue;

        [ReactiveProperty]
        public partial double MaxPlayRate { get; set; } = double.MaxValue;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public double DoubleFrameDuration => 1.0 / CompositionFrameRate;

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
            SourceDuration = parameters.GetValue<Time>(nameof(SourceDuration));
            CompositionFrameRate = parameters.GetValue<double>(nameof(CompositionFrameRate));
            MinPlayRate = (double)SourceDuration / DoubleFrameDuration * -100.0;
            MaxPlayRate = -MinPlayRate;
            Duration = Time.Abs(SourceDuration / (PlayRate * 0.01));

            RaisePropertyChanged(nameof(DoubleFrameDuration));

            IsChangingRate = false;
        }

        private void PlayRateSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayRate) when !IsChangingRate:
                    IsChangingRate = true;
                    Duration = SourceDuration / (PlayRate * 0.01);
                    IsChangingRate = false;
                    break;
                case nameof(Duration) when !IsChangingRate:
                    IsChangingRate = true;
                    PlayRate = Duration != 0.0 ? (double)(SourceDuration / Duration) * 100.0 : MaxPlayRate;
                    IsChangingRate = false;
                    break;
            }
        }
    }
}

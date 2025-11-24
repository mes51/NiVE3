using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class GenerateAudioLevelValueKeyFrameViewModel : BindableBase, IDialogAware
    {
        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.GenerateAudioLevelValueKeyFrame_Title);

        [ReactiveProperty]
        public partial AudioLevelValueType Type { get; set; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public GenerateAudioLevelValueKeyFrameViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                var result = new DialogParameters
                {
                    { nameof(Type), Type }
                };
                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = result });
            });

            CancelCommand = new DelegateCommand(() =>
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
            });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters) { }
    }
}

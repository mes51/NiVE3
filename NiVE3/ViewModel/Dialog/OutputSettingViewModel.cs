using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class OutputSettingViewModel : BindableBase, IDialogAware
    {
        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.OutputSettingView_Title);

        private FrameworkElement? settingView;
        public FrameworkElement? SettingView
        {
            get { return settingView; }
            set { SetProperty(ref settingView, value); }
        }

        private bool hasErrors;
        public bool HasErrors
        {
            get { return hasErrors; }
            set { SetProperty(ref hasErrors, value); }
        }

        int ErrorCount { get; set; }

        public event Action<IDialogResult>? RequestClose;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public OutputSettingViewModel()
        {
            OKCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.OK, null)));

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null)));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // TODO: 何らか専用のエラー通知プロパティを作ってそこを優先して見る
            SettingView = parameters.GetValue<FrameworkElement>(nameof(SettingView));
            Validation.AddErrorHandler(SettingView, (sender, e) =>
            {
                if (e.Action == ValidationErrorEventAction.Added)
                {
                    ErrorCount++;
                }
                else
                {
                    ErrorCount--;
                }
                HasErrors = ErrorCount > 0;
            });
        }
    }
}

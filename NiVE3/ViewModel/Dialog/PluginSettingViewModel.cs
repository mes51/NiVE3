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
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    class PluginSettingViewModel : BindableBase, IDialogAware
    {
        public const string TitleLanguageResourceName = nameof(TitleLanguageResourceName);

        private string title = "";
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private FrameworkElement? settingView;
        public FrameworkElement? SettingView
        {
            get { return settingView; }
            private set { SetProperty(ref settingView, value); }
        }

        private bool hasErrors;
        public bool HasErrors
        {
            get { return hasErrors; }
            set { SetProperty(ref hasErrors, value); }
        }

        int ErrorCount { get; set; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public PluginSettingViewModel()
        {
            OKCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.OK)));

            CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(parameters.GetValue<string>(TitleLanguageResourceName));
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

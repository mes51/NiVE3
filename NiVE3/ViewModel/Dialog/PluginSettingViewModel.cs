using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class PluginSettingViewModel : BindableBase, IDialogAware
    {
        public const string TitleLanguageResourceName = nameof(TitleLanguageResourceName);

        [ReactiveProperty]
        public partial string Title { get; set; } = "";

        [ReactiveProperty]
        public partial FrameworkElement? SettingView { get; private set; }

        [ReactiveProperty]
        public partial bool HasErrors { get; set; }

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

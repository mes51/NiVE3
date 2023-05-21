using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel.Dialog
{
    class InputSettingViewModel : BindableBase, IDialogAware
    {
        public string Title => "入力設定";

        private FrameworkElement? settingView;
        public FrameworkElement? SettingView
        {
            get { return settingView; }
            set { SetProperty(ref settingView, value); }
        }

        public event Action<IDialogResult>? RequestClose;

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public InputSettingViewModel()
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
            SettingView = parameters.GetValue<FrameworkElement>(nameof(SettingView));
        }
    }
}

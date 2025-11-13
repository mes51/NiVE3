using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class AboutViewModel : BindableBase, IDialogAware
    {
        [ReactiveProperty]
        public partial string VersionString { get; set; } = "";

        public string Title => "NicoVisualEffects 3 について";

        public DialogCloseListener RequestClose { get; }

        public ICommand CloseCommand { get; }

        public AboutViewModel()
        {
            CloseCommand = new DelegateCommand(() =>
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.OK));
            });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            VersionString = typeof(AboutViewModel).Assembly.GetName().Version?.ToString() + " Closed α";
        }
    }
}

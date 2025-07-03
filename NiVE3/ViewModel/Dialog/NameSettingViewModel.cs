using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    class NameSettingViewModel : BindableBase, IDialogAware
    {
        private string title = "";
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private string label = "";
        public string Label
        {
            get { return label; }
            set { SetProperty(ref label, value); }
        }

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool allowEmptyName;
        public bool AllowEmptyName
        {
            get { return allowEmptyName; }
            set { SetProperty(ref allowEmptyName, value); }
        }

        private bool canOverwrite;
        public bool CanOverwrite
        {
            get { return canOverwrite; }
            set { SetProperty(ref canOverwrite, value); }
        }

        private string[] registeredNames = [];
        public string[] RegisteredNames
        {
            get { return registeredNames; }
            set { SetProperty(ref registeredNames, value); }
        }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public NameSettingViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                if (RegisteredNames.Contains(Name))
                {
                    var text = string.Format(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_NameSettingView_ConfirmOverwrite_Text), Name);
                    if (MessageBox.Show(text, Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) != MessageBoxResult.OK)
                    {
                        return;
                    }
                }

                var result = new DialogParameters
                {
                    { nameof(Name), Name }
                };
                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = result });
            });

            CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>(nameof(Title));
            Label = parameters.GetValue<string>(nameof(Label));
            if (parameters.TryGetValue<bool>(nameof(CanOverwrite), out var canOverwrite))
            {
                CanOverwrite = canOverwrite;
            }
            if (parameters.TryGetValue<bool>(nameof(AllowEmptyName), out var allowEmptyName))
            {
                AllowEmptyName = allowEmptyName;
            }
            if (parameters.TryGetValue<string[]>(nameof(RegisteredNames), out var registeredNames))
            {
                RegisteredNames = registeredNames;
            }
            if (parameters.TryGetValue<string>(nameof(Name), out var name))
            {
                Name = name;
            }
        }
    }
}

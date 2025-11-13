using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class PrecomposeSettingViewModel : BindableBase, IDialogAware
    {
        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.PrecomposeSettingView_Title);

        [ReactiveProperty]
        public partial string LayerName { get; set; } = "";

        [ReactiveProperty]
        public partial bool HasParent { get; set; }

        [ReactiveProperty]
        public partial bool TargetIsSingleLayer { get; set; }

        [ReactiveProperty]
        public partial string NewCompositionName { get; set; } = "";

        [ReactiveProperty]
        public partial PrecomposeMode Mode { get; set; }

        [ReactiveProperty]
        public partial bool AlignDurationToLayer { get; set; }

        [ReactiveProperty]
        public partial bool CopyParent { get; set; }

        public DialogCloseListener RequestClose { get; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public PrecomposeSettingViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                var parameters = new DialogParameters
                {
                    { nameof(NewCompositionName), NewCompositionName },
                    { nameof(Mode), Mode },
                    { nameof(AlignDurationToLayer), AlignDurationToLayer },
                    { nameof(CopyParent), CopyParent }
                };

                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = parameters });
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
            LayerName = parameters.GetValue<string>(nameof(LayerName));
            HasParent = parameters.GetValue<bool>(nameof(HasParent));
            TargetIsSingleLayer = parameters.GetValue<bool>(nameof(TargetIsSingleLayer));
            NewCompositionName = $"{LayerName} - Precompose";

            if (!TargetIsSingleLayer)
            {
                Mode = PrecomposeMode.MoveAll;
            }
        }
    }

    enum PrecomposeMode
    {
        LeaveAll,
        MoveAll
    }
}

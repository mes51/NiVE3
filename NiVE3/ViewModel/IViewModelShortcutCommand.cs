using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    interface IViewModelShortcutCommand
    {
        private static readonly DelegateCommand<SelectItemType?> Empty = new DelegateCommand<SelectItemType?>(_ => { });

        DelegateCommand<SelectItemType?> DeleteCommand { get; }

        DelegateCommand<SelectItemType?> CutCommand { get; }

        DelegateCommand<SelectItemType?> CopyCommand { get; }

        DelegateCommand<SelectItemType?> PasteCommand { get; }

        DelegateCommand<SelectItemType?> DuplicateCommand { get; }

        DelegateCommand<SelectItemType?> SelectAllCommand { get; }

        DelegateCommand<SelectItemType?> AddKeyFrameCommand => Empty;

        DelegateCommand<SelectItemType?> ResetPropertyCommand => Empty;

        DelegateCommand<SelectItemType?> SavePresetCommand => Empty;

        DelegateCommand<SelectItemType?> LoadPresetCommand => Empty;
    }
}

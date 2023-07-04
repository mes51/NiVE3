using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(UndoCommand), nameof(ShortcutKeySetting.UndoGesture), IsGlobal = true)]
    [CommandHandling(nameof(RedoCommand), nameof(ShortcutKeySetting.RedoGesture), IsGlobal = true)]
    class HistoryViewModel : CommandOnlyViewModelBase
    {
        public ICommand UndoCommand { get; }

        public ICommand RedoCommand { get; }

        HistoryModel Model { get; }

        public HistoryViewModel(HistoryModel model)
        {
            Model = model;

            UndoCommand = new DelegateCommand(() => Model.Undo(), () => Model.CanUndo());

            RedoCommand = new DelegateCommand(() => Model.Redo(), () => Model.CanRedo());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class HistoryModel : BindableBase
    {
        Stack<IHistoryCommand> UndoCommands { get; } = new Stack<IHistoryCommand>();

        Stack<IHistoryCommand> RedoCommands { get; } = new Stack<IHistoryCommand>();

        public void Undo()
        {
            if (!CanUndo())
            {
                return;
            }

            var command = UndoCommands.Pop();
            command.Undo();

            RedoCommands.Push(command);
        }

        public void Redo()
        {
            if (!CanRedo())
            {
                return;
            }

            var command = RedoCommands.Pop();
            command.Redo();

            UndoCommands.Push(command);
        }

        public void Add(IHistoryCommand command)
        {
            foreach (var c in RedoCommands)
            {
                c.Dispose();
            }

            RedoCommands.Clear();
            UndoCommands.Push(command);
        }

        public bool CanUndo()
        {
            return UndoCommands.Count > 0;
        }

        public bool CanRedo()
        {
            return RedoCommands.Count > 0;
        }
    }

    interface IHistoryCommand : IDisposable
    {
        void Undo();

        void Redo();
    }
}

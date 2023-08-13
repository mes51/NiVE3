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

        GroupedHistoryCommand? CurrentGroup { get; set; }

        public void Undo()
        {
            if (CurrentGroup != null)
            {
                // may be bug
                throw new Exception();
            }

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
            if (CurrentGroup != null)
            {
                // may be bug
                throw new Exception();
            }

            if (!CanRedo())
            {
                return;
            }

            var command = RedoCommands.Pop();
            command.Redo();

            UndoCommands.Push(command);
        }

        public void BeginGroup(string name)
        {
            if (CurrentGroup != null)
            {
                // may be bug
                throw new Exception();
            }

            CurrentGroup = new GroupedHistoryCommand(name);
        }

        public void EndGroup()
        {
            if (CurrentGroup == null)
            {
                // may be bug
                throw new Exception();
            }

            AddInternal(CurrentGroup);
            CurrentGroup = null;
        }

        public void AbortGroup()
        {
            CurrentGroup?.Undo();
            CurrentGroup?.Dispose();
            CurrentGroup = null;
        }

        public void Add(IHistoryCommand command)
        {
            if (CurrentGroup != null)
            {
                CurrentGroup.Add(command);
            }
            else
            {
                AddInternal(command);
            }
        }

        public bool CanUndo()
        {
            return UndoCommands.Count > 0;
        }

        public bool CanRedo()
        {
            return RedoCommands.Count > 0;
        }

        void AddInternal(IHistoryCommand command)
        {
            foreach (var c in RedoCommands)
            {
                c.Dispose();
            }

            RedoCommands.Clear();
            UndoCommands.Push(command);
        }

        private class GroupedHistoryCommand : IHistoryCommand
        {
            public string Name { get; }

            List<IHistoryCommand> Commands { get; } = new List<IHistoryCommand>();

            public GroupedHistoryCommand(string name)
            {
                Name = name;
            }

            public void Undo()
            {
                foreach (var c in Commands.Reverse<IHistoryCommand>())
                {
                    c.Undo();
                }
            }

            public void Redo()
            {
                foreach (var c in Commands)
                {
                    c.Redo();
                }
            }

            public void Add(IHistoryCommand command)
            {
                Commands.Add(command);
            }

            public void Dispose()
            {
                foreach (var c in Commands)
                {
                    c.Dispose();
                }
            }
        }
    }

    interface IHistoryCommand : IDisposable
    {
        string Name { get; }

        void Undo();

        void Redo();
    }
}

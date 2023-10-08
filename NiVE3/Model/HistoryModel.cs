using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Mvvm;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class HistoryModel : BindableBase
    {
        private ObservableStack<IHistoryCommand> undoCommands = new ObservableStack<IHistoryCommand>();
        public ObservableStack<IHistoryCommand> UndoCommands
        {
            get { return undoCommands; }
            set { SetProperty(ref undoCommands, value); }
        }

        private ObservableStack<IHistoryCommand> redoCommands = new ObservableStack<IHistoryCommand>();
        public ObservableStack<IHistoryCommand> RedoCommands
        {
            get { return redoCommands; }
            set { SetProperty(ref redoCommands, value); }
        }

        GroupedHistoryCommand? CurrentGroup { get; set; }

        int GroupNestCount { get; set; }

        PlayControllerModel PlayControllerModel { get; }

        public HistoryModel(PlayControllerModel playControllerModel)
        {
            PlayControllerModel = playControllerModel;
        }

        public void Undo()
        {
            if (CurrentGroup != null)
            {
                // may be bug
                throw new InvalidOperationException();
            }

            if (!CanUndo())
            {
                return;
            }

            PlayControllerModel.Stop();
            var command = UndoCommands.Pop();
            command.Undo();

            RedoCommands.Push(command);
        }

        public void Redo()
        {
            if (CurrentGroup != null)
            {
                // may be bug
                throw new InvalidOperationException();
            }

            if (!CanRedo())
            {
                return;
            }

            PlayControllerModel.Stop();
            var command = RedoCommands.Pop();
            command.Redo();

            UndoCommands.Push(command);
        }

        public void BeginGroup(string name)
        {
            if (CurrentGroup == null)
            {
                CurrentGroup = new GroupedHistoryCommand(name);
            }
            GroupNestCount++;
        }

        public void EndGroup()
        {
            if (GroupNestCount < 1)
            {
                // may be bug
                throw new InvalidOperationException();
            }

            GroupNestCount--;
            if (GroupNestCount < 1 && CurrentGroup != null)
            {
                AddInternal(CurrentGroup);
                CurrentGroup = null;
            }
        }

        public void AbortGroup()
        {
            GroupNestCount--;
            if (GroupNestCount < 1)
            {
                CurrentGroup?.Undo();
                CurrentGroup?.Dispose();
                CurrentGroup = null;
            }
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

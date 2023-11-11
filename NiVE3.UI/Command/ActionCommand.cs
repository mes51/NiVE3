using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.UI.Command
{
    public class ActionCommand : ICommand
    {
        Action Func { get; }

        Func<bool> CanExecFunc { get; }

        public ActionCommand(Action func) : this(func, () => true) { }

        public ActionCommand(Action func, Func<bool> canExecFunc)
        {
            Func = func;
            CanExecFunc = canExecFunc;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return CanExecFunc();
        }

        public void Execute(object? parameter)
        {
            Func();
        }
    }
}

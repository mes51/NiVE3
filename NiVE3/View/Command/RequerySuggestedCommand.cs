using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.View.Command
{
    class RequerySuggestedCommand : RequerySuggestedCommandBase
    {
        public RequerySuggestedCommand(Action func) : this(func, () => true) { }

        public RequerySuggestedCommand(Action func, Func<bool> canExecuteFunc) : base((_) => func(), (_) => canExecuteFunc()) { }
    }

    class RequerySuggestedCommand<T> : RequerySuggestedCommandBase
    {
        public RequerySuggestedCommand(Action<T> func) : this(func, (_) => true) { }

        public RequerySuggestedCommand(Action<T> func, Func<T, bool> canExecuteFunc) : base(CreateFuncWrapper(func), CreateCanExecuteFuncWrapper(canExecuteFunc)) { }

        static Action<object?> CreateFuncWrapper(Action<T> func)
        {
            return (parameter) =>
            {
                if (typeof(T).IsInstanceOfType(parameter))
                {
                    func((T)parameter);
                }
            };
        }

        static Func<object?, bool> CreateCanExecuteFuncWrapper(Func<T, bool> func)
        {
            return (parameter) =>
            {
                if (typeof(T).IsInstanceOfType(parameter))
                {
                    return func((T)parameter);
                }
                else
                {
                    return false;
                }
            };
        }
    }

    abstract class RequerySuggestedCommandBase : ICommand
    {
        public RequerySuggestedCommandBase(Action<object?> func) : this(func, (_) => true) { }

        public RequerySuggestedCommandBase(Action<object?> func, Func<object?, bool> canExecuteFunc)
        {
            Func = func;
            CanExecuteFunc = canExecuteFunc;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        Action<object?> Func { get; }

        Func<object?, bool> CanExecuteFunc { get; }

        public bool CanExecute(object? parameter)
        {
            return CanExecuteFunc(parameter);
        }

        public void Execute(object? parameter)
        {
            Func(parameter);
        }
    }
}

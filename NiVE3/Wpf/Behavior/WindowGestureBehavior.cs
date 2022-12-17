using Microsoft.Xaml.Behaviors;
using NiVE3.ViewModel;
using NiVE3.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

namespace NiVE3.Wpf.Behavior
{
    class WindowGestureBehavior : Behavior<Window>
    {
        public static ICommand GestureCommand { get; } = new RoutedUICommand("", "Gesture", typeof(WindowGestureBehavior));

        bool Initialized { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                CommandManager.AddPreviewCanExecuteHandler(AssociatedObject, GestureCommand_PreviewCanExecute);
                CommandManager.AddPreviewExecutedHandler(AssociatedObject, GestureCommand_PreviewExecuted);
                Initialized = true;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Unload();
        }

        void Unload()
        {
            if (!Initialized)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                CommandManager.RemovePreviewCanExecuteHandler(AssociatedObject, GestureCommand_PreviewCanExecute);
                CommandManager.RemovePreviewExecutedHandler(AssociatedObject, GestureCommand_PreviewExecuted);
                Initialized = false;
            });
        }

        ICommand? FindCommand(string gesture)
        {
            // TODO: 各Paneを実装したら中身を書く
            //var windows = FindAllWindow(AssociatedObject).ToArray();

            return new Prism.Commands.DelegateCommand(() => System.Diagnostics.Debug.WriteLine("Exec Command: " + gesture));
        }

        IEnumerable<Window> FindAllWindow(Window window)
        {
            if (window.Owner != null)
            {
                return FindAllWindow(window.Owner);
            }

            var list = new List<Window>();
            var queue = new Queue<Window>();
            queue.Enqueue(window);
            while (queue.Count > 0)
            {
                var w = queue.Dequeue();
                list.Add(w);
                foreach (var child in w.OwnedWindows.OfType<Window>())
                {
                    queue.Enqueue(child);
                }
            }

            return list;
        }

        private void GestureCommand_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == GestureCommand)
            {
                e.CanExecute = FindCommand(e.Parameter as string ?? "")?.CanExecute(null) ?? false;
                e.Handled = true;
            }
        }

        private void GestureCommand_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == GestureCommand)
            {
                FindCommand(e.Parameter as string ?? "")?.Execute(null);
                e.Handled = true;
            }
        }

        ~WindowGestureBehavior()
        {
            Unload();
            GC.SuppressFinalize(this);
        }
    }
}

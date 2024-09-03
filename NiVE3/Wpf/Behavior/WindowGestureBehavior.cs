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
using NiVE3.View.Command;
using Prism.Mvvm;

namespace NiVE3.Wpf.Behavior
{
    class WindowGestureBehavior : Behavior<Window>
    {
        public static RoutedCommand GestureCommand { get; } = new RoutedCommand("Gesture", typeof(WindowGestureBehavior));

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
            var mainWindowViewModel = (((MainWindow)Application.Current.MainWindow).DataContext as MainWindowViewModel);
            if (mainWindowViewModel == null)
            {
                return null;
            }

            var activeViewModel = mainWindowViewModel.ViewModels
                .OfType<PaneViewModelBase>()
                .FirstOrDefault(vm => vm.IsActive && vm.IsSelected);
            if (activeViewModel != null)
            {
                var command = CommandHandlingAttribute.GetCommand(activeViewModel, gesture, false);
                if (command != null)
                {
                    return command;
                }
            }

            var globalCommand = mainWindowViewModel.ViewModels
                .OfType<PaneViewModelBase>()
                .Cast<BindableBase>()
                .Concat(mainWindowViewModel.CommandOnlyViewModels)
                .Select(vm => CommandHandlingAttribute.GetCommand(vm, gesture, true))
                .FirstOrDefault(c => c != null);
            if (globalCommand != null)
            {
                return globalCommand;
            }

            return CommandHandlingAttribute.GetCommand(mainWindowViewModel, gesture, false);
        }

        private void GestureCommand_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command is RoutedCommand uiCommand && uiCommand.Name == GestureCommand.Name && uiCommand.OwnerType == typeof(WindowGestureBehavior))
            {
                e.CanExecute = FindCommand(e.Parameter as string ?? "")?.CanExecute(null) ?? false;
                e.Handled = true;
            }
        }

        private void GestureCommand_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command is RoutedCommand uiCommand && uiCommand.Name == GestureCommand.Name && uiCommand.OwnerType == typeof(WindowGestureBehavior))
            {
                FindCommand(e.Parameter as string ?? "")?.Execute(null);
                e.Handled = true;
            }
        }

        ~WindowGestureBehavior()
        {
            Unload();
        }
    }
}

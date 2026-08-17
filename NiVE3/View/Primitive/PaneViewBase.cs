using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NiVE3.ViewModel;

namespace NiVE3.View.Primitive
{
    public class PaneViewBase : UserControl
    {
        public PaneViewBase()
        {
            DataContextChanged += PaneViewBase_DataContextChanged;
        }

        private void PaneViewBase_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is PaneViewModelBase oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is PaneViewModelBase newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PaneViewModelBase.IsActive) || DataContext is not PaneViewModelBase viewModel || (viewModel?.IsActive ?? true))
            {
                return;
            }

            if (Keyboard.FocusedElement is DependencyObject focusedElement)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(focusedElement), null);

                var window = focusedElement as Window ?? Window.GetWindow(focusedElement);
                window?.Focus();
            }
        }
    }
}

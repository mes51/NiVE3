using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;

namespace NiVE3.OpenFX.Bridge.Property.Control
{
    /// <summary>
    /// StringPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class OfxStringPropertyControl : PropertyControlBase
    {
        public OfxStringPropertyControl()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is IPropertyViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is IPropertyViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                ValueTextBox.Text = newViewModel.CurrentTimeRawValue as string ?? "";
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPropertyViewModel.CurrentTimeRawValue) && !ValueTextBox.IsFocused)
            {
                ValueTextBox.Text = ViewModel?.CurrentTimeRawValue as string ?? "";
            }
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitValue();
        }

        private void ValueTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 複数行入力では Enter は改行の挿入 (AcceptsReturn) に使い、確定は Ctrl+Enter で行う
            if (e.Key == Key.Enter &&
                (!ValueTextBox.AcceptsReturn || (Keyboard.Modifiers & ModifierKeys.Control) != 0))
            {
                CommitValue();
            }
        }

        void CommitValue()
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var currentValue = viewModel.CurrentTimeRawValue as string ?? "";
            if (currentValue != ValueTextBox.Text)
            {
                viewModel.BeginEditCommand.Execute(null);
                viewModel.CurrentTimeRawValue = ValueTextBox.Text;
                viewModel.EndEditCommand.Execute(null);
            }
        }
    }
}

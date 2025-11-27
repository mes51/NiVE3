using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.ViewModel.Dialog;

namespace NiVE3.View.Dialog
{
    /// <summary>
    /// CommandPaletteView.xaml の相互作用ロジック
    /// </summary>
    public partial class CommandPaletteView : UserControl
    {
        public CommandPaletteView()
        {
            InitializeComponent();
        }

        CommandPaletteViewModel? ViewModel => DataContext as CommandPaletteViewModel;

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Focus();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None && viewModel?.SelectedCommand != null)
            {
                viewModel.ExecuteCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                viewModel?.CancelCommand.Execute(null);
            }
        }

        private void CommandListView_KeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (e.Key == Key.Enter && viewModel?.SelectedCommand != null)
            {
                viewModel.ExecuteCommand.Execute(null);
            }
        }

        private void CommandListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            var viewModel = ViewModel;
            if (viewModel?.SelectedCommand != null)
            {
                viewModel.ExecuteCommand.Execute(null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
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
using NiVE3.ViewModel;

namespace NiVE3.View.Popup
{
    /// <summary>
    /// CommandPalettePopup.xaml の相互作用ロジック
    /// </summary>
    public partial class CommandPalettePopup : System.Windows.Controls.Primitives.Popup
    {
        public CommandPalettePopup()
        {
            InitializeComponent();
        }

        CommandPaletteViewModel? ViewModel => DataContext as CommandPaletteViewModel;

        void Close()
        {
            SetCurrentValue(IsOpenProperty, false);
        }

        private void Root_Opened(object sender, EventArgs e)
        {
            FilterTextBox.Focus();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (e.Key == Key.Enter && e.ImeProcessedKey == Key.None && viewModel?.SelectedCommand != null)
            {
                viewModel.ExecuteCommand.Execute(null);
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void CommandListView_KeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (e.Key == Key.Enter && viewModel?.SelectedCommand != null)
            {
                viewModel.ExecuteCommand.Execute(null);
                Close();
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
                Close();
            }
        }
    }
}

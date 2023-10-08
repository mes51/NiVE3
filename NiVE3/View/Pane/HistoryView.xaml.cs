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

namespace NiVE3.View.Pane
{
    /// <summary>
    /// HistoryView.xaml の相互作用ロジック
    /// </summary>
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        HistoryViewModel? ViewModel => DataContext as HistoryViewModel;

        private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListView.SelectedItem != null)
            {
                ViewModel?.ReproduceToTargetHistoryCommand.Execute(HistoryListView.SelectedItem);
                HistoryListView.SelectedItem = null;
            }
        }
    }
}

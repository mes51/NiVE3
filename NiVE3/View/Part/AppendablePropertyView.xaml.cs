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

namespace NiVE3.View.Part
{
    /// <summary>
    /// AppendablePropertyView.xaml の相互作用ロジック
    /// </summary>
    public partial class AppendablePropertyView : PropertyViewBase
    {
        public AppendablePropertyView()
        {
            InitializeComponent();
        }

        private void OpenAppendItemMenuButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAppendItemMenuButton.ContextMenu.Visibility = Visibility.Visible;
            OpenAppendItemMenuButton.ContextMenu.PlacementTarget = OpenAppendItemMenuButton;
            OpenAppendItemMenuButton.ContextMenu.IsOpen = true;
        }
    }
}

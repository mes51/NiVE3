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
using NiVE3.Data;
using NiVE3.ViewModel.Input;
using NiVE3.Windows;

namespace NiVE3.View.Input
{
    /// <summary>
    /// SolidSettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class SolidSettingView : UserControl
    {
        public SolidSettingView()
        {
            InitializeComponent();
        }

        private void ColorChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SolidSettingViewModel vm)
            {
                var dialog = new ColorPickerWindow(vm.Color.ToByteColor());
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() ?? false)
                {
                    vm.Color = FloatColor.FromColor(dialog.Color);
                }
            }
        }
    }
}

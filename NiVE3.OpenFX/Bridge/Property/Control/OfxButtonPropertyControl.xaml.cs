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
using NiVE3.OpenFX.Bridge.Property.Properties;
using NiVE3.Plugin.Property.Control;

namespace NiVE3.OpenFX.Bridge.Property.Control
{
    /// <summary>
    /// ButtonPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class OfxButtonPropertyControl : PropertyControlBase
    {
        public OfxButtonPropertyControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (ViewModel?.Property as OfxButtonProperty)?.PerformClick();
        }
    }
}

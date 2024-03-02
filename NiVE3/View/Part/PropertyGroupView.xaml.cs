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
using NiVE3.Plugin.Property;
using NiVE3.View.Converter;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// PropertyGroupView.xaml の相互作用ロジック
    /// </summary>
    public partial class PropertyGroupView : PropertyViewBase
    {
        PropertyGroupViewModel? ViewModel => DataContext as PropertyGroupViewModel;

        public PropertyGroupView()
        {
            InitializeComponent();
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            ParentCollection?.SelectItem(ParentContainer, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            ViewModel?.SelectItemCommand?.Execute(null);
            e.Handled = true;
        }
    }
}

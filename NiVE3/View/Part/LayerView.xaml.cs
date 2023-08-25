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
    /// LayerView.xaml の相互作用ロジック
    /// </summary>
    public partial class LayerView : UserControl
    {
        public static readonly DependencyProperty LayerControlAreaWidthProperty = DependencyProperty.Register(
            nameof(LayerControlAreaWidth),
            typeof(double),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty LayerNumberProperty = DependencyProperty.Register(
            nameof(LayerNumber),
            typeof(int),
            typeof(LayerView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public int LayerNumber
        {
            get { return (int)GetValue(LayerNumberProperty); }
            set { SetValue(LayerNumberProperty, value); }
        }

        public double LayerControlAreaWidth
        {
            get { return (double)GetValue(LayerControlAreaWidthProperty); }
            set { SetValue(LayerControlAreaWidthProperty, value); }
        }

        LayerCollection? ParentCollection => ItemsControl.ItemsControlFromItemContainer(this) as LayerCollection;

        public LayerView()
        {
            InitializeComponent();
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ParentCollection?.SelectItem(this, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) ||  Keyboard.IsKeyDown(Key.RightCtrl));
        }
    }
}

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

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// DoublePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class DoublePropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
            nameof(Unit),
            typeof(string),
            typeof(DoublePropertyControl),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public DoublePropertyControl()
        {
            InitializeComponent();
        }

        private void SlidableNumberTextBox_BeginEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.BeginEditCommand?.Execute(null);
        }

        private void SlidableNumberTextBox_EndEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void SlidableNumberTextBox_AbortEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.AbortEditCommand?.Execute(null);
        }
    }
}

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
using NiVE3.Plugin.Interfaces;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// LayerPropertyControllerView.xaml の相互作用ロジック
    /// </summary>
    public partial class LayerPropertyControllerView : UserControl
    {
        // NOTE: なぜかTypeConverterをSourceTypeにつけてもNREが出てXAML上でリソースとして定義出来ないため、定数として定義する
        public static readonly SourceType CompositionDisplayableSourceType = SourceType.Image | SourceType.Video;

        public LayerPropertyControllerView()
        {
            InitializeComponent();
        }
    }
}

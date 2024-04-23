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
    /// RenderQueueItemView.xaml の相互作用ロジック
    /// </summary>
    public partial class RenderQueueItemView : UserControl
    {
        public static readonly DependencyProperty IsLockedByRenderingProperty = DependencyProperty.Register(
            nameof(IsLockedByRendering),
            typeof(bool),
            typeof(RenderQueueItemView),
            new FrameworkPropertyMetadata(false)
        );

        public bool IsLockedByRendering
        {
            get { return (bool)GetValue(IsLockedByRenderingProperty); }
            set { SetValue(IsLockedByRenderingProperty, value); }
        }

        public RenderQueueItemView()
        {
            InitializeComponent();
        }
    }
}

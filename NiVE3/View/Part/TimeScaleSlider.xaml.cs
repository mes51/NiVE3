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
using Prism.Commands;

namespace NiVE3.View.Part
{
    /// <summary>
    /// TimeScaleSlider.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeScaleSlider : Slider
    {
        public ICommand DecreaseScaleCommand { get; }

        public ICommand IncreaseScaleCommand { get; }

        public TimeScaleSlider()
        {
            InitializeComponent();

            DecreaseScaleCommand = new DelegateCommand(() => Value += (Maximum - Value) * 0.5);
            IncreaseScaleCommand = new DelegateCommand(() => Value *= 0.5);

            Orientation = Orientation.Horizontal;
        }
    }
}

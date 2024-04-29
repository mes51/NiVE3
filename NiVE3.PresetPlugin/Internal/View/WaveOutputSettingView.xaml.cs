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

namespace NiVE3.PresetPlugin.Internal.View
{
    /// <summary>
    /// WaveOutputSettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class WaveOutputSettingView : UserControl
    {
        public static readonly int[] SupportedAudioSamplingRate = [48000, 44100, 32000, 22050, 16000, 11025, 8000];

        public static readonly int[] SupportedAudioBitsPerSample = [32, 16, 8];

        public WaveOutputSettingView()
        {
            InitializeComponent();
        }
    }
}

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
    /// AudioLevelMeter.xaml の相互作用ロジック
    /// </summary>
    public partial class AudioLevelMeter : UserControl
    {
        const double MinimumLevel = 60.0;

        public static readonly DependencyProperty AudioLevelProperty = DependencyProperty.Register(
            nameof(AudioLevel),
            typeof(double),
            typeof(AudioLevelMeter),
            new FrameworkPropertyMetadata(-60.0, AudioLevelChanged)
        );

        private static readonly DependencyProperty IsUnderZeroDecibelProperty = DependencyProperty.Register(
            nameof(IsUnderZeroDecibel),
            typeof(bool),
            typeof(AudioLevelMeter),
            new FrameworkPropertyMetadata(true)
        );

        private static readonly DependencyProperty LevelCoverWidthRateProperty = DependencyProperty.Register(
            nameof(LevelCoverWidthRate),
            typeof(double),
            typeof(AudioLevelMeter),
            new FrameworkPropertyMetadata(1.0)
        );

        public double AudioLevel
        {
            get { return (double)GetValue(AudioLevelProperty); }
            set { SetValue(AudioLevelProperty, value); }
        }

        private double LevelCoverWidthRate
        {
            get { return (double)GetValue(LevelCoverWidthRateProperty); }
            set { SetValue(LevelCoverWidthRateProperty, value); }
        }

        private bool IsUnderZeroDecibel
        {
            get { return (bool)GetValue(IsUnderZeroDecibelProperty); }
            set { SetValue(IsUnderZeroDecibelProperty, value); }
        }

        public AudioLevelMeter()
        {
            InitializeComponent();
        }

        private void OverZeroDecibelMarker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsUnderZeroDecibel = true;
        }

        static void AudioLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AudioLevelMeter audioLevelMeter)
            {
                var levelRate = Math.Max((MinimumLevel + audioLevelMeter.AudioLevel) / MinimumLevel, 0.0);
                audioLevelMeter.LevelCoverWidthRate = 1.0 - Math.Min(levelRate, 1.0);
                audioLevelMeter.IsUnderZeroDecibel &= levelRate < 1.0;
            }
        }
    }
}

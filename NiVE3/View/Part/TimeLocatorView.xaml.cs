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
    /// TimeLocatorView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeLocatorView : UserControl
    {
        // TODO: デザイン決定後調整
        public const double SideSpacerWidth = 10;

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, TimeChanged)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, TimeChanged)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, TimeChanged)
        );

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(
            nameof(CurrentTime),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, TimeChanged)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyPropertyKey IndicatorPositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IndicatorPosition),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(SideSpacerWidth, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IndicatorPositionProperty = IndicatorPositionPropertyKey.DependencyProperty;

        public double CurrentTime
        {
            get { return (double)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        public double Duration
        {
            get { return (double)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public double RangeStart
        {
            get { return (double)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public double Range
        {
            get { return (double)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public double IndicatorPosition
        {
            get { return (double)GetValue(IndicatorPositionProperty); }
            private set { SetValue(IndicatorPositionPropertyKey, value); }
        }

        bool IsScrubbing { get; set; }

        public TimeLocatorView()
        {
            InitializeComponent();
        }

        double CalcTimeFromPixel(double x)
        {
            var timePerPixel = Range / (ActualWidth - SideSpacerWidth * 2.0);
            return Math.Clamp(RangeStart + x * timePerPixel, 0.0, Duration);
        }

        private void ScrubbingArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScrubbingArea.CaptureMouse();
            IsScrubbing = true;

            var time = (int)Math.Round(CalcTimeFromPixel(e.GetPosition((IInputElement)sender).X - SideSpacerWidth) * FrameRate) / FrameRate;
            CurrentTime = time;
            if (time < RangeStart)
            {
                RangeStart = time;
            }
            else if (time >= RangeStart + Range)
            {
                RangeStart = time - Range;
            }
        }

        private void ScrubbingArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsScrubbing)
            {
                return;
            }

            var time = (int)Math.Round(CalcTimeFromPixel(e.GetPosition((IInputElement)sender).X - SideSpacerWidth) * FrameRate) / FrameRate;
            CurrentTime = time;
            if (time < RangeStart)
            {
                RangeStart = time;
            }
            else if (time >= RangeStart + Range)
            {
                RangeStart = time - Range;
            }
        }

        private void ScrubbingArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsScrubbing)
            {
                ScrubbingArea.ReleaseMouseCapture();
            }
            IsScrubbing = false;
        }

        static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeLocatorView timeLocatorView)
            {
                var timePerPixel = timeLocatorView.Range / (timeLocatorView.ActualWidth - SideSpacerWidth * 2.0);
                timeLocatorView.IndicatorPosition = (timeLocatorView.CurrentTime - timeLocatorView.RangeStart) / timePerPixel + SideSpacerWidth;
            }
        }
    }
}

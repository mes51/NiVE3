using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using NiVE3.View.Primitive;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    /// <summary>
    /// TimeLocatorView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeLocatorView : UserControl
    {
        const double DisplayFrameRangeThreshold = 20.0;

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TimeChanged)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TimeChanged)
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
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TimeChanged)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ShowIndicatorLineInRangeBarProperty = DependencyProperty.Register(
            nameof(ShowIndicatorLineInRangeBar),
            typeof(bool),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyProperty RangeBarIndicatorPositionProperty = DependencyProperty.Register(
            nameof(RangeBarIndicatorPosition),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyPropertyKey IndicatorPositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IndicatorPosition),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(UIParameters.TimelineRangeThumbWidth, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IndicatorPositionProperty = IndicatorPositionPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey FrameRangeWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FrameRangeWidth),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty FrameRangeWidthProperty = FrameRangeWidthPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey FrameRangePositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FrameRangePosition),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty FrameRangePositionProperty = FrameRangePositionPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey LocatorAreaActualHeightPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LocatorAreaActualHeight),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty LocatorAreaActualHeightProperty = LocatorAreaActualHeightPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey MinimumRangePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MinimumRange),
            typeof(double),
            typeof(TimeLocatorView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MinimumRangeProperty = MinimumRangePropertyKey.DependencyProperty;

        public double MinimumRange
        {
            get { return (double)GetValue(MinimumRangeProperty); }
            private set { SetValue(MinimumRangePropertyKey, value); }
        }

        public double LocatorAreaActualHeight
        {
            get { return (double)GetValue(LocatorAreaActualHeightProperty); }
            private set { SetValue(LocatorAreaActualHeightPropertyKey, value); }
        }

        public double FrameRangePosition
        {
            get { return (double)GetValue(FrameRangePositionProperty); }
            private set { SetValue(FrameRangePositionPropertyKey, value); }
        }

        public double FrameRangeWidth
        {
            get { return (double)GetValue(FrameRangeWidthProperty); }
            private set { SetValue(FrameRangeWidthPropertyKey, value); }
        }

        public double IndicatorPosition
        {
            get { return (double)GetValue(IndicatorPositionProperty); }
            private set { SetValue(IndicatorPositionPropertyKey, value); }
        }

        private double RangeBarIndicatorPosition
        {
            get { return (double)GetValue(RangeBarIndicatorPositionProperty); }
            set { SetValue(RangeBarIndicatorPositionProperty, value); }
        }

        public bool ShowIndicatorLineInRangeBar
        {
            get { return (bool)GetValue(ShowIndicatorLineInRangeBarProperty); }
            set { SetValue(ShowIndicatorLineInRangeBarProperty, value); }
        }

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

        bool IsScrubbing { get; set; }

        public TimeLocatorView()
        {
            InitializeComponent();
        }

        double CalcTimeFromPixel(double x)
        {
            var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbWidth * 2.0);
            return Math.Clamp(RangeStart + x * timePerPixel, 0.0, Duration);
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            MinimumRange = TimeBar.MinimumRange;
            // TODO: Unloadedとセットなのでリークは大丈夫なはずだが、一応他の方法が見つかったら変更する
            DependencyPropertyDescriptor.FromProperty(Primitive.TimeBar.MinimumRangeProperty, typeof(TimeBar))
                .AddValueChanged(TimeBar, TimeBar_MinimumRangeChanged);
        }

        private void Root_Unloaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor.FromProperty(Primitive.TimeBar.MinimumRangeProperty, typeof(TimeBar))
                .RemoveValueChanged(TimeBar, TimeBar_MinimumRangeChanged);
        }

        private void ScrubbingArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScrubbingArea.CaptureMouse();
            IsScrubbing = true;

            var time = (int)Math.Round(CalcTimeFromPixel(e.GetPosition((IInputElement)sender).X - UIParameters.TimelineRangeThumbWidth) * FrameRate) / FrameRate;
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

            var time = (int)Math.Round(CalcTimeFromPixel(e.GetPosition((IInputElement)sender).X - UIParameters.TimelineRangeThumbWidth) * FrameRate) / FrameRate;
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

        private void HeightMeasurementLocatorArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LocatorAreaActualHeight = HeightMeasurementLocatorArea.ActualHeight;
        }

        private void TimeBar_MinimumRangeChanged(object? sender, EventArgs e)
        {
            MinimumRange = TimeBar.MinimumRange;
        }

        static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TimeLocatorView timeLocatorView)
            {
                return;
            }

            var pixelPerTime = (timeLocatorView.ActualWidth - UIParameters.TimelineRangeThumbWidth * 2.0) / timeLocatorView.Range;
            if (!double.IsNaN(pixelPerTime) && !double.IsInfinity(pixelPerTime) && pixelPerTime > 0.0)
            {
                timeLocatorView.IndicatorPosition = (timeLocatorView.CurrentTime - timeLocatorView.RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;

                var frameRangeWidth = (1.0 / timeLocatorView.FrameRate) * pixelPerTime;
                if (frameRangeWidth >= DisplayFrameRangeThreshold)
                {
                    timeLocatorView.FrameRangeWidth = frameRangeWidth;
                }
                else
                {
                    timeLocatorView.FrameRangeWidth = 0.0;
                }
            }
            else
            {
                timeLocatorView.IndicatorPosition = UIParameters.TimelineRangeThumbWidth;
            }

            var globalPixelPerTime = (timeLocatorView.ActualWidth - UIParameters.TimelineRangeThumbWidth * 2.0) / timeLocatorView.Duration;
            if (!double.IsNaN(globalPixelPerTime) && !double.IsInfinity(globalPixelPerTime) && globalPixelPerTime > 0.0)
            {
                timeLocatorView.RangeBarIndicatorPosition = timeLocatorView.CurrentTime * globalPixelPerTime + UIParameters.TimelineRangeThumbWidth;
            }
            else
            {
                timeLocatorView.RangeBarIndicatorPosition = UIParameters.TimelineRangeThumbWidth;
            }
            timeLocatorView.FrameRangePosition = timeLocatorView.IndicatorPosition; // TODO: モーションブラー適用時の範囲に合わせる
        }
    }
}

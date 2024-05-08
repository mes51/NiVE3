using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    /// <summary>
    /// WorkareaBar.xaml の相互作用ロジック
    /// </summary>
    public partial class WorkareaBar : UserControl
    {
        private static readonly DependencyProperty BeforeWorkareaStartWidthProperty = DependencyProperty.Register(
            nameof(BeforeWorkareaStartWidth),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        private static readonly DependencyProperty AfterWorkareaEndWidthProperty = DependencyProperty.Register(
            nameof(AfterWorkareaEndWidth),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        private static readonly DependencyProperty FrameRangeWidthProperty = DependencyProperty.Register(
            nameof(FrameRangeWidth),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        private static readonly DependencyProperty WorkareaLeftProperty = DependencyProperty.Register(
            nameof(WorkareaLeft),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        private static readonly DependencyProperty WorkareaGridWidthProperty = DependencyProperty.Register(
            nameof(WorkareaGridWidth),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
            nameof(BarHeight),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, TimeChanged)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsArrange, TimeChanged)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, TimeChanged)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, TimeChanged)
        );

        public static readonly DependencyProperty WorkareaBeginProperty = DependencyProperty.Register(
            nameof(WorkareaBegin),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TimeChanged, CoerceWorkareaBegin)
        );

        public static readonly DependencyProperty WorkareaEndProperty = DependencyProperty.Register(
            nameof(WorkareaEnd),
            typeof(double),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TimeChanged, CoerceWorkareaEnd)
        );

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(WorkareaBar),
            new FrameworkPropertyMetadata(null)
        );

        public ICommand? Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public double WorkareaEnd
        {
            get { return (double)GetValue(WorkareaEndProperty); }
            set { SetValue(WorkareaEndProperty, value); }
        }

        public double WorkareaBegin
        {
            get { return (double)GetValue(WorkareaBeginProperty); }
            set { SetValue(WorkareaBeginProperty, value); }
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

        public double BarHeight
        {
            get { return (double)GetValue(BarHeightProperty); }
            set { SetValue(BarHeightProperty, value); }
        }

        private double WorkareaGridWidth
        {
            get { return (double)GetValue(WorkareaGridWidthProperty); }
            set { SetValue(WorkareaGridWidthProperty, value); }
        }

        private double WorkareaLeft
        {
            get { return (double)GetValue(WorkareaLeftProperty); }
            set { SetValue(WorkareaLeftProperty, value); }
        }

        private double FrameRangeWidth
        {
            get { return (double)GetValue(FrameRangeWidthProperty); }
            set { SetValue(FrameRangeWidthProperty, value); }
        }

        private double AfterWorkareaEndWidth
        {
            get { return (double)GetValue(AfterWorkareaEndWidthProperty); }
            set { SetValue(AfterWorkareaEndWidthProperty, value); }
        }

        private double BeforeWorkareaStartWidth
        {
            get { return (double)GetValue(BeforeWorkareaStartWidthProperty); }
            set { SetValue(BeforeWorkareaStartWidthProperty, value); }
        }

        bool IsRangeStartThumbDragging { get; set; }

        bool IsRangeThumbDragging { get; set; }

        bool IsRangeEndThumbDragging { get; set; }

        public WorkareaBar()
        {
            InitializeComponent();
        }

        void UpdateBar()
        {
            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
            {
                WorkareaGridWidth = ActualWidth;
                WorkareaLeft = 0;
                return;
            }

            BeforeWorkareaStartWidth = WorkareaBegin * pixelPerTime;
            AfterWorkareaEndWidth = Math.Max(Duration - WorkareaEnd, 0.0) * pixelPerTime;
            FrameRangeWidth = (1.0 / FrameRate) * pixelPerTime;
            WorkareaGridWidth = Duration * pixelPerTime + UIParameters.TimelineRangeThumbTotalWidth;
            WorkareaLeft = -RangeStart * pixelPerTime;
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBar();
        }

        private void RangeStartThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeThumbDragging && !IsRangeEndThumbDragging && FrameRate > 0.0)
            {
                IsRangeStartThumbDragging = true;
            }
        }

        private void RangeStartThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!IsRangeStartThumbDragging)
            {
                return;
            }

            var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
            var frameDuration = 1.0 / FrameRate;
            var time = (int)Math.Round(Math.Clamp(WorkareaBegin + e.HorizontalChange * timePerPixel, 0.0, WorkareaEnd - frameDuration) * FrameRate) * frameDuration;
            WorkareaBegin = time;
        }

        private void RangeStartThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeStartThumbDragging = false;

            Command?.Execute(Tuple.Create(WorkareaBegin, WorkareaEnd));
        }

        private void RangeEndThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeStartThumbDragging && !IsRangeThumbDragging && FrameRate > 0.0)
            {
                IsRangeEndThumbDragging = true;
            }
        }

        private void RangeEndThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!IsRangeEndThumbDragging)
            {
                return;
            }

            var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
            var frameDuration = 1.0 / FrameRate;
            var time = (int)Math.Round(Math.Clamp(WorkareaEnd + e.HorizontalChange * timePerPixel, WorkareaBegin + frameDuration, Duration) * FrameRate) * frameDuration;
            WorkareaEnd = time;
        }

        private void RangeEndThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeEndThumbDragging = false;

            Command?.Execute(Tuple.Create(WorkareaBegin, WorkareaEnd));
        }

        private void RangeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeStartThumbDragging && !IsRangeEndThumbDragging && FrameRate > 0.0)
            {
                IsRangeThumbDragging = true;
            }
        }

        private void RangeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!IsRangeThumbDragging)
            {
                return;
            }

            var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
            var frameDuration = 1.0 / FrameRate;
            var diffTime = (int)Math.Round(Math.Clamp(e.HorizontalChange * timePerPixel, -WorkareaBegin, Duration - WorkareaEnd) * FrameRate) * frameDuration;
            WorkareaBegin += diffTime;
            WorkareaEnd += diffTime;
        }

        private void RangeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeThumbDragging = false;

            Command?.Execute(Tuple.Create(WorkareaBegin, WorkareaEnd));
        }

        static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WorkareaBar workareaBar)
            {
                workareaBar.UpdateBar();
            }
        }

        static object CoerceWorkareaBegin(DependencyObject d, object value)
        {
            if (d is WorkareaBar workareaBar)
            {
                if (workareaBar.FrameRate > 0.0 && workareaBar.Duration > 0.0)
                {
                    return Math.Clamp((double)value, 0.0, workareaBar.Duration - 1.0 / workareaBar.FrameRate);
                }
                else
                {
                    return 0.0;
                }
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static object CoerceWorkareaEnd(DependencyObject d, object value)
        {
            if (d is WorkareaBar workareaBar)
            {
                if (workareaBar.FrameRate > 0.0 && workareaBar.Duration > 0.0)
                {
                    return Math.Clamp((double)value, 1.0 / workareaBar.FrameRate, workareaBar.Duration);
                }
                else
                {
                    return 0.0;
                }
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}

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
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Resource;
using Prism.Commands;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// RangeScrollBar.xaml の相互作用ロジック
    /// </summary>
    public partial class RangeScrollBar : UserControl
    {
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(Time),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceMinimum),
            IsValidTime
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(Time),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(Time.One, FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceMaximum),
            IsValidTime
        );

        public static readonly DependencyProperty MinimumRangeProperty = DependencyProperty.Register(
            nameof(MinimumRange),
            typeof(Time),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(new Time(0.01), FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(Time.One, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange, RangeChanged),
            IsValidTime
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceRangeStart),
            IsValidTime
        );

        public static readonly DependencyProperty RangeChangeRateProperty = DependencyProperty.Register(
            nameof(RangeChangeRate),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(0.1, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        public double RangeChangeRate
        {
            get { return (double)GetValue(RangeChangeRateProperty); }
            set { SetValue(RangeChangeRateProperty, value); }
        }

        public Time RangeStart
        {
            get { return (Time)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public Time Range
        {
            get { return (Time)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public Time MinimumRange
        {
            get { return (Time)GetValue(MinimumRangeProperty); }
            set { SetValue(MinimumRangeProperty, value); }
        }

        public Time Maximum
        {
            get { return (Time)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public Time Minimum
        {
            get { return (Time)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        double RangeWidth => ActualWidth - UIParameters.TimelineRangeThumbTotalWidth;

        // NOTE: Bindingの値の更新タイミング的にRangeの更新よりも先にMaximumの更新が出来ない(っぽい?)ため、Coerceによる値の修正は行わず、使用時にClampする
        Time ClampedRange => Time.Clamp(Range, MinimumRange, Maximum);

        bool IsRangeStartThumbDragging { get; set; }

        bool IsRangeThumbDragging { get; set; }

        bool IsRangeEndThumbDragging { get; set; }

        public ICommand IncreaseRangeStartCommand { get; }

        public ICommand DecreaseRangeStartCommand { get; }

        public RangeScrollBar()
        {
            IncreaseRangeStartCommand = new DelegateCommand(() => RangeStart += Range * RangeChangeRate);
            DecreaseRangeStartCommand = new DelegateCommand(() => RangeStart -= Range * RangeChangeRate);

            InitializeComponent();
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            base.ArrangeOverride(arrangeBounds);

            var totalRange = Maximum - Minimum;
            var rangeBounds = arrangeBounds.Width - UIParameters.TimelineRangeThumbTotalWidth;
            if (rangeBounds < 0.0 || totalRange <= Time.Zero)
            {
                DecreaseButton.Width = 0.0;
                IncreaseButton.Width = 0.0;
                return arrangeBounds;
            }

            var rangeGridWidth = (double)(Range / totalRange) * rangeBounds + UIParameters.TimelineRangeThumbTotalWidth;
            DecreaseButton.Width = (double)(RangeStart / totalRange) * rangeBounds;
            IncreaseButton.Width = Math.Max(arrangeBounds.Width - (DecreaseButton.Width + rangeGridWidth), 0.0);

            return arrangeBounds;
        }

        private void RangeStartThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeThumbDragging && !IsRangeEndThumbDragging)
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

            var clampedRange = ClampedRange;
            var area = Maximum - Minimum;
            var rangePerPixel = area / RangeWidth;
            var prevRangeStart = RangeStart;
            var changed = Time.MaxAndMin(-e.HorizontalChange * rangePerPixel, MinimumRange - clampedRange, RangeStart);
            Range = clampedRange + changed;
            RangeStart = prevRangeStart - changed;
        }

        private void RangeStartThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeStartThumbDragging = false;
        }

        private void RangeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeStartThumbDragging && !IsRangeEndThumbDragging)
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

            var area = Maximum - Minimum;
            var rangePerPixel = area / RangeWidth;
            RangeStart += Time.MaxAndMin(e.HorizontalChange * rangePerPixel, Maximum - (RangeStart + ClampedRange), -RangeStart);
        }

        private void RangeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeThumbDragging = false;
        }

        private void RangeEndThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsRangeStartThumbDragging && !IsRangeThumbDragging)
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

            var area = Maximum - Minimum;
            var rangePerPixel = area / RangeWidth;
            var changed = Time.MaxAndMin(e.HorizontalChange * rangePerPixel, MinimumRange - Range, Maximum - (RangeStart + Range));
            var prevRangeStart = RangeStart;
            Range += changed;
            RangeStart = prevRangeStart;
        }

        private void RangeEndThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsRangeEndThumbDragging = false;
        }

        static void RangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RangeScrollBar scrollBar)
            {
                var diff = (Time)e.NewValue - (Time)e.OldValue;
                scrollBar.RangeStart = Time.MaxAndMin(scrollBar.RangeStart - diff * 0.5, scrollBar.Minimum, scrollBar.Maximum - scrollBar.ClampedRange);
            }
        }

        static object CoerceMinimum(DependencyObject d, object value)
        {
            if (d is RangeScrollBar scrollBar)
            {
                return Time.Min((Time)value, scrollBar.Maximum);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static object CoerceMaximum(DependencyObject d, object value)
        {
            if (d is RangeScrollBar scrollBar)
            {
                return Time.Max((Time)value, scrollBar.Minimum);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static object CoerceRangeStart(DependencyObject d, object value)
        {
            if (d is RangeScrollBar scrollBar)
            {
                return Time.MaxAndMin((Time)value, scrollBar.Minimum, scrollBar.Maximum - scrollBar.ClampedRange);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static bool IsValidTime(object value)
        {
            if (value is Time d)
            {
                return !Time.IsNaN(d) && !Time.IsInfinity(d);
            }
            else
            {
                return false;
            }
        }
    }
}

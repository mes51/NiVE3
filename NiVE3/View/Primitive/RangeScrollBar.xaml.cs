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
using Prism.Commands;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// RangeScrollBar.xaml の相互作用ロジック
    /// </summary>
    public partial class RangeScrollBar : UserControl
    {
        // TODO: デザイン決定後調整
        const double RangeThumbWidth = 20.0;

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceMinimum),
            IsValidDouble
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceMaximum),
            IsValidDouble
        );

        public static readonly DependencyProperty MinimumRangeProperty = DependencyProperty.Register(
            nameof(MinimumRange),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(0.01, FrameworkPropertyMetadataOptions.AffectsArrange)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange, RangeChanged),
            IsValidDouble
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(RangeScrollBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceRangeStart),
            IsValidDouble
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

        public double MinimumRange
        {
            get { return (double)GetValue(MinimumRangeProperty); }
            set { SetValue(MinimumRangeProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        double RangeWidth => ActualWidth - RangeThumbWidth;

        // NOTE: Bindingの値の更新タイミング的にRangeの更新よりも先にMaximumの更新が出来ない(っぽい?)ため、Coerceによる値の修正は行わず、使用時にClampする
        double ClampedRange => Math.Clamp(Range, MinimumRange, Maximum);

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
            var rangeBounds = arrangeBounds.Width - RangeThumbWidth;
            var rangeGridWidth = (Range / totalRange) * rangeBounds + RangeThumbWidth;
            DecreaseButton.Width = (RangeStart / totalRange) * rangeBounds;
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
            var changed = Math.Min(Math.Max(-e.HorizontalChange * rangePerPixel, MinimumRange - clampedRange), RangeStart);
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
            RangeStart += Math.Max(Math.Min(e.HorizontalChange * rangePerPixel, Maximum - (RangeStart + ClampedRange)), -RangeStart);
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
            var changed = Math.Min(Math.Max(e.HorizontalChange * rangePerPixel, MinimumRange - Range), Maximum - (RangeStart + Range));
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
                var diff = (double)e.NewValue - (double)e.OldValue;
                scrollBar.RangeStart = Math.Clamp(scrollBar.RangeStart - diff * 0.5, scrollBar.Minimum, scrollBar.Maximum - scrollBar.Range);
            }
        }

        static object CoerceMinimum(DependencyObject d, object value)
        {
            if (d is RangeScrollBar scrollBar)
            {
                return Math.Min((double)value, scrollBar.Maximum);
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
                return Math.Max((double)value, scrollBar.Minimum);
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
                return Math.Clamp((double)value, scrollBar.Minimum, scrollBar.Maximum - scrollBar.Range);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        static bool IsValidDouble(object value)
        {
            if (value is double d)
            {
                return !double.IsNaN(d) && !double.IsInfinity(d);
            }
            else
            {
                return false;
            }
        }
    }
}

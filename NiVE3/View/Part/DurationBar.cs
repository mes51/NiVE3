using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Extension;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class DurationBar : FrameworkElement
    {
        const byte DurationBrushAlpha = 64;

        const double EdgeCursorChangeWidth = 3.0;

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SourceStartPointProperty = DependencyProperty.Register(
            nameof(SourceStartPoint),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty InPointProperty = DependencyProperty.Register(
            nameof(InPoint),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty OutPointProperty = DependencyProperty.Register(
            nameof(OutPoint),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty HasDurationProperty = DependencyProperty.Register(
            nameof(HasDuration),
            typeof(bool),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty IsEnableTimeRemapProperty = DependencyProperty.Register(
            nameof(IsEnableTimeRemap),
            typeof(bool),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty TagColorProperty = DependencyProperty.Register(
            nameof(TagColor),
            typeof(Color),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        private static readonly DependencyPropertyKey IsClickedPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsClicked),
            typeof(bool),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(false, IsClickChangedHandler)
        );

        public static readonly DependencyProperty IsClickedProperty = IsClickedPropertyKey.DependencyProperty;

        public static RoutedEvent IsClickedChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(IsClickedChanged), RoutingStrategy.Direct, typeof(EventHandler), typeof(DurationBar)
        );

        public bool IsClicked
        {
            get { return (bool)GetValue(IsClickedProperty); }
            private set { SetValue(IsClickedPropertyKey, value); }
        }

        public Color TagColor
        {
            get { return (Color)GetValue(TagColorProperty); }
            set { SetValue(TagColorProperty, value); }
        }

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
        }

        public bool IsEnableTimeRemap
        {
            get { return (bool)GetValue(IsEnableTimeRemapProperty); }
            set { SetValue(IsEnableTimeRemapProperty, value); }
        }

        public bool HasDuration
        {
            get { return (bool)GetValue(HasDurationProperty); }
            set { SetValue(HasDurationProperty, value); }
        }

        public double OutPoint
        {
            get { return (double)GetValue(OutPointProperty); }
            set { SetValue(OutPointProperty, value); }
        }

        public double InPoint
        {
            get { return (double)GetValue(InPointProperty); }
            set { SetValue(InPointProperty, value); }
        }

        public double SourceStartPoint
        {
            get { return (double)GetValue(SourceStartPointProperty); }
            set { SetValue(SourceStartPointProperty, value); }
        }

        public double Duration
        {
            get { return (double)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public double Range
        {
            get { return (double)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public double RangeStart
        {
            get { return (double)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public event EventHandler IsClickedChanged
        {
            add { AddHandler(IsClickedChangedEvent, value); }
            remove { RemoveHandler(IsClickedChangedEvent, value); }
        }

        double ClickX { get; set; }

        DurationEditMode EditMode { get; set; }

        Brush DurationBrush { get; set; } = new SolidColorBrush(Color.FromArgb(DurationBrushAlpha, 255, 0, 0)).FreezeCurrentObject();

        Brush EnableAreaBrush { get; set; } = new SolidColorBrush(Colors.Red).FreezeCurrentObject();

        public DurationBar()
        {
            MouseDown += DurationBar_MouseDown;
            MouseMove += DurationBar_MouseMove;
            MouseUp += DurationBar_MouseUp;
        }

        private void DurationBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClicked)
            {
                EditMode = DurationEditMode.None;
                IsClicked = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void DurationBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
                var frameDuration = 1.0 / CompositionFrameRate;
                var posX = e.GetPosition(this).X;
                var diffTime = (int)(((posX - ClickX) * timePerPixel) * CompositionFrameRate) * frameDuration;
                if (diffTime != 0.0)
                {
                    var changed = false;
                    switch (EditMode)
                    {
                        case DurationEditMode.InPoint:
                            {
                                var prev = InPoint;
                                InPoint = Math.Clamp(InPoint + diffTime, 0.0, OutPoint - frameDuration);
                                changed = prev != InPoint;
                            }
                            break;
                        case DurationEditMode.OutPoint:
                            {
                                var prev = OutPoint;
                                if (HasDuration && !IsEnableTimeRemap)
                                {
                                    OutPoint = Math.Clamp(OutPoint + diffTime, InPoint + frameDuration, Duration);
                                }
                                else
                                {
                                    OutPoint = Math.Max(OutPoint + diffTime, InPoint + frameDuration);
                                }
                                changed = prev != OutPoint;
                            }
                            break;
                        case DurationEditMode.SourceStartPoint:
                            SourceStartPoint += diffTime;
                            changed = true;
                            break;
                        case DurationEditMode.Slip:
                            {
                                var newInPoint = 0.0;
                                var newOutPoint = 0.0;
                                if (diffTime > 0.0)
                                {
                                    newInPoint = Math.Clamp(InPoint - diffTime, 0.0, Math.Max(OutPoint - diffTime, 0.0));
                                    diffTime = InPoint - newInPoint;
                                    newOutPoint = Math.Clamp(OutPoint - diffTime, newInPoint + frameDuration, Duration);
                                }
                                else
                                {
                                    newOutPoint = Math.Clamp(OutPoint - diffTime, Math.Min(InPoint - diffTime + frameDuration, Duration), Duration);
                                    diffTime = OutPoint - newOutPoint;
                                    newInPoint = Math.Clamp(InPoint - diffTime, 0.0, newOutPoint - frameDuration);
                                }
                                if (InPoint != newInPoint && OutPoint != newOutPoint)
                                {
                                    SourceStartPoint += diffTime;
                                    InPoint = newInPoint;
                                    OutPoint = newOutPoint;
                                    changed = true;
                                }
                            }
                            break;
                    }
                    if (changed)
                    {
                        ClickX += diffTime / timePerPixel;
                    }
                }
                e.Handled = true;
            }
            else
            {
                Cursor = Cursors.Arrow;

                var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
                if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
                {
                    return;
                }

                var posX = e.GetPosition(this).X;
                var inPointPos = (InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                var outPointPos = (OutPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                if (Math.Abs(inPointPos - posX) <= EdgeCursorChangeWidth || Math.Abs(outPointPos - posX) <= EdgeCursorChangeWidth)
                {
                    Cursor = Cursors.SizeWE;
                    return;
                }

                if (HasDuration)
                {
                    var beforeInPointPos = (SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                    var afterOutPointPos = (Duration + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                    if ((beforeInPointPos <= posX && inPointPos >= posX) || (outPointPos <= posX && afterOutPointPos >= posX))
                    {
                        Cursor = Cursors.ScrollSE;
                    }
                }
            }
        }

        private void DurationBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
            {
                return;
            }

            ClickX = e.GetPosition(this).X;
            var inPointPos = (InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if (Math.Abs(inPointPos - ClickX) <= EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.InPoint;
                IsClicked = true;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            var outPointPos = (OutPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if (Math.Abs(outPointPos - ClickX) <= EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.OutPoint;
                IsClicked = true;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if ((ClickX - inPointPos) > EdgeCursorChangeWidth && (outPointPos - ClickX) > EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.SourceStartPoint;
                IsClicked = true;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (!HasDuration || IsEnableTimeRemap)
            {
                return;
            }

            var beforeInPointPos = (SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            var afterOutPointPos = (Duration + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if ((beforeInPointPos <= ClickX && inPointPos >= ClickX) || (outPointPos <= ClickX && afterOutPointPos >= ClickX))
            {
                EditMode = DurationEditMode.Slip;
                IsClicked = true;
                CaptureMouse();
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
            {
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));

            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));

            if (HasDuration && !IsEnableTimeRemap)
            {
                drawingContext.DrawRectangle(DurationBrush, null, new Rect((SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, Duration * pixelPerTime, ActualHeight));
            }
            drawingContext.DrawRectangle(EnableAreaBrush, null, new Rect((InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, (OutPoint - InPoint) * pixelPerTime, ActualHeight));

            drawingContext.Pop();
        }

        static void TagColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DurationBar durationBar)
            {
                durationBar.DurationBrush = new SolidColorBrush(Color.FromArgb(DurationBrushAlpha, durationBar.TagColor.R, durationBar.TagColor.G, durationBar.TagColor.B)).FreezeCurrentObject();
                durationBar.EnableAreaBrush = new SolidColorBrush(durationBar.TagColor).FreezeCurrentObject();
            }
        }

        static void IsClickChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DurationBar durationBar)
            {
                durationBar.RaiseEvent(new RoutedEventArgs(IsClickedChangedEvent, d));
            }
        }

        private enum DurationEditMode
        {
            None,
            InPoint,
            OutPoint,
            SourceStartPoint,
            Slip
        }
    }
}

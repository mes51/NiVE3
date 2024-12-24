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
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class DurationBar : FrameworkElement
    {
        const byte DurationBrushAlpha = 64;

        const double EdgeCursorChangeWidth = 3.0;

        const double BeforeZeroHatchSize = 7.0;

        const double BeforeZeroHatchWidth = 2.0;

        static readonly DrawingBrush BeforeZeroBrush;

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SourceStartPointProperty = DependencyProperty.Register(
            nameof(SourceStartPoint),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty InPointProperty = DependencyProperty.Register(
            nameof(InPoint),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty OutPointProperty = DependencyProperty.Register(
            nameof(OutPoint),
            typeof(Time),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty HasDurationProperty = DependencyProperty.Register(
            nameof(HasDuration),
            typeof(bool),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );


        public static readonly DependencyProperty IsDisableDurationProperty = DependencyProperty.Register(
            nameof(IsDisableDuration),
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
            new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender, TagColorChanged)
        );

        public static readonly DependencyProperty BeginEditDurationCommandProperty = DependencyProperty.Register(
            nameof(BeginEditDurationCommand),
            typeof(ICommand),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty UpdateDurationCommandProperty = DependencyProperty.Register(
            nameof(UpdateDurationCommand),
            typeof(ICommand),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty AbortEditDurationCommandProperty = DependencyProperty.Register(
            nameof(AbortEditDurationCommand),
            typeof(ICommand),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(null)
        );

        public ICommand? AbortEditDurationCommand
        {
            get { return (ICommand)GetValue(AbortEditDurationCommandProperty); }
            set { SetValue(AbortEditDurationCommandProperty, value); }
        }

        public ICommand? UpdateDurationCommand
        {
            get { return (ICommand)GetValue(UpdateDurationCommandProperty); }
            set { SetValue(UpdateDurationCommandProperty, value); }
        }

        public ICommand? BeginEditDurationCommand
        {
            get { return (ICommand)GetValue(BeginEditDurationCommandProperty); }
            set { SetValue(BeginEditDurationCommandProperty, value); }
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

        public bool IsDisableDuration
        {
            get { return (bool)GetValue(IsDisableDurationProperty); }
            set { SetValue(IsDisableDurationProperty, value); }
        }

        public bool HasDuration
        {
            get { return (bool)GetValue(HasDurationProperty); }
            set { SetValue(HasDurationProperty, value); }
        }

        public Time OutPoint
        {
            get { return (Time)GetValue(OutPointProperty); }
            set { SetValue(OutPointProperty, value); }
        }

        public Time InPoint
        {
            get { return (Time)GetValue(InPointProperty); }
            set { SetValue(InPointProperty, value); }
        }

        public Time SourceStartPoint
        {
            get { return (Time)GetValue(SourceStartPointProperty); }
            set { SetValue(SourceStartPointProperty, value); }
        }

        public Time Duration
        {
            get { return (Time)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public Time Range
        {
            get { return (Time)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public Time RangeStart
        {
            get { return (Time)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        bool IsClicked { get; set; }

        double ClickX { get; set; }

        DurationEditMode EditMode { get; set; }

        Brush DurationBrush { get; set; } = new SolidColorBrush(Color.FromArgb(DurationBrushAlpha, 255, 0, 0)).FreezeCurrentObject();

        Brush EnableAreaBrush { get; set; } = new SolidColorBrush(Colors.Red).FreezeCurrentObject();

        static DurationBar()
        {
            var lineFigure = new PathFigure { IsClosed = true, StartPoint = new Point(BeforeZeroHatchSize - BeforeZeroHatchWidth, 0.0) };
            lineFigure.Segments.Add(new PolyLineSegment(
            [
                new Point(BeforeZeroHatchSize, 0.0),
                new Point(BeforeZeroHatchSize, BeforeZeroHatchWidth),
                new Point(BeforeZeroHatchWidth, BeforeZeroHatchSize),
                new Point(0.0, BeforeZeroHatchSize),
                new Point(0.0, BeforeZeroHatchSize - BeforeZeroHatchWidth)
            ], false));
            var topCornerFigure = new PathFigure { IsClosed = true, StartPoint = new Point() };
            topCornerFigure.Segments.Add(new PolyLineSegment(
            [
                new Point(),
                new Point(BeforeZeroHatchWidth, 0.0),
                new Point(0.0, BeforeZeroHatchWidth)
            ], false));
            var bottomCornerFigure = new PathFigure { IsClosed = true, StartPoint = new Point(BeforeZeroHatchSize, BeforeZeroHatchSize) };
            bottomCornerFigure.Segments.Add(new PolyLineSegment(
            [
                new Point(BeforeZeroHatchSize, BeforeZeroHatchSize),
                new Point(BeforeZeroHatchSize - BeforeZeroHatchWidth, BeforeZeroHatchSize),
                new Point(BeforeZeroHatchSize, BeforeZeroHatchSize - BeforeZeroHatchWidth)
            ], false));

            BeforeZeroBrush = new DrawingBrush
            {
                Drawing = new GeometryDrawing
                {
                    Geometry = new PathGeometry([topCornerFigure, lineFigure, bottomCornerFigure]),
                    Brush = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255))
                },
                Viewport = new Rect(0.0, 0.0, BeforeZeroHatchSize, BeforeZeroHatchSize),
                Viewbox = new Rect(0.0, 0.0, BeforeZeroHatchSize, BeforeZeroHatchSize),
                ViewportUnits = BrushMappingMode.Absolute,
                ViewboxUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile,
            }.FreezeCurrentObject();

            FocusableProperty.OverrideMetadata(typeof(DurationBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
            FocusVisualStyleProperty.OverrideMetadata(typeof(DurationBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        }

        public DurationBar()
        {
            MouseDown += DurationBar_MouseDown;
            MouseMove += DurationBar_MouseMove;
            MouseUp += DurationBar_MouseUp;
            KeyDown += DurationBar_KeyDown;
        }

        private void DurationBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClicked)
            {
                EditMode = DurationEditMode.None;
                IsClicked = false;
                ReleaseMouseCapture();
                UpdateDurationCommand?.Execute(Tuple.Create(Time.Zero, Time.Zero, Time.Zero, true));
                e.Handled = true;
            }
        }

        private void DurationBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                var frameRate = CompositionFrameRate;
                var frameDuration = new Time(1, frameRate);
                var diffTime = TimeCalc.CalcTimeFromPixel(e.GetPosition(this).X - ClickX, ActualWidth, Range, Time.Zero);
                var globalTime = TimeCalc.CalcTimeFromPixelAligned(e.GetPosition(this).X - UIParameters.TimelineRangeThumbWidth, ActualWidth, Range, RangeStart, frameRate);
                if (diffTime != 0.0)
                {
                    var changed = false;
                    switch (EditMode)
                    {
                        case DurationEditMode.InPoint:
                            {
                                var prev = InPoint;
                                var newTime = globalTime.RoundToFrameRate(frameRate) - SourceStartPoint;
                                var max = (OutPoint - frameDuration).FloorToFrameRate(frameRate);
                                var newInPoint = Time.Min(newTime, max);
                                if (HasDuration && !IsDisableDuration)
                                {
                                    max = Time.Max(max, Time.Zero);
                                    newInPoint = Time.MaxAndMin(newTime, Time.Zero, max);
                                }
                                changed = prev != newInPoint;
                                diffTime = newInPoint - prev;
                                UpdateDurationCommand?.Execute(Tuple.Create(diffTime, Time.Zero, Time.Zero, false));
                            }
                            break;
                        case DurationEditMode.OutPoint:
                            {
                                var prev = OutPoint;
                                var newTime = globalTime.RoundToFrameRate(frameRate) - SourceStartPoint;
                                var min = (InPoint + frameDuration).FloorToFrameRate(frameRate);
                                var newOutPoint = Time.Max(newTime, min);
                                if (HasDuration && !IsDisableDuration)
                                {
                                    min = Time.Min(min, Duration);
                                    newOutPoint = Time.MaxAndMin(newTime, min, Duration);
                                }
                                changed = prev != newOutPoint;
                                diffTime = newOutPoint - prev;
                                UpdateDurationCommand?.Execute(Tuple.Create(Time.Zero, diffTime, Time.Zero, false));
                            }
                            break;
                        case DurationEditMode.SourceStartPoint:
                            {
                                var subFrameTime = SourceStartPoint - SourceStartPoint.FloorToFrameRate(frameRate);
                                var prev = SourceStartPoint;
                                var newSourceStartPoint = (SourceStartPoint + diffTime).FloorToFrameRate(frameRate) + subFrameTime;
                                diffTime = newSourceStartPoint - prev;
                                changed = prev != newSourceStartPoint;
                                UpdateDurationCommand?.Execute(Tuple.Create(Time.Zero, Time.Zero, diffTime, false));
                            }
                            break;
                        case DurationEditMode.Slip:
                            {
                                diffTime = diffTime.RoundToFrameRate(frameRate);
                                var newInPoint = Time.Zero;
                                var newOutPoint = Time.Zero;
                                if (diffTime > Time.Zero)
                                {
                                    newInPoint = Time.Max(InPoint - diffTime, Time.Zero);
                                    diffTime = InPoint - newInPoint;
                                    newOutPoint = Time.Max(OutPoint - diffTime, newInPoint + frameDuration);
                                }
                                else
                                {
                                    newOutPoint = Time.Min(OutPoint - diffTime, Duration);
                                    diffTime = OutPoint - newOutPoint;
                                    newInPoint = Time.Max(InPoint - diffTime, Time.Zero);
                                }
                                if (InPoint != newInPoint && OutPoint != newOutPoint)
                                {
                                    UpdateDurationCommand?.Execute(Tuple.Create(Time.Zero, Time.Zero, diffTime, false));
                                    changed = true;
                                }
                            }
                            break;
                    }
                    if (changed)
                    {
                        var timePerPixel = (double)Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
                        ClickX += (double)diffTime / timePerPixel;
                    }
                }
                e.Handled = true;
            }
            else
            {
                Cursor = Cursors.Arrow;

                var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
                if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
                {
                    return;
                }

                var posX = e.GetPosition(this).X;
                var inPointPos = (InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                var outPointPos = (OutPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
                if (Time.Abs(inPointPos - posX) <= EdgeCursorChangeWidth || Time.Abs(outPointPos - posX) <= EdgeCursorChangeWidth)
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
            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
            {
                return;
            }

            ClickX = e.GetPosition(this).X;
            var inPointPos = (double)(InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if (Math.Abs(inPointPos - ClickX) <= EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.InPoint;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(BeginEditDurationEventArgs.DurationType.InPoint);
                e.Handled = true;
                return;
            }

            var outPointPos = (double)(OutPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if (Math.Abs(outPointPos - ClickX) <= EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.OutPoint;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(BeginEditDurationEventArgs.DurationType.OutPoint);
                e.Handled = true;
                return;
            }

            if ((ClickX - inPointPos) > EdgeCursorChangeWidth && (outPointPos - ClickX) > EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.SourceStartPoint;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(BeginEditDurationEventArgs.DurationType.SourceStartPoint);
                e.Handled = true;
                return;
            }

            if (!HasDuration || IsDisableDuration)
            {
                return;
            }

            var beforeInPointPos = (SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            var afterOutPointPos = (Duration + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if ((beforeInPointPos <= ClickX && inPointPos >= ClickX) || (outPointPos <= ClickX && afterOutPointPos >= ClickX))
            {
                EditMode = DurationEditMode.Slip;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(BeginEditDurationEventArgs.DurationType.Slip);
                e.Handled = true;
            }
        }

        private void DurationBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && IsClicked)
            {
                EditMode = DurationEditMode.None;
                IsClicked = false;
                ReleaseMouseCapture();
                Keyboard.ClearFocus();
                AbortEditDurationCommand?.Execute(null);
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime))
            {
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));

            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));

            if (HasDuration && !IsDisableDuration)
            {
                drawingContext.DrawRectangle(DurationBrush, null, new Rect((double)(SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, (double)Duration * pixelPerTime, ActualHeight));
            }
            drawingContext.DrawRectangle(EnableAreaBrush, null, new Rect((double)(InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, (double)(OutPoint - InPoint) * pixelPerTime, ActualHeight));
            if ((!HasDuration || IsDisableDuration) && InPoint < 0.0)
            {
                drawingContext.DrawRectangle(BeforeZeroBrush, null, new Rect((double)(InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, -(double)InPoint * pixelPerTime, ActualHeight));
            }

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

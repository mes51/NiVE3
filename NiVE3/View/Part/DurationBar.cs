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
            new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender, TagColorChanged)
        );

        public static readonly DependencyProperty BeginEditDurationCommandProperty = DependencyProperty.Register(
            nameof(BeginEditDurationCommand),
            typeof(ICommand),
            typeof(DurationBar),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty EndEditDurationCommandProperty = DependencyProperty.Register(
            nameof(EndEditDurationCommand),
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

        public ICommand? EndEditDurationCommand
        {
            get { return (ICommand)GetValue(EndEditDurationCommandProperty); }
            set { SetValue(EndEditDurationCommandProperty, value); }
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
                Keyboard.ClearFocus();
                EndEditDurationCommand?.Execute(null);
                e.Handled = true;
            }
        }

        private void DurationBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                var frameRate = CompositionFrameRate;
                var frameDuration = 1.0 / frameRate;
                var diffTime = TimeCalc.CalcTimeFromPixel(e.GetPosition(this).X - ClickX, ActualWidth, Range, 0.0);
                var globalTime = TimeCalc.CalcTimeFromPixelAligned(e.GetPosition(this).X - UIParameters.TimelineRangeThumbWidth, ActualWidth, Range, RangeStart, frameRate);
                if (diffTime != 0.0)
                {
                    var changed = false;
                    switch (EditMode)
                    {
                        case DurationEditMode.InPoint:
                            {
                                var prev = InPoint;
                                var newTime = TimeCalc.AlignRound(globalTime, frameRate) - SourceStartPoint;
                                var max = TimeCalc.AlignFloor(OutPoint - frameDuration, frameRate);
                                if (HasDuration && !IsEnableTimeRemap)
                                {
                                    max = Math.Max(max, 0.0);
                                    InPoint = Math.Min(Math.Max(newTime, 0.0), max);
                                }
                                else
                                {
                                    InPoint = Math.Min(newTime, max);
                                }
                                changed = prev != InPoint;
                                diffTime = InPoint - prev;
                            }
                            break;
                        case DurationEditMode.OutPoint:
                            {
                                var prev = OutPoint;
                                var newTime = TimeCalc.AlignRound(globalTime, frameRate) - SourceStartPoint;
                                var min = TimeCalc.AlignFloor(InPoint + frameDuration, frameRate);
                                if (HasDuration && !IsEnableTimeRemap)
                                {
                                    min = Math.Min(min, Duration);
                                    OutPoint = Math.Min(Math.Max(newTime, min), Duration);
                                }
                                else
                                {
                                    OutPoint = Math.Max(newTime, min);
                                }
                                changed = prev != OutPoint;
                                diffTime = OutPoint - prev;
                            }
                            break;
                        case DurationEditMode.SourceStartPoint:
                            {
                                var subFrameTime = SourceStartPoint - TimeCalc.AlignFloor(SourceStartPoint, frameRate);
                                var prev = SourceStartPoint;
                                SourceStartPoint = TimeCalc.AlignFloor(SourceStartPoint + diffTime, frameRate) + subFrameTime;
                                diffTime = SourceStartPoint - prev;
                                changed = prev != SourceStartPoint;
                            }
                            break;
                        case DurationEditMode.Slip:
                            {
                                diffTime = TimeCalc.AlignRound(diffTime, frameRate);
                                var newInPoint = 0.0;
                                var newOutPoint = 0.0;
                                if (diffTime > 0.0)
                                {
                                    newInPoint = Math.Max(InPoint - diffTime, 0.0);
                                    diffTime = InPoint - newInPoint;
                                    newOutPoint = Math.Max(OutPoint - diffTime, newInPoint + frameDuration);
                                }
                                else
                                {
                                    newOutPoint = Math.Min(OutPoint - diffTime, Duration);
                                    diffTime = OutPoint - newOutPoint;
                                    newInPoint = Math.Max(InPoint - diffTime, 0.0);
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
                        var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
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
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(null);
                e.Handled = true;
                return;
            }

            var outPointPos = (OutPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth;
            if (Math.Abs(outPointPos - ClickX) <= EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.OutPoint;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(null);
                e.Handled = true;
                return;
            }

            if ((ClickX - inPointPos) > EdgeCursorChangeWidth && (outPointPos - ClickX) > EdgeCursorChangeWidth)
            {
                EditMode = DurationEditMode.SourceStartPoint;
                IsClicked = true;
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(null);
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
                Keyboard.Focus(this);
                CaptureMouse();
                BeginEditDurationCommand?.Execute(null);
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
            if ((!HasDuration || IsEnableTimeRemap) && InPoint < 0.0)
            {
                drawingContext.DrawRectangle(BeforeZeroBrush, null, new Rect((InPoint + SourceStartPoint - RangeStart) * pixelPerTime + UIParameters.TimelineRangeThumbWidth, 0.0, -InPoint * pixelPerTime, ActualHeight));
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

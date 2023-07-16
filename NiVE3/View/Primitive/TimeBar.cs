using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NiVE3.Extension;

namespace NiVE3.View.Primitive
{
    class TimeBar : Control
    {
        const double MinGap = 75.0;

        static readonly int[] CountScale = new int[] { 1, 2, 5, 10, 20, 50, 60, 120, 240, 480, 960, 1920 };

        public static readonly DependencyProperty SideSpacerWidthProperty = DependencyProperty.Register(
            nameof(SideSpacerWidth),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SideSpacerBrushProperty = DependencyProperty.Register(
            nameof(SideSpacerBrush),
            typeof(Brush),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty MeasureLineWidthProperty = DependencyProperty.Register(
            nameof(MeasureLineWidth),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, MeasureLinePropertyChanged)
        );

        private static readonly DependencyPropertyKey MinimumRangePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MinimumRange),
            typeof(double),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(0.01, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MinimumRangeProperty = MinimumRangePropertyKey.DependencyProperty;

        public double MeasureLineWidth
        {
            get { return (double)GetValue(MeasureLineWidthProperty); }
            set { SetValue(MeasureLineWidthProperty, value); }
        }

        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
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

        public double Duration
        {
            get { return (double)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public Brush SideSpacerBrush
        {
            get { return (Brush)GetValue(SideSpacerBrushProperty); }
            set { SetValue(SideSpacerBrushProperty, value); }
        }

        public double SideSpacerWidth
        {
            get { return (double)GetValue(SideSpacerWidthProperty); }
            set { SetValue(SideSpacerWidthProperty, value); }
        }

        public double MinimumRange
        {
            get { return (double)GetValue(MinimumRangeProperty); }
            private set { SetValue(MinimumRangePropertyKey, value); }
        }

        Pen MeasreLinePen { get; set; } = new Pen(SystemColors.ControlTextBrush, 1.0);

        static TimeBar()
        {
            FontSizeProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
            ClipToBoundsProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            ForegroundProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, MeasureLinePropertyChanged));

            EventManager.RegisterClassHandler(typeof(TimeBar), SizeChangedEvent, new SizeChangedEventHandler(SizeChangedEventHandler));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var timePerPixel = Range / (ActualWidth - SideSpacerWidth * 2.0);
            if (timePerPixel <= 0.0)
            {
                return;
            }

            var textWidth = this.CreateFormattedText("00:00s", Foreground).Width;

            var (timeUnit, measureTime) = (timePerPixel * MinGap) switch
            {
                var g when (g >= 3600.0) => (TimeUnit.Hour, g / 3600.0),
                var g when (g >= 60.0) => (TimeUnit.Minute, g / 60.0),
                var g when (g >= 1.0) => (TimeUnit.Second, g),
                var g => (TimeUnit.Frame, g * FrameRate),
            };

            var halfScale = timeUnit switch
            {
                TimeUnit.Frame => (int)(FrameRate * 0.5),
                TimeUnit.Second => 30,
                TimeUnit.Minute => 30,
                _ => int.MaxValue
            };
            var selectableScales = CountScale.TakeWhile(s => s < halfScale);
            var scale = selectableScales.FirstOrDefault(s => s >= measureTime, halfScale);
            var forceFullFormat = scale == halfScale;
            var timePerGap = (double)scale;
            switch (timeUnit)
            {
                case TimeUnit.Frame:
                    timePerGap /= FrameRate;
                    break;
                case TimeUnit.Minute:
                    timePerGap *= 60.0;
                    break;
                case TimeUnit.Hour:
                    timePerGap *= 3600.0;
                    break;
            }
            if (timePerGap / timePerPixel < MinGap)
            {
                (timeUnit, timePerGap) = timeUnit switch
                {
                    TimeUnit.Frame => (TimeUnit.Second, 1.0),
                    TimeUnit.Second => (TimeUnit.Minute, 60.0),
                    TimeUnit.Minute => (TimeUnit.Hour, 3600.0),
                    _ => (TimeUnit.Hour, timePerGap)
                };
            }

            var minTextWidth = timePerGap / timePerPixel;
            var rangeStartX = -RangeStart / timePerPixel;

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));

            drawingContext.DrawRectangle(SideSpacerBrush, null, new Rect(rangeStartX, 0.0, SideSpacerWidth, ActualHeight));
            drawingContext.DrawRectangle(SideSpacerBrush, null, new Rect(rangeStartX + Duration / timePerPixel + SideSpacerWidth, 0.0, SideSpacerWidth, ActualHeight));
            for (double w = (rangeStartX % minTextWidth) + SideSpacerWidth, limit = ActualWidth + textWidth; w <= limit; w += minTextWidth)
            {
                var time = Math.Round(timePerPixel * (w - SideSpacerWidth) + RangeStart, 7); // TODO: 7桁で足りるか?
                var timeText = CreateTimeText(time, timeUnit, forceFullFormat);
                var formattedText = this.CreateFormattedText(timeText, Foreground);
                drawingContext.DrawText(formattedText, new Point(w - formattedText.Width * 0.5, ActualHeight - formattedText.Height - 5));

                drawingContext.DrawLine(MeasreLinePen, new Point(w, ActualHeight - 5), new Point(w, ActualHeight));
            }

            drawingContext.Pop();
        }

        string CreateTimeText(double time, TimeUnit unit, bool forceLongFormat)
        {
            switch (unit)
            {
                case TimeUnit.Frame:
                    {
                        var frame = Math.Round(time * FrameRate);
                        var second = (int)Math.Floor(frame / FrameRate);
                        frame -= second * FrameRate;
                        if (forceLongFormat || frame < 1.0)
                        {
                            return $"{second % 60:D02}:{(int)frame:D02}f";
                        }
                        else
                        {
                            return $"{(int)frame:D02}f";
                        }
                    }
                case TimeUnit.Second:
                    {
                        var second = (int)Math.Floor(time);
                        var minute = (second / 60) % 60;
                        if (forceLongFormat || second % 60 == 0)
                        {
                            return $"{minute % 60:D02}:{second % 60:D02}s";
                        }
                        else
                        {
                            return $"{second % 60:D02}s";
                        }
                    }
                case TimeUnit.Minute:
                    {
                        var minute = (int)Math.Floor(time / 60.0);
                        var hour = (minute / 60) % 60;
                        if (forceLongFormat || minute % 60 == 0)
                        {
                            return $"{hour:D02}:{minute % 60:D02}m";
                        }
                        else
                        {
                            return $"{minute % 60:D02}m";
                        }
                    }
                case TimeUnit.Hour:
                    return $"{(int)Math.Round(time / 3600.0):D02}h";
                default:
                    return "";
            }
        }

        static void MeasureLinePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TimeBar timeBar)
            {
                timeBar.MeasreLinePen = new Pen(timeBar.Foreground, timeBar.MeasureLineWidth);
            }
        }

        static void SizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            if (sender is TimeBar timeBar)
            {
                timeBar.MinimumRange = (timeBar.ActualWidth - timeBar.SideSpacerWidth * 2.0) * (1.0 / timeBar.FrameRate / MinGap);
            }
        }

        private enum TimeUnit
        {
            Frame,
            Second,
            Minute,
            Hour
        }
    }
}

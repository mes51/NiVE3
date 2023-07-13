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

        Pen MeasreLinePen { get; set; } = new Pen(SystemColors.ControlTextBrush, 1.0);

        static TimeBar()
        {
            FontSizeProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
            ClipToBoundsProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            ForegroundProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, MeasureLinePropertyChanged));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));

            var timePerPixel = Range / ActualWidth;
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
            for (double w = -((RangeStart / timePerPixel) % minTextWidth), limit = ActualWidth + textWidth; w <= limit; w += minTextWidth)
            {
                var time = Math.Round(timePerPixel * w + RangeStart, 7); // TODO: 7桁で足りるか?
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
                            return $"{second:D02}:{(int)frame:D02}f";
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
                            return $"{minute:D02}:{second % 60:D02}s";
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

        private enum TimeUnit
        {
            Frame,
            Second,
            Minute,
            Hour
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Cache;
using NiVE3.Util;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class CachedFrameIndicator : FrameworkElement
    {
        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(
            nameof(CurrentTime),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CacheLineBrushProperty = DependencyProperty.Register(
            nameof(CacheLineBrush),
            typeof(Brush),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(Brushes.LimeGreen, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            nameof(LineWidth),
            typeof(double),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty TargetObjectIdProperty = DependencyProperty.Register(
            nameof(TargetObjectId),
            typeof(Guid),
            typeof(CachedFrameIndicator),
            new FrameworkPropertyMetadata(Guid.Empty, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public Guid TargetObjectId
        {
            get { return (Guid)GetValue(TargetObjectIdProperty); }
            set { SetValue(TargetObjectIdProperty, value); }
        }

        public double LineWidth
        {
            get { return (double)GetValue(LineWidthProperty); }
            set { SetValue(LineWidthProperty, value); }
        }

        public Brush CacheLineBrush
        {
            get { return (Brush)GetValue(CacheLineBrushProperty); }
            set { SetValue(CacheLineBrushProperty, value); }
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

        static CachedFrameIndicator()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(CachedFrameIndicator), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            IsHitTestVisibleProperty.OverrideMetadata(typeof(CachedFrameIndicator), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var width = ActualWidth;
            var timePerPixel = Range / (width - UIParameters.TimelineRangeThumbTotalWidth);
            if (timePerPixel <= 0.0)
            {
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, width, ActualHeight)));

            var brush = CacheLineBrush;
            var lineWidth = LineWidth;
            var y = ActualHeight - lineWidth;
            var frameRate = FrameRate;
            var pixelPerFrame = 1.0 / frameRate / timePerPixel;
            var rangeStartX = -RangeStart / timePerPixel + UIParameters.TimelineRangeThumbWidth;
            var cacheStarted = -1;
            var cachedFrames = 0;
            foreach (var frame in ImageCache.GetCachedTime(TargetObjectId))
            {
                var currentFrame = (int)Math.Round(frame * frameRate);
                if (cacheStarted + cachedFrames == currentFrame)
                {
                    cachedFrames++;
                    continue;
                }
                else if (cachedFrames > 0)
                {
                    DrawCachedLine(drawingContext, brush, width, y, lineWidth, rangeStartX, pixelPerFrame, cacheStarted, cachedFrames);
                }

                cacheStarted = currentFrame;
                cachedFrames = 1;
            }

            if (cachedFrames > 0)
            {
                DrawCachedLine(drawingContext, brush, width, y, lineWidth, rangeStartX, pixelPerFrame, cacheStarted, cachedFrames);
            }

            drawingContext.Pop();
        }

        static void DrawCachedLine(DrawingContext drawingContext, Brush brush, double actualWidth, double y, double lineWidth, double startX, double pixelPerFrame, int startFrame, int frameCount)
        {
            var width = frameCount * pixelPerFrame;
            var x = startFrame * pixelPerFrame + startX;

            if (x + width > 0.0 && x < actualWidth)
            {
                drawingContext.DrawRectangle(brush, null, new Rect(x, y, width, lineWidth));
            }
        }
    }
}

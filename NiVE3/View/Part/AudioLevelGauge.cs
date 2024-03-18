using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NiVE3.Extension;
using NiVE3.View.Primitive;

namespace NiVE3.View.Part
{
    class AudioLevelGauge : TextRenderableElement
    {
        const double LineDegenerationThresholdHeight = 3.0;

        const double LargeMeasureLineMargin = 3.0;

        public static readonly DependencyProperty MinimumLevelProperty = DependencyProperty.Register(
            nameof(MinimumLevel),
            typeof(double),
            typeof(AudioLevelGauge),
            new FrameworkPropertyMetadata(-60.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty LargeMeasureLineIntervalProperty = DependencyProperty.Register(
            nameof(LargeMeasureLineInterval),
            typeof(int),
            typeof(AudioLevelGauge),
            new FrameworkPropertyMetadata(6, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty GaugeLeftPaddingProperty = DependencyProperty.Register(
            nameof(GaugeLeftPadding),
            typeof(double),
            typeof(AudioLevelGauge),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty GaugeRightPaddingProperty = DependencyProperty.Register(
            nameof(GaugeRightPadding),
            typeof(double),
            typeof(AudioLevelGauge),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public double GaugeRightPadding
        {
            get { return (double)GetValue(GaugeRightPaddingProperty); }
            set { SetValue(GaugeRightPaddingProperty, value); }
        }

        public double GaugeLeftPadding
        {
            get { return (double)GetValue(GaugeLeftPaddingProperty); }
            set { SetValue(GaugeLeftPaddingProperty, value); }
        }

        public int LargeMeasureLineInterval
        {
            get { return (int)GetValue(LargeMeasureLineIntervalProperty); }
            set { SetValue(LargeMeasureLineIntervalProperty, value); }
        }

        public double MinimumLevel
        {
            get { return (double)GetValue(MinimumLevelProperty); }
            set { SetValue(MinimumLevelProperty, value); }
        }

        Pen MeasureLinePen { get; set; } = new Pen(SystemColors.ControlTextBrush, 1.0).FreezeCurrentObject();

        static AudioLevelGauge()
        {
            FontSizeProperty.OverrideMetadata(typeof(AudioLevelGauge), new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.Inherits));
            ForegroundProperty.OverrideMetadata(typeof(AudioLevelGauge), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits, ForegroundChanged));
            ClipToBoundsProperty.OverrideMetadata(typeof(AudioLevelGauge), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var paddedWidth = ActualWidth - GaugeLeftPadding - GaugeRightPadding;
            var gaugeX = GaugeLeftPadding;

            var bottomLevel = -MinimumLevel;
            var pixelPerLevel = paddedWidth / bottomLevel;
            var textHeight = this.CreateFormattedText($"{MinimumLevel}db", Foreground).Height;
            var largeMeasureLineHeight = ActualHeight - textHeight - LargeMeasureLineMargin;
            if (textHeight > ActualHeight)
            {
                largeMeasureLineHeight = ActualHeight;
            }

            var lineCount = (int)(Math.Floor(bottomLevel) / LargeMeasureLineInterval);
            var lineInterval = LargeMeasureLineInterval * pixelPerLevel;
            var alignedStartX = (bottomLevel - (int)bottomLevel) * pixelPerLevel + gaugeX;
            if (largeMeasureLineHeight > LineDegenerationThresholdHeight)
            {
                drawingContext.DrawLine(MeasureLinePen, new Point(gaugeX, 0.0), new Point(gaugeX, largeMeasureLineHeight));
                drawingContext.DrawLine(MeasureLinePen, new Point(paddedWidth + gaugeX, 0.0), new Point(paddedWidth + gaugeX, largeMeasureLineHeight));

                for (var i = 0; i <= lineCount; i++)
                {
                    var x = alignedStartX + i * lineInterval;
                    drawingContext.DrawLine(MeasureLinePen, new Point(x, 0.0), new Point(x, largeMeasureLineHeight));
                }
            }

            if (textHeight <= ActualHeight)
            {
                var textY = ActualHeight - textHeight;
                var formattedText = this.CreateFormattedText($"{MinimumLevel}dB", Foreground);
                var textX = (ClipToBounds ? 0.0 : -formattedText.Width * 0.5) + gaugeX;
                drawingContext.DrawText(formattedText, new Point(textX, textY));

                formattedText = this.CreateFormattedText("0dB", Foreground);
                var maxLevelTextStart = paddedWidth - (ClipToBounds ? formattedText.Width : formattedText.Width * 0.5) + gaugeX;
                drawingContext.DrawText(formattedText, new Point(maxLevelTextStart, textY));

                var prevTextEnd = textX + formattedText.Width;
                formattedText = this.CreateFormattedText($"-18dB", Foreground);
                textX = (bottomLevel - 18.0) * pixelPerLevel + alignedStartX - formattedText.Width * 0.5;
                if (prevTextEnd < textX)
                {
                    drawingContext.DrawText(formattedText, new Point(textX, textY));
                    prevTextEnd = textX + formattedText.Width;
                }

                formattedText = this.CreateFormattedText($"-6dB", Foreground);
                textX = (bottomLevel - 6.0) * pixelPerLevel + alignedStartX - formattedText.Width * 0.5;
                if (prevTextEnd < textX && maxLevelTextStart > textX + formattedText.Width)
                {
                    drawingContext.DrawText(formattedText, new Point(textX, textY));
                }
            }
        }

        static void ForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AudioLevelGauge gauge)
            {
                gauge.MeasureLinePen = new Pen(gauge.Foreground, 1.0).FreezeCurrentObject();
            }
        }
    }
}

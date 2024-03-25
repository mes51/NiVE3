using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class AudioLevelMeter : FrameworkElement
    {
        const double MinimumLevel = 60.0;

        const int SegmentGroupCount = 10;

        const double OverZeroMarkerWidth = 8.0;

        const double OverZeroMarkerLeftMargin = 4.0;

        const double SegmentMargin = 2.0;

        const double MinimumSegmentWidth = 4.0;

        const int Level1SegmentGroupStart = 0;

        const int Level2SegmentGroupStart = 7;

        const int Level3SegmentGroupStart = 8;

        const int Level4SegmentGroupStart = 9;

        public static readonly DependencyProperty AudioLevelProperty = DependencyProperty.Register(
            nameof(AudioLevel),
            typeof(double),
            typeof(AudioLevelMeter),
            new FrameworkPropertyMetadata(-60.0, FrameworkPropertyMetadataOptions.AffectsRender, AudioLevelChanged)
        );

        private static readonly DependencyProperty IsOverZeroDecibelProperty = DependencyProperty.Register(
            nameof(IsOverZeroDecibel),
            typeof(bool),
            typeof(AudioLevelMeter),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public double AudioLevel
        {
            get { return (double)GetValue(AudioLevelProperty); }
            set { SetValue(AudioLevelProperty, value); }
        }

        private bool IsOverZeroDecibel
        {
            get { return (bool)GetValue(IsOverZeroDecibelProperty); }
            set { SetValue(IsOverZeroDecibelProperty, value); }
        }

        public AudioLevelMeter()
        {
            MouseDown += AudioLevelMeter_MouseDown;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));

            var segmentAreaWidth = ActualWidth - OverZeroMarkerWidth - OverZeroMarkerLeftMargin + SegmentMargin;
            var sixDecibelWidth = segmentAreaWidth / SegmentGroupCount;
            var segmentCount = (int)(sixDecibelWidth / (SegmentMargin + MinimumSegmentWidth));
            var totalSegmentCount = segmentCount * SegmentGroupCount;
            var decibelPerSegment = (MinimumLevel / SegmentGroupCount) / segmentCount;
            var segmentWidth = (sixDecibelWidth / segmentCount) - SegmentMargin;
            var enableSegmentCount = Math.Clamp(1.0 + AudioLevel / MinimumLevel, 0.0, 1.0) * totalSegmentCount;

            for (var i = 0; i < totalSegmentCount; i++)
            {
                var x = i * (SegmentMargin + segmentWidth);
                var interSegmentLevel = 1.0 - Math.Clamp(enableSegmentCount - i, 0.0, 1.0);

                var (disableBrushKey, enableBrushKey) = (i / segmentCount) switch
                {
                    int gc when gc >= Level2SegmentGroupStart && gc < Level3SegmentGroupStart => (AppearanceResourceDictionary.AudioLevelMeter2DisableFill, AppearanceResourceDictionary.AudioLevelMeter2EnableFill),
                    int gc when gc >= Level3SegmentGroupStart && gc < Level4SegmentGroupStart => (AppearanceResourceDictionary.AudioLevelMeter3DisableFill, AppearanceResourceDictionary.AudioLevelMeter3EnableFill),
                    int gc when gc >= Level4SegmentGroupStart => (AppearanceResourceDictionary.AudioLevelMeter4DisableFill, AppearanceResourceDictionary.AudioLevelMeter4EnableFill),
                    _ => (AppearanceResourceDictionary.AudioLevelMeter1DisableFill, AppearanceResourceDictionary.AudioLevelMeter1EnableFill)
                };

                var disableBrush = AppearanceResourceDictionary.Dictionary.GetBrush(disableBrushKey);
                var enableBrush = AppearanceResourceDictionary.Dictionary.GetBrush(enableBrushKey);
                drawingContext.DrawRectangle(i < enableSegmentCount ? enableBrush : disableBrush, null, new Rect(x, 0.0, segmentWidth, ActualHeight));
            }

            drawingContext.DrawRectangle(
                AppearanceResourceDictionary.Dictionary.GetBrush(IsOverZeroDecibel ? AppearanceResourceDictionary.AudioLevelMeterOverZeroEnableFill : AppearanceResourceDictionary.AudioLevelMeterOverZeroDisableFill),
                null,
                new Rect(ActualWidth - OverZeroMarkerWidth, 0.0, OverZeroMarkerWidth, ActualHeight)
            );
        }

        private void AudioLevelMeter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var xFromMarker = ActualWidth - e.GetPosition(this).X;
            if (xFromMarker >= 0.0 && xFromMarker < OverZeroMarkerWidth)
            {
                IsOverZeroDecibel = false;
            }
        }

        static void AudioLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AudioLevelMeter audioLevelMeter)
            {
                var levelRate = Math.Max((MinimumLevel + audioLevelMeter.AudioLevel) / MinimumLevel, 0.0);
                audioLevelMeter.IsOverZeroDecibel |= levelRate >= 1.0;
            }
        }
    }
}

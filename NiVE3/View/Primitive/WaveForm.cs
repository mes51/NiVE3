using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Shared.Extension;
using NiVE3.Util;
using NiVE3.ViewModel;

namespace NiVE3.View.Primitive
{
    class WaveForm : FrameworkElement
    {
        const double MinimumLevel = 60.0;

        const double WaveChannelMargin = 5.0;

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(WaveForm),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(WaveForm),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(WaveForm),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(WaveForm),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty ThumbSizeProperty = DependencyProperty.Register(
            nameof(ThumbSize),
            typeof(double),
            typeof(WaveForm),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public double ThumbSize
        {
            get { return (double)GetValue(ThumbSizeProperty); }
            set { SetValue(ThumbSizeProperty, value); }
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

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        LayerViewModel? ViewModel => DataContext as LayerViewModel;

        public WaveForm()
        {
            DataContextChanged += WaveForm_DataContextChanged;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Background, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));

            var viewModel = ViewModel;
            if (ActualHeight <= WaveChannelMargin || ActualWidth < ThumbSize * 2.0 || Range <= 0.0 || viewModel == null)
            {
                return;
            }

            var width = ActualWidth;
            var marginTime = ThumbSize * (Range / (width - ThumbSize * 2.0));
            var audio = viewModel.GetAudio(RangeStart - marginTime, Range + marginTime * 2.0);
            var (leftLevels, rightLevels, isSampleTime) = GetWaveFormLevels(audio, (int)width);

            var renderableHeight = ActualHeight * 0.5 - WaveChannelMargin;

            drawingContext.DrawGeometry(Foreground, null, CreateWaveFormGeometry(leftLevels, isSampleTime, width, renderableHeight, Range));

            drawingContext.PushTransform(new TranslateTransform(0.0, ActualHeight - renderableHeight));
            drawingContext.DrawGeometry(Foreground, null, CreateWaveFormGeometry(rightLevels, isSampleTime, width, renderableHeight, Range));
            drawingContext.Pop();
        }

        private void WaveForm_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LayerViewModel oldLayer)
            {
                oldLayer.PropertyChanged -= LayerViewModel_PropertyChanged;
            }
            if (e.NewValue is LayerViewModel newLayer)
            {
                newLayer.PropertyChanged += LayerViewModel_PropertyChanged;
            }
        }

        private void LayerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerViewModel.SourceStartPoint) || e.PropertyName == nameof(LayerViewModel.InPoint) || e.PropertyName == nameof(LayerViewModel.OutPoint))
            {
                InvalidateVisual();
            }
        }

        static (float[] leftLevels, float[] rightLevels, bool isSampleTime) GetWaveFormLevels(float[] audio, int width)
        {
            var channelLength = audio.Length / Const.AudioChannelCount;

            var leftChannel = ArrayPool<float>.Shared.Rent(channelLength);
            var rightChannel = ArrayPool<float>.Shared.Rent(channelLength);

            var audioSpan = audio.AsSpan();
            var audioVectorSpan = MemoryMarshal.Cast<float, Vector128<float>>(audioSpan[..((audio.Length / Vector128<float>.Count / 2) * Vector128<float>.Count * 2)]);
            var leftChannelVectorSpan = MemoryMarshal.Cast<float, Vector4>(leftChannel.AsSpan(0, (channelLength / 4) * 4));
            var rightChannelVectorSpan = MemoryMarshal.Cast<float, Vector4>(rightChannel.AsSpan(0, (channelLength / 4) * 4));
            for (int vi = 0, cvi = 0; vi < audioVectorSpan.Length; vi += 2, cvi++)
            {
                var a1 = audioVectorSpan[vi];
                var a2 = audioVectorSpan[vi + 1];
                var left = Sse.Shuffle(a1, a2, 0b10101010);
                var right = Sse.Shuffle(a1, a2, 0b01010101);
                leftChannelVectorSpan[cvi] = Vector4.Abs(left.AsVector4());
                rightChannelVectorSpan[cvi] = Vector4.Abs(right.AsVector4());
            }
            for (int i = audioVectorSpan.Length * Vector128<float>.Count, ci = leftChannelVectorSpan.Length * 4; i < audioSpan.Length; i += 2, ci++)
            {
                leftChannel[ci] = Math.Abs(audioSpan[i]);
                rightChannel[ci] = Math.Abs(audioSpan[i + 1]);
            }

            if (width >= channelLength)
            {
                var leftResult = new float[channelLength];
                var rightResult = new float[channelLength];
                leftChannel.AsSpan(0, channelLength).CopyTo(leftResult);
                rightChannel.AsSpan(0, channelLength).CopyTo(rightResult);
                return (leftResult, rightResult, true);
            }

            var groupSize = channelLength / (double)width;
            var leftLevel = new float[width];
            var rightLevel = new float[width];
            var leftChannelSpan = leftChannel.AsSpan(0, channelLength);
            var rightChannelSpan = rightChannel.AsSpan(0, channelLength);

            if (groupSize >= Vector<float>.Count * 2)
            {
                for (var i = 0; i < width; i++)
                {
                    var start = (int)(i * groupSize);
                    var end = (int)((i + 1) * groupSize);
                    var lmax = new Vector<float>(leftChannelSpan[start..]);
                    var rmax = new Vector<float>(rightChannelSpan[start..]);
                    start += Vector<float>.Count;

                    for (var limit = end - Vector<float>.Count; start < limit; start += Vector<float>.Count)
                    {
                        lmax = System.Numerics.Vector.Max(lmax, new Vector<float>(leftChannelSpan[start..]));
                        rmax = System.Numerics.Vector.Max(rmax, new Vector<float>(rightChannelSpan[start..]));
                    }

                    var lsmax = lmax[0];
                    var rsmax = rmax[0];
                    for (var vi = 1; vi < Vector<float>.Count; vi++)
                    {
                        lsmax = Math.Max(lmax[vi], lsmax);
                        rsmax = Math.Max(rmax[vi], rsmax);
                    }
                    for (; start < end; start++)
                    {
                        lsmax = Math.Max(lsmax, leftChannelSpan[start]);
                        rsmax = Math.Max(rsmax, rightChannelSpan[start]);
                    }
                    leftLevel[i] = lsmax;
                    rightLevel[i] = rsmax;
                }
            }
            else
            {
                for (var i = 0; i < width; i++)
                {
                    var start = (int)(i * groupSize);
                    var lmax = leftChannelSpan[start];
                    var rmax = rightChannelSpan[start];
                    start++;
                    for (var end = (int)((i + 1) * groupSize); start < end; start++)
                    {
                        lmax = Math.Max(lmax, leftChannelSpan[start]);
                        rmax = Math.Max(rmax, rightChannelSpan[start]);
                    }
                    leftLevel[i] = lmax;
                    rightLevel[i] = rmax;
                }
            }

            ArrayPool<float>.Shared.Return(leftChannel);
            ArrayPool<float>.Shared.Return(rightChannel);
            return (leftLevel, rightLevel, false);
        }

        static Geometry CreateWaveFormGeometry(float[] levels, bool isSampleTime, double width, double height, double range)
        {
            var pixelPerLevel = width / (isSampleTime ? (range / Const.AudioSampleTime) : width);

            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                var isPointAdded = false;
                for (var i = 0; i < levels.Length; i++)
                {
                    var x = i * pixelPerLevel;
                    var db = levels[i] > 0.0F ? Math.Log10(levels[i]) * 20.0 : -MinimumLevel;
                    if (db <= -MinimumLevel)
                    {
                        if (isPointAdded)
                        {
                            geometryContext.LineTo(new Point(x, height), false, false);
                            geometryContext.BeginFigure(new Point(x, height), true, true);
                            isPointAdded = false;
                        }
                        continue;
                    }
                    else
                    {
                        if (!isPointAdded)
                        {
                            geometryContext.BeginFigure(new Point(x - pixelPerLevel, height), true, true);
                        }
                        geometryContext.LineTo(new Point(x, Math.Max(-db / MinimumLevel, 0.0) * height), false, false);
                        isPointAdded = true;
                    }
                }

                if (isPointAdded)
                {
                    geometryContext.LineTo(new Point(width, height), false, false);
                }
            }

            return geometry;
        }
    }
}

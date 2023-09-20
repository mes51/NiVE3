using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ImTools;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class KeyFrameCollectionView : FrameworkElement
    {
        const double KeyFrameIconSize = 10.0;

        static readonly ReadOnlyDictionary<(InterpolationType, InterpolationType), Geometry> KeyFrameIcons;

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(double),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(double),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SourceStartPointProperty = DependencyProperty.Register(
            nameof(SourceStartPoint),
            typeof(double),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty KeyFramesProperty = DependencyProperty.Register(
            nameof(KeyFrames),
            typeof(ObservableCollection<KeyFrame>),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(new ObservableCollection<KeyFrame>(), FrameworkPropertyMetadataOptions.AffectsRender, KeyFramesChanged)
        );

        public ObservableCollection<KeyFrame> KeyFrames
        {
            get { return (ObservableCollection<KeyFrame>)GetValue(KeyFramesProperty); }
            set { SetValue(KeyFramesProperty, value); }
        }

        public double SourceStartPoint
        {
            get { return (double)GetValue(SourceStartPointProperty); }
            set { SetValue(SourceStartPointProperty, value); }
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

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        static KeyFrameCollectionView()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(KeyFrameCollectionView), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

            var keyFrameLinearLeftFigure = new PathFigure();
            keyFrameLinearLeftFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameLinearLeftFigure.Segments.Add(new PolyLineSegment(new Point[] { new Point(0.0, 0.5), new Point(0.5, 1.0) }, false));
            keyFrameLinearLeftFigure.IsClosed = true;
            var keyFrameLinearRightFigure = new PathFigure();
            keyFrameLinearRightFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameLinearRightFigure.Segments.Add(new PolyLineSegment(new Point[] { new Point(1.0, 0.5), new Point(0.5, 1.0) }, false));
            keyFrameLinearRightFigure.IsClosed = true;

            var keyFrameCatmullRomLeftFigure = new PathFigure();
            keyFrameCatmullRomLeftFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameCatmullRomLeftFigure.Segments.Add(new ArcSegment(new Point(0.5, 1.0), new Size(0.5, 0.5), 0.0, false, SweepDirection.Counterclockwise, false));
            keyFrameCatmullRomLeftFigure.IsClosed = true;
            var keyFrameCatmullRomRightFigure = new PathFigure();
            keyFrameCatmullRomRightFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameCatmullRomRightFigure.Segments.Add(new ArcSegment(new Point(0.5, 1.0), new Size(0.5, 0.5), 0.0, false, SweepDirection.Clockwise, false));
            keyFrameCatmullRomRightFigure.IsClosed = true;

            var keyFrameBezierLeftFigure = new PathFigure();
            keyFrameBezierLeftFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameBezierLeftFigure.Segments.Add(new PolyLineSegment(new Point[] { new Point(0.0, 0.0), new Point(0.25, 0.5), new Point(0.0, 1.0), new Point(0.5, 1.0) }, false));
            keyFrameBezierLeftFigure.IsClosed = true;
            var keyFrameBezierRightFigure = new PathFigure();
            keyFrameBezierRightFigure.StartPoint = new Point(0.5, 0.0);
            keyFrameBezierRightFigure.Segments.Add(new PolyLineSegment(new Point[] { new Point(1.0, 0.0), new Point(0.75, 0.5), new Point(1.0, 1.0), new Point(0.5, 1.0) }, false));
            keyFrameBezierRightFigure.IsClosed = true;

            var leftShapes = new Dictionary<InterpolationType, Geometry>
            {
                { InterpolationType.None, new RectangleGeometry(new Rect(0.0, 0.0, 0.5, 1.0)) },
                { InterpolationType.Linear, new PathGeometry(new PathFigure[] { keyFrameLinearLeftFigure }) },
                { InterpolationType.CatmullRom, new PathGeometry(new PathFigure[] { keyFrameCatmullRomLeftFigure }) },
                { InterpolationType.Bezier, new PathGeometry(new PathFigure[] { keyFrameBezierLeftFigure }) }
            };
            var rightShapes = new Dictionary<InterpolationType, Geometry>
            {
                { InterpolationType.None, new RectangleGeometry(new Rect(0.5, 0.0, 0.5, 1.0)) },
                { InterpolationType.Linear, new PathGeometry(new PathFigure[] { keyFrameLinearRightFigure }) },
                { InterpolationType.CatmullRom, new PathGeometry(new PathFigure[] { keyFrameCatmullRomRightFigure }) },
                { InterpolationType.Bezier, new PathGeometry(new PathFigure[] { keyFrameBezierRightFigure }) }
            };

            var interpolationTypes = Enum.GetValues<InterpolationType>();
            var keyFrameGeometries = new Dictionary<(InterpolationType, InterpolationType), Geometry>();
            foreach (var el in interpolationTypes)
            {
                foreach (var er in interpolationTypes)
                {
                    var geometry = new CombinedGeometry(GeometryCombineMode.Union, leftShapes[el], rightShapes[er]);
                    geometry.Transform = new ScaleTransform(KeyFrameIconSize, KeyFrameIconSize);
                    geometry.Freeze();
                    keyFrameGeometries.Add((el, er), geometry);
                }
            }
            KeyFrameIcons = new ReadOnlyDictionary<(InterpolationType, InterpolationType), Geometry>(keyFrameGeometries);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var actualWidth = ActualWidth;
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, actualWidth, ActualHeight));

            var pixelPerTime = (actualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime) || KeyFrames.Count < 1)
            {
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, actualWidth, ActualHeight)));

            drawingContext.PushTransform(new TranslateTransform(UIParameters.TimelineRangeThumbWidth - KeyFrameIconSize * 0.5 - 1.0, (ActualHeight - KeyFrameIconSize) * 0.5));

            var brush = Foreground;
            var timeOffset = SourceStartPoint - RangeStart;
            foreach (var (k, kn) in KeyFrames.Zip(KeyFrames.Skip(1).Append(KeyFrames.Last())))
            {
                var icon = KeyFrameIcons[(k.InterpolationType, kn.InterpolationType)];

                var x = (k.Time + timeOffset) * pixelPerTime;
                if (x > -KeyFrameIconSize && x < actualWidth)
                {
                    drawingContext.PushTransform(new TranslateTransform(x, 0.0));
                    drawingContext.DrawGeometry(brush, null, icon);
                    drawingContext.Pop();
                }
            }

            drawingContext.Pop();
            drawingContext.Pop();
        }

        private void KeyFrames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        static void KeyFramesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not KeyFrameCollectionView collectionView)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<KeyFrame> oldValue)
            {
                oldValue.CollectionChanged -= collectionView.KeyFrames_CollectionChanged;
            }
            if (e.NewValue is ObservableCollection<KeyFrame> newValue)
            {
                newValue.CollectionChanged += collectionView.KeyFrames_CollectionChanged;
            }
        }
    }
}

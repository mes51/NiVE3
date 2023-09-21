using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class KeyFrameCollectionView : FrameworkElement
    {
        const double KeyFrameIconSize = 10.0;

        static readonly ReadOnlyDictionary<(InterpolationType, InterpolationType), Geometry> KeyFrameIcons;

        public static readonly DependencyProperty KeyFrameBrushProperty = DependencyProperty.Register(
            nameof(KeyFrameBrush),
            typeof(Brush),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SelectedKeyFrameBrushProperty = DependencyProperty.Register(
            nameof(SelectedKeyFrameBrush),
            typeof(Brush),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(Brushes.CornflowerBlue, FrameworkPropertyMetadataOptions.AffectsRender)
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

        public static readonly DependencyProperty CompositionFrameRateProperty = DependencyProperty.Register(
            nameof(CompositionFrameRate),
            typeof(double),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty KeyFramesProperty = DependencyProperty.Register(
            nameof(KeyFrames),
            typeof(ObservableCollection<KeyFrame>),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(new ObservableCollection<KeyFrame>(), FrameworkPropertyMetadataOptions.AffectsRender, KeyFramesChanged)
        );

        public static readonly DependencyProperty SelectedKeyFramesProperty = DependencyProperty.Register(
            nameof(SelectedKeyFrames),
            typeof(ObservableCollection<KeyFrame>),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(new ObservableCollection<KeyFrame>(), FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static RoutedEvent KeyFrameMoveRequestEvent = EventManager.RegisterRoutedEvent(
            nameof(KeyFrameMoveRequest), RoutingStrategy.Direct, typeof(EventHandler<KeyFrameMoveEventArgs>), typeof(KeyFrameCollectionView)
        );

        public ObservableCollection<KeyFrame> SelectedKeyFrames
        {
            get { return (ObservableCollection<KeyFrame>)GetValue(SelectedKeyFramesProperty); }
            set { SetValue(SelectedKeyFramesProperty, value); }
        }

        public ObservableCollection<KeyFrame> KeyFrames
        {
            get { return (ObservableCollection<KeyFrame>)GetValue(KeyFramesProperty); }
            set { SetValue(KeyFramesProperty, value); }
        }

        public double CompositionFrameRate
        {
            get { return (double)GetValue(CompositionFrameRateProperty); }
            set { SetValue(CompositionFrameRateProperty, value); }
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

        public Brush SelectedKeyFrameBrush
        {
            get { return (Brush)GetValue(SelectedKeyFrameBrushProperty); }
            set { SetValue(SelectedKeyFrameBrushProperty, value); }
        }

        public Brush KeyFrameBrush
        {
            get { return (Brush)GetValue(KeyFrameBrushProperty); }
            set { SetValue(KeyFrameBrushProperty, value); }
        }

        KeyFrame? LastSelected { get; set; }

        bool IsClicked { get; set; }

        double ClickX { get; set; }

        double KeyFrameMoveingTime { get; set; }

        public event EventHandler<KeyFrameMoveEventArgs> KeyFrameMoveRequest
        {
            add { AddHandler(KeyFrameMoveRequestEvent, value); }
            remove { RemoveHandler(KeyFrameMoveRequestEvent, value); }
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

        public KeyFrameCollectionView()
        {
            Unloaded += KeyFrameCollectionView_Unloaded;
            MouseDown += KeyFrameCollectionView_MouseDown;
            MouseMove += KeyFrameCollectionView_MouseMove;
            MouseUp += KeyFrameCollectionView_MouseUp;
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

            var brush = KeyFrameBrush;
            var selectedBrush = SelectedKeyFrameBrush;
            var timeOffset = SourceStartPoint - RangeStart;
            var selected = SelectedKeyFrames;
            var frameDuration = 1.0 / CompositionFrameRate;
            foreach (var (k, kn) in KeyFrames.Zip(KeyFrames.Skip(1).Append(KeyFrames.Last())))
            {
                var icon = KeyFrameIcons[(k.InterpolationType, kn.InterpolationType)];

                var isSelected = selected.Contains(k);
                var keyFrameTime = isSelected && IsClicked ? (int)Math.Round((KeyFrameMoveingTime + k.Time) * CompositionFrameRate) * frameDuration : k.Time;
                var x = (timeOffset + keyFrameTime) * pixelPerTime;
                if (x > -KeyFrameIconSize && x < actualWidth)
                {
                    drawingContext.PushTransform(new TranslateTransform(x, 0.0));
                    drawingContext.DrawGeometry(isSelected ? selectedBrush : brush, null, icon);
                    drawingContext.Pop();
                }
            }

            drawingContext.Pop();
            drawingContext.Pop();
        }

        void SelectKeyFrame(KeyFrame keyFrame, bool selectRange, bool selectMultiple)
        {
            if (KeyFrames.Count < 1)
            {
                return;
            }

            var keyFrames = KeyFrames.ToArray();
            if (selectMultiple)
            {
                if (SelectedKeyFrames.Contains(keyFrame))
                {
                    SelectedKeyFrames.Remove(keyFrame);
                }
                else
                {
                    SelectedKeyFrames.Add(keyFrame);
                }
                LastSelected = keyFrame;
            }
            else if (selectRange && SelectedKeyFrames.Count > 0)
            {
                var oldSelectedKeyFrames = SelectedKeyFrames.ToArray();
                if (LastSelected == null)
                {
                    LastSelected = keyFrames[0];
                }
                var startIndex = Array.IndexOf(keyFrames, LastSelected);
                var endIndex = Array.IndexOf(keyFrames, keyFrame);
                if (startIndex == endIndex)
                {
                    foreach (var k in oldSelectedKeyFrames)
                    {
                        if (k != keyFrame)
                        {
                            SelectedKeyFrames.Remove(k);
                        }
                    }
                    if (!SelectedKeyFrames.Contains(keyFrame))
                    {
                        SelectedKeyFrames.Add(keyFrame);
                    }
                    return;
                }
                else if (startIndex > endIndex)
                {
                    var temp = endIndex;
                    endIndex = startIndex;
                    startIndex = temp;
                }

                var targets = keyFrames.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
                foreach (var k in oldSelectedKeyFrames.Except(targets))
                {
                    SelectedKeyFrames.Remove(k);
                }
                foreach (var k in targets.Except(oldSelectedKeyFrames))
                {
                    SelectedKeyFrames.Add(k);
                }
            }
            else if (!SelectedKeyFrames.Contains(keyFrame))
            {
                SelectedKeyFrames.Clear();
                SelectedKeyFrames.Add(keyFrame);
                LastSelected = keyFrame;
            }
        }

        private void KeyFrameCollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (KeyFrames != null)
            {
                KeyFrames.CollectionChanged -= KeyFrames_CollectionChanged;
            }
        }

        private void KeyFrameCollectionView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClicked)
            {
                var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
                var frameDuration = 1.0 / CompositionFrameRate;
                var posX = e.GetPosition(this).X;
                var diffTime = (posX - ClickX) * timePerPixel;

                IsClicked = false;
                ReleaseMouseCapture();

                var oldSelectedKeyFrame = SelectedKeyFrames.ToArray();
                var newTimes = oldSelectedKeyFrame.Select(k => (int)Math.Round((diffTime + k.Time) * CompositionFrameRate) * frameDuration).ToArray();

                if (oldSelectedKeyFrame.Select((k, i) => k.Time == newTimes[i]).All(b => b))
                {
                    return;
                }

                var eventArgs = new KeyFrameMoveEventArgs(oldSelectedKeyFrame, newTimes, KeyFrameMoveRequestEvent, this);
                RaiseEvent(eventArgs);

                SelectedKeyFrames.Clear();
                foreach (var k in KeyFrames.Where(k => oldSelectedKeyFrame.Any(ok => k.Id == ok.Id)))
                {
                    SelectedKeyFrames.Add(k);
                }

                InvalidateVisual();
            }
        }

        private void KeyFrameCollectionView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsClicked)
            {
                return;
            }

            var timePerPixel = Range / (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth);
            var posX = e.GetPosition(this).X;
            KeyFrameMoveingTime = (posX - ClickX) * timePerPixel;

            InvalidateVisual();
        }

        private void KeyFrameCollectionView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime) || KeyFrames.Count < 1)
            {
                return;
            }

            var isSelectRange = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            var isSelectMultiple = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            var pos = e.GetPosition(this);
            var hitHeight = (ActualHeight - KeyFrameIconSize) * 0.5;
            if (pos.Y < hitHeight || (pos.Y - hitHeight) > KeyFrameIconSize)
            {
                if (!isSelectRange && !isSelectMultiple)
                {
                    SelectedKeyFrames.Clear();
                    InvalidateVisual();
                }
                return;
            }

            var timeOffset = SourceStartPoint - RangeStart;
            var x = pos.X - UIParameters.TimelineRangeThumbWidth;
            var clickedKeyFrame = KeyFrames.LastOrDefault(k => Math.Abs((k.Time + timeOffset) * pixelPerTime - x) < KeyFrameIconSize * 0.5);
            if (clickedKeyFrame != null)
            {
                SelectKeyFrame(clickedKeyFrame, isSelectRange, isSelectMultiple);
                if (SelectedKeyFrames.Contains(clickedKeyFrame))
                {
                    IsClicked = true;
                    ClickX = pos.X;
                    KeyFrameMoveingTime = 0.0;
                    CaptureMouse();
                }
            }
            else if (!isSelectRange && !isSelectMultiple)
            {
                SelectedKeyFrames.Clear();
            }
            InvalidateVisual();
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

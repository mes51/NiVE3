using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;
using NiVE3.Util;
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

        public static readonly DependencyProperty SelectedKeyFrameIdsProperty = DependencyProperty.Register(
            nameof(SelectedKeyFrameIds),
            typeof(ObservableCollection<int>),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(new ObservableCollection<int>(), FrameworkPropertyMetadataOptions.AffectsRender, SelectedKeyFrameIdsChanged)
        );

        public static readonly DependencyProperty SupportedInterpolationTypesProperty = DependencyProperty.Register(
            nameof(SupportedInterpolationTypes),
            typeof(InterpolationType),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(InterpolationType.None | InterpolationType.Linear | InterpolationType.CatmullRom | InterpolationType.Bezier, SupportedInterpolationTypesChanged)
        );

        public static readonly DependencyProperty SelectItemCommandProperty = DependencyProperty.Register(
            nameof(SelectItemCommand),
            typeof(ICommand),
            typeof(KeyFrameCollectionView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static RoutedEvent KeyFrameMoveRequestEvent = EventManager.RegisterRoutedEvent(
            nameof(KeyFrameMoveRequest), RoutingStrategy.Direct, typeof(EventHandler<KeyFrameMoveEventArgs>), typeof(KeyFrameCollectionView)
        );

        public static RoutedEvent KeyFrameInterpolationTypeChangeRequestEvent = EventManager.RegisterRoutedEvent(
            nameof(KeyFrameInterpolationTypeChangeRequest), RoutingStrategy.Direct, typeof(EventHandler<ChangeKeyFrameInterpolationTypeEventArgs>), typeof(KeyFrameCollectionView)
        );

        public ICommand? SelectItemCommand
        {
            get { return (ICommand)GetValue(SelectItemCommandProperty); }
            set { SetValue(SelectItemCommandProperty, value); }
        }

        public InterpolationType SupportedInterpolationTypes
        {
            get { return (InterpolationType)GetValue(SupportedInterpolationTypesProperty); }
            set { SetValue(SupportedInterpolationTypesProperty, value); }
        }

        public ObservableCollection<int> SelectedKeyFrameIds
        {
            get { return (ObservableCollection<int>)GetValue(SelectedKeyFrameIdsProperty); }
            set { SetValue(SelectedKeyFrameIdsProperty, value); }
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

        InterpolationType[]? SupportedInterpolationTypeList { get; set; } = Enum.GetValues<InterpolationType>();

        public event EventHandler<ChangeKeyFrameInterpolationTypeEventArgs> KeyFrameInterpolationTypeChangeRequest
        {
            add { AddHandler(KeyFrameInterpolationTypeChangeRequestEvent, value); }
            remove { RemoveHandler(KeyFrameInterpolationTypeChangeRequestEvent, value); }
        }

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

        public void SelectAllKeyFrames()
        {
            SelectedKeyFrameIds.Clear();
            foreach (var k in KeyFrames)
            {
                SelectedKeyFrameIds.Add(k.Id);
            }
            InvalidateVisual();
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
            var selected = SelectedKeyFrameIds;
            var frameDuration = 1.0 / CompositionFrameRate;
            foreach (var (k, kp) in KeyFrames.Zip(KeyFrames.Prepend(KeyFrames.First())))
            {
                var icon = KeyFrameIcons[(kp.InterpolationType, k.InterpolationType)];

                var isSelected = selected.Contains(k.Id);
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

            foreach (var (k, i) in SelectedKeyFrameIds.ZipWithIndex().ToArray())
            {
                if (KeyFrames.All(nk => k != nk.Id))
                {
                    SelectedKeyFrameIds.Remove(k);
                }
            }
            if (LastSelected != null && !KeyFrames.Contains(LastSelected))
            {
                LastSelected = null;
            }

            var keyFrames = KeyFrames.ToArray();
            if (selectMultiple)
            {
                if (SelectedKeyFrameIds.Contains(keyFrame.Id))
                {
                    SelectedKeyFrameIds.Remove(keyFrame.Id);
                }
                else
                {
                    SelectedKeyFrameIds.Add(keyFrame.Id);
                }
                LastSelected = keyFrame;
            }
            else if (selectRange && SelectedKeyFrameIds.Count > 0)
            {
                var oldSelectedKeyFrames = SelectedKeyFrameIds.ToArray();
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
                        if (k != keyFrame.Id)
                        {
                            SelectedKeyFrameIds.Remove(k);
                        }
                    }
                    if (!SelectedKeyFrameIds.Contains(keyFrame.Id))
                    {
                        SelectedKeyFrameIds.Add(keyFrame.Id);
                    }
                    return;
                }
                else if (startIndex > endIndex)
                {
                    var temp = endIndex;
                    endIndex = startIndex;
                    startIndex = temp;
                }

                var targets = keyFrames.Skip(startIndex).Take(endIndex - startIndex + 1).Select(k => k.Id).ToArray();
                foreach (var k in oldSelectedKeyFrames.Except(targets))
                {
                    SelectedKeyFrameIds.Remove(k);
                }
                foreach (var k in targets.Except(oldSelectedKeyFrames))
                {
                    SelectedKeyFrameIds.Add(k);
                }
            }
            else if (!SelectedKeyFrameIds.Contains(keyFrame.Id))
            {
                SelectedKeyFrameIds.Clear();
                SelectedKeyFrameIds.Add(keyFrame.Id);
                LastSelected = keyFrame;
            }
        }

        private void KeyFrameCollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (KeyFrames != null)
            {
                CollectionChangedEventManager.RemoveHandler(KeyFrames, KeyFrameCollection_CollectionChanged);
            }
            if (SelectedKeyFrameIds != null)
            {
                CollectionChangedEventManager.RemoveHandler(SelectedKeyFrameIds, KeyFrameCollection_CollectionChanged);
            }
        }

        private void KeyFrameCollectionView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClicked)
            {
                var frameDuration = 1.0 / CompositionFrameRate;
                var diffTime = TimeCalc.CalcTimeFromPixel(e.GetPosition(this).X - ClickX, ActualWidth, Range, 0.0);

                IsClicked = false;
                ReleaseMouseCapture();

                var oldSelectedKeyFrame = SelectedKeyFrameIds.Where(id => KeyFrames.Any(k => k.Id == id)).Select(id => KeyFrames.First(k => k.Id == id)).ToArray();
                var newTimes = oldSelectedKeyFrame.Select(k => (int)Math.Round((diffTime + k.Time) * CompositionFrameRate) * frameDuration).ToArray();

                if (oldSelectedKeyFrame.Select((k, i) => k.Time == newTimes[i]).All(b => b))
                {
                    return;
                }

                var eventArgs = new KeyFrameMoveEventArgs(oldSelectedKeyFrame, newTimes, KeyFrameMoveRequestEvent, this);
                RaiseEvent(eventArgs);

                SelectedKeyFrameIds.Clear();
                foreach (var k in oldSelectedKeyFrame)
                {
                    SelectedKeyFrameIds.Add(k.Id);
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

            KeyFrameMoveingTime = TimeCalc.CalcTimeFromPixel(e.GetPosition(this).X - ClickX, ActualWidth, Range, 0.0);

            InvalidateVisual();
        }

        private void KeyFrameCollectionView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

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
                    SelectedKeyFrameIds.Clear();
                    InvalidateVisual();
                    SelectItemCommand?.Execute(null);
                }
                return;
            }

            var timeOffset = SourceStartPoint - RangeStart;
            var x = pos.X - UIParameters.TimelineRangeThumbWidth;
            var clickedKeyFrame = KeyFrames.LastOrDefault(k => Math.Abs((k.Time + timeOffset) * pixelPerTime - x) < KeyFrameIconSize * 0.5);
            if (clickedKeyFrame != null)
            {
                if (e.ClickCount == 2)
                {
                    if (SupportedInterpolationTypeList != null)
                    {
                        var nextInterpolationType = SupportedInterpolationTypeList[(Array.IndexOf(SupportedInterpolationTypeList, clickedKeyFrame.InterpolationType) + 1) % SupportedInterpolationTypeList.Length];
                        var selectedKeyFrameIds = SelectedKeyFrameIds;
                        var targetKeyFrames = KeyFrames.Where(k => selectedKeyFrameIds.Contains(k.Id)).ToArray();
                        RaiseEvent(new ChangeKeyFrameInterpolationTypeEventArgs(targetKeyFrames, nextInterpolationType, KeyFrameInterpolationTypeChangeRequestEvent, this));
                    }
                }
                else
                {
                    SelectKeyFrame(clickedKeyFrame, isSelectRange, isSelectMultiple);
                    SelectItemCommand?.Execute(null);
                    if (SelectedKeyFrameIds.Contains(clickedKeyFrame.Id))
                    {
                        IsClicked = true;
                        ClickX = pos.X;
                        KeyFrameMoveingTime = 0.0;
                        CaptureMouse();
                    }
                }
            }
            else if (!isSelectRange && !isSelectMultiple)
            {
                SelectedKeyFrameIds.Clear();
                SelectItemCommand?.Execute(null);
            }
            InvalidateVisual();
        }

        private void KeyFrameCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                CollectionChangedEventManager.RemoveHandler(oldValue, collectionView.KeyFrameCollection_CollectionChanged);
            }
            if (e.NewValue is ObservableCollection<KeyFrame> newValue)
            {
                CollectionChangedEventManager.AddHandler(newValue, collectionView.KeyFrameCollection_CollectionChanged);
            }
        }

        static void SelectedKeyFrameIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not KeyFrameCollectionView collectionView)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<int> oldValue)
            {
                CollectionChangedEventManager.RemoveHandler(oldValue, collectionView.KeyFrameCollection_CollectionChanged);
            }
            if (e.NewValue is ObservableCollection<int> newValue)
            {
                CollectionChangedEventManager.AddHandler(newValue, collectionView.KeyFrameCollection_CollectionChanged);
            }
        }

        static void SupportedInterpolationTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyFrameCollectionView collection)
            {
                collection.SupportedInterpolationTypeList = Enum.GetValues<InterpolationType>().Where(i => collection.SupportedInterpolationTypes.HasFlag(i)).Order().ToArray();
            }
        }
    }
}

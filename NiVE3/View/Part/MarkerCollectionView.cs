using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using NiVE3.Plugin.ValueObject;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.ValueObject;
using NiVE3.View.Resource;

namespace NiVE3.View.Part
{
    class MarkerCollectionView : FrameworkElement
    {
        const double MarkerWidth = 10.0;

        const double MarkerHeight = UIParameters.TimeLocatorTimeBarHeight * 0.5;

        static readonly Geometry MarkerIcon;

        public static readonly DependencyProperty MarkerBrushProperty = DependencyProperty.Register(
            nameof(MarkerBrush),
            typeof(Brush),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range),
            typeof(Time),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
            nameof(RangeStart),
            typeof(Time),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(Time.Zero, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate),
            typeof(double),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CompositionMarkersProperty = DependencyProperty.Register(
            nameof(CompositionMarkers),
            typeof(ObservableCollection<Marker>),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(new ObservableCollection<Marker>(), FrameworkPropertyMetadataOptions.AffectsRender, CompositionMarkersChanged)
        );

        public static readonly DependencyProperty AddMarkerCommandProperty = DependencyProperty.Register(
            nameof(AddMarkerCommand),
            typeof(ICommand),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty DeleteMarkerCommandProperty = DependencyProperty.Register(
            nameof(DeleteMarkerCommand),
            typeof(ICommand),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty EditMarkerNameCommandProperty = DependencyProperty.Register(
            nameof(EditMarkerNameCommand),
            typeof(ICommand),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(null)
        );

        private static readonly DependencyPropertyKey ToolTipIsVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ToolTipIsVisible),
            typeof(bool),
            typeof(MarkerCollectionView),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty ToolTipIsVisibleProperty = ToolTipIsVisiblePropertyKey.DependencyProperty;

        public static readonly RoutedEvent MarkerMoveRequestEvent = EventManager.RegisterRoutedEvent(
            nameof(MarkerMoveRequest), RoutingStrategy.Direct, typeof(EventHandler<MarkerMoveEventArgs>), typeof(MarkerCollectionView)
        );

        public bool ToolTipIsVisible
        {
            get { return (bool)GetValue(ToolTipIsVisibleProperty); }
            private set { SetValue(ToolTipIsVisiblePropertyKey, value); }
        }

        public event EventHandler<MarkerMoveEventArgs> MarkerMoveRequest
        {
            add { AddHandler(MarkerMoveRequestEvent, value); }
            remove { RemoveHandler(MarkerMoveRequestEvent, value); }
        }

        public ICommand? EditMarkerNameCommand
        {
            get { return (ICommand)GetValue(EditMarkerNameCommandProperty); }
            set { SetValue(EditMarkerNameCommandProperty, value); }
        }

        public ICommand? DeleteMarkerCommand
        {
            get { return (ICommand)GetValue(DeleteMarkerCommandProperty); }
            set { SetValue(DeleteMarkerCommandProperty, value); }
        }

        public ICommand? AddMarkerCommand
        {
            get { return (ICommand)GetValue(AddMarkerCommandProperty); }
            set { SetValue(AddMarkerCommandProperty, value); }
        }

        public ObservableCollection<Marker> CompositionMarkers
        {
            get { return (ObservableCollection<Marker>)GetValue(CompositionMarkersProperty); }
            set { SetValue(CompositionMarkersProperty, value); }
        }

        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        public Time RangeStart
        {
            get { return (Time)GetValue(RangeStartProperty); }
            set { SetValue(RangeStartProperty, value); }
        }

        public Time Range
        {
            get { return (Time)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        public Brush MarkerBrush
        {
            get { return (Brush)GetValue(MarkerBrushProperty); }
            set { SetValue(MarkerBrushProperty, value); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand AddMarkerCommandWrapper { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand DeleteMarkerCommandWrapper { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand EditMarkerNameCommandWrapper { get; }

        bool IsClicked { get; set; }

        double ClickX { get; set; }

        Marker? MoveTarget { get; set; }

        Marker? RightClickedMarker { get; set; }

        Time RightClickedTime { get; set; }

        Time MarkerMovingTime { get; set; }

        static MarkerCollectionView()
        {
            var markerIconGeometry = new StreamGeometry();

            using (var context = markerIconGeometry.Open())
            {
                context.BeginFigure(new Point(MarkerWidth * 0.5, 0.0), true, true);
                context.LineTo(new Point(0.0, MarkerHeight * 0.5), true, false);
                context.LineTo(new Point(0.0, MarkerHeight), true, false);
                context.LineTo(new Point(MarkerWidth, MarkerHeight), true, false);
                context.LineTo(new Point(MarkerWidth, MarkerHeight * 0.5), true, false);
                context.LineTo(new Point(MarkerWidth * 0.5, 0.0), true, false);
            }

            MarkerIcon = markerIconGeometry;
        }

        public MarkerCollectionView()
        {
            AddMarkerCommandWrapper = new RequerySuggestedCommand(() =>
            {
                AddMarkerCommand?.Execute(RightClickedTime);
            }, () => AddMarkerCommand?.CanExecute(RightClickedTime) ?? false);

            DeleteMarkerCommandWrapper = new RequerySuggestedCommand(() =>
            {
                if (RightClickedMarker != null)
                {
                    DeleteMarkerCommand?.Execute(RightClickedMarker);
                }
            }, () => RightClickedMarker != null && (DeleteMarkerCommand?.CanExecute(RightClickedMarker) ?? false));

            EditMarkerNameCommandWrapper = new RequerySuggestedCommand(() =>
            {
                if (RightClickedMarker != null)
                {
                    EditMarkerNameCommand?.Execute(RightClickedMarker);
                }
            }, () => RightClickedMarker != null && (EditMarkerNameCommand?.CanExecute(RightClickedMarker) ?? false));

            MouseDown += CompositionMarkerView_MouseDown;
            MouseMove += CompositionMarkerView_MouseMove;
            MouseUp += CompositionMarkerView_MouseUp;
            ContextMenuOpening += CompositionMarkerView_ContextMenuOpening;
        }

        protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            else if (hitTestParameters.HitPoint.Y < ActualHeight - MarkerHeight)
            {
                return null;
            }

            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime) || CompositionMarkers.Count < 1)
            {
                return null;
            }

            var rangeStart = RangeStart;
            var diffTime = MoveTarget != null ? (MarkerMovingTime + MoveTarget.Time).RoundToFrameRate(FrameRate) - MoveTarget.Time : Time.Zero;
            var posX = hitTestParameters.HitPoint.X - UIParameters.TimelineRangeThumbWidth;
            foreach (var m in CompositionMarkers)
            {
                var isMoving = MoveTarget == m;
                var markerTime = m.Time + (isMoving ? diffTime : Time.Zero);

                if (Math.Abs(posX - (double)(markerTime - rangeStart) * pixelPerTime) < MarkerWidth * 0.5)
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var actualWidth = ActualWidth;
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, actualWidth, ActualHeight));

            var pixelPerTime = (actualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime) || CompositionMarkers.Count < 1)
            {
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, actualWidth, ActualHeight)));

            drawingContext.PushTransform(new TranslateTransform(UIParameters.TimelineRangeThumbWidth - MarkerWidth * 0.5 - 1.0, ActualHeight - MarkerHeight));

            var brush = MarkerBrush;
            var rangeStart = RangeStart;
            var diffTime = MoveTarget != null ? (MarkerMovingTime + MoveTarget.Time).RoundToFrameRate(FrameRate) - MoveTarget.Time : Time.Zero;
            foreach (var (m, mp) in CompositionMarkers.Zip(CompositionMarkers.Prepend(CompositionMarkers.First())))
            {
                var isMoving = MoveTarget == m;
                var markerTime = m.Time + (isMoving ? diffTime : Time.Zero);
                var x = (double)(markerTime - rangeStart) * pixelPerTime + 1.0;
                if (x > -MarkerWidth && x < actualWidth)
                {
                    drawingContext.PushTransform(new TranslateTransform(x, 0.0));
                    drawingContext.DrawGeometry(brush, null, MarkerIcon);
                    drawingContext.Pop();
                }
            }
            drawingContext.Pop();
            drawingContext.Pop();
        }

        private void CompositionMarkerView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (FrameRate < 0.0 && Range < Time.Zero)
            {
                return;
            }

            RightClickedTime = TimeCalc.CalcTimeFromPixelAligned(e.CursorLeft - UIParameters.TimelineRangeThumbTotalWidth, ActualWidth, Range, RangeStart, FrameRate);
            RightClickedMarker = GetMarkerByPosition(e.CursorLeft);
        }

        private void CompositionMarkerView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsClicked || MoveTarget == null)
            {
                return;
            }

            IsClicked = false;
            ToolTipIsVisible = true;
            ReleaseMouseCapture();

            var diffTime = (MarkerMovingTime + MoveTarget.Time).RoundToFrameRate(FrameRate) - MoveTarget.Time;
            var newTime = MoveTarget.Time + diffTime;
            if (newTime != MoveTarget.Time)
            {
                var eventArgs = new MarkerMoveEventArgs(MoveTarget, newTime, MarkerMoveRequestEvent, this);
                RaiseEvent(eventArgs);
            }

            MoveTarget = null;
            InvalidateVisual();
        }

        private void CompositionMarkerView_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                MarkerMovingTime = TimeCalc.CalcTimeFromPixel(e.GetPosition(this).X - ClickX, ActualWidth, Range, RangeStart);
                InvalidateVisual();
            }
            else
            {
                var toolTip = ToolTip as ToolTip;
                if (toolTip == null)
                {
                    return;
                }

                var pos = e.GetPosition(this);
                var targetMarker = GetMarkerByPosition(pos.X);
                if (targetMarker == null || string.IsNullOrEmpty(targetMarker.Name))
                {
                    toolTip.Visibility = Visibility.Collapsed;
                }
                else
                {
                    toolTip.Visibility = Visibility.Visible;
                    toolTip.Content = targetMarker.Name;
                    toolTip.HorizontalOffset = pos.X + 16.0;
                    toolTip.VerticalOffset = pos.Y + 16.0;
                }
            }
        }

        private void CompositionMarkerView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            var pos = e.GetPosition(this);
            var clickedMarker = GetMarkerByPosition(pos.X);
            if (clickedMarker != null)
            {
                if (e.ClickCount == 2)
                {
                    EditMarkerNameCommand?.Execute(clickedMarker);
                }
                else
                {
                    MoveTarget = clickedMarker;
                    ClickX = pos.X;
                    IsClicked = true;
                    ToolTipIsVisible = false;
                    MarkerMovingTime = Time.Zero;
                    CaptureMouse();
                }
                e.Handled = true;
            }
        }

        Marker? GetMarkerByPosition(double posX)
        {
            var pixelPerTime = (ActualWidth - UIParameters.TimelineRangeThumbTotalWidth) / (double)Range;
            if (pixelPerTime < 0 || double.IsNaN(pixelPerTime) || double.IsInfinity(pixelPerTime) || CompositionMarkers.Count < 1)
            {
                return null;
            }

            var x = posX - UIParameters.TimelineRangeThumbWidth;
            var rangeStart = RangeStart;
            return CompositionMarkers.LastOrDefault(m => Math.Abs((double)(m.Time - rangeStart) * pixelPerTime - x) < MarkerWidth * 0.5);
        }

        private void CompositionMarkers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private static void CompositionMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MarkerCollectionView view)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<Marker> oldValue)
            {
                oldValue.CollectionChanged -= view.CompositionMarkers_CollectionChanged;
            }
            if (e.NewValue is ObservableCollection<Marker> newValue)
            {
                newValue.CollectionChanged += view.CompositionMarkers_CollectionChanged;
            }
        }
    }
}

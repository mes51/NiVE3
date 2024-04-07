using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Shared.Extension;

namespace NiVE3.Plugin.Internal.View
{
    class GradientMarkerView : FrameworkElement
    {
        const double MarkerRectSize = 10.0;

        const double MarkerHalfSize = MarkerRectSize * 0.5;

        const double BorderThickness = 1.0;

        static readonly Geometry MarkerTopHeadGeometry;

        static readonly Geometry MarkerBottomHeadGeometry;

        static readonly Geometry MarkerTopBodyGeometry;

        static readonly Geometry MarkerBottomBodyGeometry;

        public static readonly DependencyProperty GradientMarkersProperty = DependencyProperty.Register(
            nameof(GradientMarkers),
            typeof(ObservableCollection<GradientMarker>),
            typeof(GradientMarkerView),
            new FrameworkPropertyMetadata(new ObservableCollection<GradientMarker>(), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, GradientMarkersChanged)
        );

        public static readonly DependencyProperty IsArrowTopProperty = DependencyProperty.Register(
            nameof(IsArrowTop),
            typeof(bool),
            typeof(GradientMarkerView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
            nameof(BorderBrush),
            typeof(Brush),
            typeof(GradientMarkerView),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender, BorderBrushChanged)
        );

        public static readonly DependencyProperty SelectedBorderBrushProperty = DependencyProperty.Register(
            nameof(SelectedBorderBrush),
            typeof(Brush),
            typeof(GradientBrush),
            new FrameworkPropertyMetadata(Brushes.CornflowerBlue, FrameworkPropertyMetadataOptions.AffectsRender, BorderBrushChanged)
        );

        public static readonly DependencyProperty SelectedMarkerProperty = DependencyProperty.Register(
            nameof(SelectedMarker),
            typeof(GradientMarker),
            typeof(GradientMarkerView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public GradientMarker? SelectedMarker
        {
            get { return (GradientMarker)GetValue(SelectedMarkerProperty); }
            set { SetValue(SelectedMarkerProperty, value); }
        }

        public Brush SelectedBorderBrush
        {
            get { return (Brush)GetValue(SelectedBorderBrushProperty); }
            set { SetValue(SelectedBorderBrushProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public bool IsArrowTop
        {
            get { return (bool)GetValue(IsArrowTopProperty); }
            set { SetValue(IsArrowTopProperty, value); }
        }

        public ObservableCollection<GradientMarker> GradientMarkers
        {
            get { return (ObservableCollection<GradientMarker>)GetValue(GradientMarkersProperty); }
            set { SetValue(GradientMarkersProperty, value); }
        }

        Pen BorderPen { get; set; } = new Pen(Brushes.Gray, BorderThickness);

        Pen SelectedBorderPen { get; set; } = new Pen(Brushes.CornflowerBlue, BorderThickness);

        bool IsClicked { get; set; }

        static GradientMarkerView()
        {
            HeightProperty.OverrideMetadata(typeof(GradientMarkerView), new FrameworkPropertyMetadata(MarkerRectSize + MarkerHalfSize, FrameworkPropertyMetadataOptions.AffectsRender));
            ClipToBoundsProperty.OverrideMetadata(typeof(GradientMarkerView), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(GradientMarkerView), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

            var topHead = new StreamGeometry();
            var bottomHead = new StreamGeometry();
            var topBody = new StreamGeometry();
            var bottomBody = new StreamGeometry();

            using (var context = topHead.Open())
            {
                context.BeginFigure(new Point(MarkerHalfSize, 0.0), true, true);
                context.LineTo(new Point(0.0, MarkerHalfSize), false, false);
                context.LineTo(new Point(MarkerHalfSize * 2.0, MarkerHalfSize), false, false);
            }
            using (var context = bottomHead.Open())
            {
                context.BeginFigure(new Point(MarkerHalfSize, MarkerRectSize + MarkerHalfSize), true, true);
                context.LineTo(new Point(0.0, MarkerRectSize), false, false);
                context.LineTo(new Point(MarkerHalfSize * 2.0, MarkerRectSize), false, false);
            }
            using (var context = topBody.Open())
            {
                context.BeginFigure(new Point(0.0, MarkerHalfSize), true, true);
                context.LineTo(new Point(0.0, MarkerHalfSize + MarkerRectSize), true, false);
                context.LineTo(new Point(MarkerRectSize, MarkerHalfSize + MarkerRectSize), true, false);
                context.LineTo(new Point(MarkerRectSize, MarkerHalfSize), true, false);
                context.LineTo(new Point(0.0, MarkerHalfSize), false, false);
            }
            using (var context = bottomBody.Open())
            {
                context.BeginFigure(new Point(0.0, MarkerRectSize), true, true);
                context.LineTo(new Point(), true, false);
                context.LineTo(new Point(MarkerRectSize, 0.0), true, false);
                context.LineTo(new Point(MarkerRectSize, MarkerRectSize), true, false);
                context.LineTo(new Point(0.0, MarkerRectSize), false, false);
            }

            topHead.Freeze();
            bottomHead.Freeze();
            topBody.Freeze();
            bottomBody.Freeze();
            MarkerTopHeadGeometry = topHead;
            MarkerBottomHeadGeometry = bottomHead;
            MarkerTopBodyGeometry = topBody;
            MarkerBottomBodyGeometry = bottomBody;
        }

        public GradientMarkerView()
        {
            Unloaded += GradientMarkerView_Unloaded;
            MouseDown += GradientMarkerView_MouseDown;
            MouseMove += GradientMarkerView_MouseMove;
            MouseUp += GradientMarkerView_MouseUp;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));

            if (ActualWidth <= MarkerHalfSize * 2.0)
            {
                return;
            }

            var range = ActualWidth - MarkerHalfSize * 2.0;
            var selected = GradientMarkers.FirstOrDefault(m => m == SelectedMarker);
            if (IsArrowTop)
            {
                foreach (var gradient in GradientMarkers)
                {
                    if (selected == gradient)
                    {
                        continue;
                    }

                    var pos = gradient.Position * range;
                    drawingContext.PushTransform(new TranslateTransform(pos, 0.0));

                    drawingContext.DrawGeometry(BorderBrush, BorderPen, MarkerTopHeadGeometry);
                    drawingContext.DrawGeometry(new SolidColorBrush(gradient.Color), BorderPen, MarkerTopBodyGeometry);

                    drawingContext.Pop();
                }
                if (selected != null)
                {
                    var pos = selected.Position * range;
                    drawingContext.PushTransform(new TranslateTransform(pos, 0.0));

                    drawingContext.DrawGeometry(SelectedBorderBrush, SelectedBorderPen, MarkerTopHeadGeometry);
                    drawingContext.DrawGeometry(new SolidColorBrush(selected.Color), SelectedBorderPen, MarkerTopBodyGeometry);

                    drawingContext.Pop();
                }
            }
            else
            {
                foreach (var gradient in GradientMarkers)
                {
                    if (selected == gradient)
                    {
                        continue;
                    }

                    var pos = gradient.Position * range;
                    drawingContext.PushTransform(new TranslateTransform(pos, 0.0));

                    drawingContext.DrawGeometry(BorderBrush, BorderPen, MarkerBottomHeadGeometry);
                    drawingContext.DrawGeometry(new SolidColorBrush(gradient.Color), BorderPen, MarkerBottomBodyGeometry);

                    drawingContext.Pop();
                }
                if (selected != null)
                {
                    var pos = selected.Position * range;
                    drawingContext.PushTransform(new TranslateTransform(pos, 0.0));

                    drawingContext.DrawGeometry(SelectedBorderBrush, SelectedBorderPen, MarkerBottomHeadGeometry);
                    drawingContext.DrawGeometry(new SolidColorBrush(selected.Color), SelectedBorderPen, MarkerBottomBodyGeometry);

                    drawingContext.Pop();
                }
            }
        }

        private void GradientMarkerView_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (var maker in GradientMarkers)
            {
                maker.PropertyChanged -= Maker_PropertyChanged;
            }
        }

        private void GradientMarkerView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouseX = e.GetPosition(this).X - MarkerHalfSize;
            var range = ActualWidth - MarkerHalfSize * 2.0;
            var selected = GradientMarkers.LastOrDefault(m => Math.Abs(m.Position * range - mouseX) <= MarkerHalfSize);
            var pos = (float)Math.Round(Math.Clamp(mouseX / range, 0.0, 1.0), 4);

            if (selected != null)
            {
                SelectedMarker = selected;
            }
            else if (GradientMarkers.Count < 1)
            {
                SelectedMarker = null;
                return;
            }
            else if (GradientMarkers.Count < 2)
            {
                var newMarker = GradientMarkers.First().Copy();
                newMarker.Position = pos;
                GradientMarkers.Add(newMarker);
                GradientMarkers.SortBy(m => m.Position);
                SelectedMarker = newMarker;
            }
            else
            {
                var prev = GradientMarkers.LastOrDefault(m => m.Position <= pos, GradientMarkers.First());
                var next = GradientMarkers.FirstOrDefault(m => m.Position > pos, GradientMarkers.Last());

                var newMarker = prev == next ? prev.Copy() : prev.Interpolation(next, pos);
                newMarker.Position = pos;
                GradientMarkers.Add(newMarker);
                GradientMarkers.SortBy(m => m.Position);
                SelectedMarker = newMarker;
            }

            CaptureMouse();
            IsClicked = true;
        }

        private void GradientMarkerView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsClicked || SelectedMarker == null || !GradientMarkers.Contains(SelectedMarker))
            {
                return;
            }

            var mouse = e.GetPosition(this);

            if (GradientMarkers.Count > 2 && Math.Abs(mouse.Y) > ActualHeight * 2.0)
            {
                GradientMarkers.Remove(SelectedMarker);
                SelectedMarker = null;
                ReleaseMouseCapture();
                IsClicked = false;
            }
            else
            {
                var range = ActualWidth - MarkerHalfSize * 2.0;
                var pos = (float)Math.Round(Math.Clamp((mouse.X - MarkerHalfSize) / range, 0.0, 1.0), 4);
                SelectedMarker.Position = pos;
            }
        }

        private void GradientMarkerView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsClicked || SelectedMarker == null || !GradientMarkers.Contains(SelectedMarker))
            {
                return;
            }

            IsClicked = false;
            ReleaseMouseCapture();

            var range = ActualWidth - MarkerHalfSize * 2.0;
            var pos = (float)Math.Round(Math.Clamp((e.GetPosition(this).X - MarkerHalfSize) / range, 0.0, 1.0), 4);
            SelectedMarker.Position = pos;
            GradientMarkers.SortBy(m => m.Position);
        }

        private void GradientMarkers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var maker in (e.OldItems?.OfType<GradientMarker>() ?? []))
            {
                maker.PropertyChanged -= Maker_PropertyChanged;
            }
            foreach (var marker in (e.NewItems?.OfType<GradientMarker>() ?? []))
            {
                marker.PropertyChanged += Maker_PropertyChanged;
            }
            InvalidateVisual();
        }

        private void Maker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        static void GradientMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GradientMarkerView gradientMarkerView)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<GradientMarker> oldMakers)
            {
                oldMakers.CollectionChanged -= gradientMarkerView.GradientMarkers_CollectionChanged;
                foreach (var maker in oldMakers)
                {
                    maker.PropertyChanged -= gradientMarkerView.Maker_PropertyChanged;
                }
            }
            if (e.NewValue is ObservableCollection<GradientMarker> newMarkers)
            {
                newMarkers.CollectionChanged += gradientMarkerView.GradientMarkers_CollectionChanged;
                foreach (var maker in newMarkers)
                {
                    maker.PropertyChanged += gradientMarkerView.Maker_PropertyChanged;
                }
            }
        }

        static void BorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GradientMarkerView gradientMarkerView)
            {
                gradientMarkerView.BorderPen = new Pen(gradientMarkerView.BorderBrush, BorderThickness);
                gradientMarkerView.BorderPen.Freeze();
                gradientMarkerView.SelectedBorderPen = new Pen(gradientMarkerView.SelectedBorderBrush, BorderThickness);
                gradientMarkerView.SelectedBorderPen.Freeze();
            }
        }
    }

    abstract class GradientMarker : INotifyPropertyChanged
    {
        private float position;
        public float Position
        {
            get { return position; }
            set { SetProperty(ref position, value); }
        }

        public abstract Color Color { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public abstract GradientMarker Interpolation(GradientMarker nextMarker, float position);

        public abstract GradientMarker Copy();

        protected void SetProperty<TValue>(ref TValue storage, TValue value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<TValue>.Default.Equals(value, storage))
            {
                storage = value;
                if (!string.IsNullOrEmpty(name))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}

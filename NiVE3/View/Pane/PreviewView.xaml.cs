using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Shared.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Converter;
using NiVE3.View.Part;
using NiVE3.ViewModel;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// PreviewView.xaml の相互作用ロジック
    /// </summary>
    public partial class PreviewView : UserControl
    {
        public const double StretchPreview = -2.0;

        public const double StretchPreviewMax100 = -1.0;

        public const double SeparatorScale = 0.0;

        public const double UseImageInterpolationThreshold = 200.0;

        public static readonly double[] ScaleList =
        [
            StretchPreview,
            StretchPreviewMax100,
            SeparatorScale,
            1.5625,
            3.125,
            6.25,
            12.5,
            25.0,
            100 / 3.0,
            50.0,
            100.0,
            200.0,
            400.0,
            800.0,
            1600.0,
            3200.0,
            6400.0
        ];

        public static readonly int[] DownScaleList =
        [
            1,
            2,
            3,
            4
        ];

        public static readonly DependencyProperty IsStretchPreviewProperty = DependencyProperty.Register(
            nameof(IsStretchPreview),
            typeof(bool),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SelectedScaleIndexProperty = DependencyProperty.Register(
            nameof(SelectedScaleIndex),
            typeof(int),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, SelectedScaleChanged)
        );

        public static readonly DependencyProperty PreviewAreaScaleRateProperty = DependencyProperty.Register(
            nameof(PreviewAreaScaleRate),
            typeof(double),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, PreviewAreaScaleRateChanged)
        );

        public static readonly DependencyProperty BoundingBoxBorderThicknessRateProperty = DependencyProperty.Register(
            nameof(BoundingBoxBorderThicknessRate),
            typeof(double),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SelectedDownScaleRateIndexProperty = DependencyProperty.Register(
            nameof(SelectedDownScaleRateIndex),
            typeof(int),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, SelectedDownScaleRateChanged)
        );

        public static readonly DependencyProperty UseImageInterpolationProperty = DependencyProperty.Register(
            nameof(UseImageInterpolation),
            typeof(bool),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyPropertyKey PreviewAreaScaleRateInvertPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PreviewAreaScaleRateInvert),
            typeof(double),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty PreviewAreaScaleRateInvertProperty = PreviewAreaScaleRateInvertPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PreviewAreaImageScaleXPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PreviewAreaImageScaleX),
            typeof(double),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty PreviewAreaImageScaleXProperty = PreviewAreaImageScaleXPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PreviewAreaImageScaleYPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PreviewAreaImageScaleY),
            typeof(double),
            typeof(PreviewView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty PreviewAreaImageScaleYProperty = PreviewAreaImageScaleYPropertyKey.DependencyProperty;

        public double PreviewAreaImageScaleY
        {
            get { return (double)GetValue(PreviewAreaImageScaleYProperty); }
            private set { SetValue(PreviewAreaImageScaleYPropertyKey, value); }
        }

        public double PreviewAreaImageScaleX
        {
            get { return (double)GetValue(PreviewAreaImageScaleXProperty); }
            private set { SetValue(PreviewAreaImageScaleXPropertyKey, value); }
        }

        public double PreviewAreaScaleRateInvert
        {
            get { return (double)GetValue(PreviewAreaScaleRateInvertProperty); }
            private set { SetValue(PreviewAreaScaleRateInvertPropertyKey, value); }
        }

        public bool UseImageInterpolation
        {
            get { return (bool)GetValue(UseImageInterpolationProperty); }
            set { SetValue(UseImageInterpolationProperty, value); }
        }

        public int SelectedDownScaleRateIndex
        {
            get { return (int)GetValue(SelectedDownScaleRateIndexProperty); }
            set { SetValue(SelectedDownScaleRateIndexProperty, value); }
        }

        public double PreviewAreaScaleRate
        {
            get { return (double)GetValue(PreviewAreaScaleRateProperty); }
            set { SetValue(PreviewAreaScaleRateProperty, value); }
        }

        public double BoundingBoxBorderThicknessRate
        {
            get { return (double)GetValue(BoundingBoxBorderThicknessRateProperty); }
            set { SetValue(BoundingBoxBorderThicknessRateProperty, value); }
        }

        public int SelectedScaleIndex
        {
            get { return (int)GetValue(SelectedScaleIndexProperty); }
            set { SetValue(SelectedScaleIndexProperty, value); }
        }

        public bool IsStretchPreview
        {
            get { return (bool)GetValue(IsStretchPreviewProperty); }
            set { SetValue(IsStretchPreviewProperty, value); }
        }

        public DataTemplate BoundingBoxTemplate { get; }

        bool IsStretchLimited { get; set; }

        public PreviewView()
        {
            // NOTE: なぜかXAML上で定義するとエラーが出るため、コードで定義する
            var factory = new FrameworkElementFactory(typeof(BoundingBoxView));

            var boundingBoxBinding = new Binding
            {
                Path = new PropertyPath(nameof(ColoredPreviewBoundingBox.BoundingBox))
            };
            factory.SetBinding(BoundingBoxView.BoundingBoxProperty, boundingBoxBinding);

            var colorBinding = new Binding
            {
                Path = new PropertyPath(nameof(ColoredPreviewBoundingBox.Color)),
                Converter = new ColorToBrushConverter()
            };
            factory.SetBinding(BoundingBoxView.BorderBrushProperty, colorBinding);

            var borderThicknessRateBinding = new Binding
            {
                Path = new PropertyPath(nameof(BoundingBoxBorderThicknessRate)),
                Source = this
            };
            factory.SetBinding(BoundingBoxView.BorderThicknessRateProperty, borderThicknessRateBinding);

            BoundingBoxTemplate = new DataTemplate
            {
                DataType = typeof(ColoredPreviewBoundingBox),
                VisualTree = factory
            };

            var dpi = VisualTreeHelper.GetDpi(this);
            PreviewAreaImageScaleX = 1.0 / dpi.DpiScaleX;
            PreviewAreaImageScaleY = 1.0 / dpi.DpiScaleY;

            InitializeComponent();

            DataContextChanged += PreviewView_DataContextChanged;
        }

        bool IsMouseDown { get; set; }

        Point ClickPoint { get; set; }

        Point PrevPoint { get; set; }

        PreviewViewModel? ViewModel => DataContext as PreviewViewModel;

        bool NeedPositionReset { get; set; } = true;

        void UpdateScale()
        {
            var scale = ScaleList[SelectedScaleIndex];
            IsStretchPreview = scale < 0.0;
            IsStretchLimited = Array.IndexOf(ScaleList, StretchPreviewMax100) > -1;
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                if (viewModel.Width > 0 && viewModel.Height > 0)
                {
                    var dpi = VisualTreeHelper.GetDpi(this);
                    if (scale <= StretchPreview)
                    {
                        scale = Math.Min(Math.Min(PreviewCanvas.ActualWidth / viewModel.Width * dpi.DpiScaleX, PreviewCanvas.ActualHeight / viewModel.Height * dpi.DpiScaleY), 6.4) * 100.0;
                    }
                    else if (scale < SeparatorScale)
                    {
                        scale = Math.Min(Math.Min(PreviewCanvas.ActualWidth / viewModel.Width * dpi.DpiScaleX, PreviewCanvas.ActualHeight / viewModel.Height * dpi.DpiScaleY), 1.0) * 100.0;
                    }
                }
                else
                {
                    scale = 100.0;
                }
                viewModel.Scale = scale;
            }
            else
            {
                scale = 100.0;
            }
            PreviewAreaScaleRate = scale * 0.01;
            UseImageInterpolation = scale < UseImageInterpolationThreshold;
            BoundingBoxBorderThicknessRate = 1.0 / (scale * 0.01);
            SetValue(PreviewAreaScaleRateInvertPropertyKey, 100.0 / scale);
        }

        void LayoutCenterPreviewArea()
        {
            var realWidth = PreviewAnchorGrid.ActualWidth * PreviewAreaImageScaleX;
            var realHeight = PreviewAnchorGrid.ActualHeight * PreviewAreaImageScaleY;
            Canvas.SetLeft(PreviewAnchorGrid, (PreviewCanvas.ActualWidth - realWidth) * 0.5);
            Canvas.SetTop(PreviewAnchorGrid, (PreviewCanvas.ActualHeight - realHeight) * 0.5);
            NeedPositionReset = realWidth <= 0.0 || realHeight <= 0.0;
        }

        void MovePreviewArea(double x, double y, bool isAbsolute)
        {
            if (isAbsolute)
            {
                Canvas.SetLeft(PreviewAnchorGrid, x);
                Canvas.SetTop(PreviewAnchorGrid, y);
            }
            else
            {
                Canvas.SetLeft(PreviewAnchorGrid, Canvas.GetLeft(PreviewAnchorGrid) + x);
                Canvas.SetTop(PreviewAnchorGrid, Canvas.GetTop(PreviewAnchorGrid) + y);
            }
        }

        private void PreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NeedPositionReset = true;
        }

        private void PreviewCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null && !viewModel.IsFootage)
            {
                IsMouseDown = true;
                ClickPoint = e.GetPosition(PreviewCanvas);
                PrevPoint = new Point(Canvas.GetLeft(PreviewAnchorGrid), Canvas.GetTop(PreviewAnchorGrid));
                PreviewCanvas.CaptureMouse();
            }
        }

        private void PreviewCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                var newPoint = e.GetPosition(PreviewCanvas) - ClickPoint + PrevPoint;
                MovePreviewArea(newPoint.X, newPoint.Y, true);
            }
        }

        private void PreviewCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseDown)
            {
                var newPoint = e.GetPosition(PreviewCanvas) - ClickPoint + PrevPoint;
                MovePreviewArea(newPoint.X, newPoint.Y, true);
                PreviewCanvas.ReleaseMouseCapture();
                IsMouseDown = false;
            }
        }

        private void PreviewCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                var maximumScaleIndex = ScaleList.Length - 1;
                if (IsStretchPreview)
                {
                    var currentScale = PreviewAreaScaleRate * 100.0;
                    var newIndex = ScaleList.IndexOf(v => v > currentScale);
                    if (newIndex < 0)
                    {
                        newIndex = maximumScaleIndex;
                    }
                    SelectedScaleIndex = newIndex;
                }
                else
                {
                    SelectedScaleIndex = Math.Min(SelectedScaleIndex + 1, maximumScaleIndex);
                }
            }
            else
            {
                var minimumScaleIndex = Array.IndexOf(ScaleList, SeparatorScale) + 1;
                if (IsStretchPreview)
                {
                    var currentScale = PreviewAreaScaleRate * 100.0;
                    var newIndex = ScaleList.IndexOfLast(v => v < currentScale);
                    if (newIndex < 0)
                    {
                        newIndex = minimumScaleIndex;
                    }
                    SelectedScaleIndex = newIndex;
                }
                else
                {
                    SelectedScaleIndex = Math.Max(SelectedScaleIndex - 1, minimumScaleIndex);
                }
            }
        }

        private void PreviewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((ViewModel?.IsFootage ?? false) || NeedPositionReset)
            {
                LayoutCenterPreviewArea();
            }
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScale();
            if ((ViewModel?.IsFootage ?? false) || NeedPositionReset || IsStretchPreview)
            {
                LayoutCenterPreviewArea();
            }
            else
            {
                var move = ((Point)e.NewSize - (Point)e.PreviousSize) * 0.5;
                MovePreviewArea(move.X, move.Y, false);
            }
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is PreviewViewModel oldViewModel)
            {
                oldViewModel.SourceChanged -= ViewModel_SourceChanged;
            }
            if (e.NewValue is PreviewViewModel newViewModel)
            {
                newViewModel.SourceChanged += ViewModel_SourceChanged;
            }
        }

        private void ViewModel_SourceChanged(object? sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                if (viewModel.IsStretchPreview)
                {
                    SelectedScaleIndex = Array.IndexOf(ScaleList, viewModel.IsStretchLimited ? StretchPreviewMax100 : StretchPreview);
                }
                else
                {
                    SelectedScaleIndex = ScaleList.IndexOf(s => s >= viewModel.Scale);
                }
                UpdateScale();

                SelectedDownScaleRateIndex = Array.IndexOf(DownScaleList, viewModel.DownScaleRate);
            }
        }

        static void SelectedScaleChanged(DependencyObject d,  DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewView preview)
            {
                preview.UpdateScale();
                var viewModel = preview.ViewModel;
                if (viewModel != null)
                {
                    viewModel.IsStretchPreview = preview.IsStretchPreview;
                    viewModel.IsStretchLimited = preview.IsStretchLimited;
                }
            }
        }

        static void PreviewAreaScaleRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PreviewView preview)
            {
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(preview);
            preview.PreviewAreaImageScaleX = preview.PreviewAreaScaleRate / dpi.DpiScaleX;
            preview.PreviewAreaImageScaleY = preview.PreviewAreaScaleRate / dpi.DpiScaleY;

            if (preview.IsStretchPreview)
            {
                preview.LayoutCenterPreviewArea();
            }
            else if (e.OldValue is double oldScale)
            {
                var diffX = (preview.PreviewAnchorGrid.ActualWidth * oldScale) - (preview.PreviewAnchorGrid.ActualWidth * preview.PreviewAreaScaleRate);
                var diffY = (preview.PreviewAnchorGrid.ActualHeight * oldScale) - (preview.PreviewAnchorGrid.ActualHeight * preview.PreviewAreaScaleRate);
                preview.MovePreviewArea(diffX * 0.5 / dpi.DpiScaleX, diffY * 0.5 / dpi.DpiScaleY, false);
            }
        }

        static void SelectedDownScaleRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviewView preview && preview.ViewModel is PreviewViewModel viewModel)
            {
                viewModel.DownScaleRate = DownScaleList[preview.SelectedDownScaleRateIndex];
            }
        }

        private void TimeLocatorView_CurrentTimeChangeByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeCurrentTimeCommand?.Execute(null);
        }
    }
}

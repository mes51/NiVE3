using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Extension;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.View.Part
{
    class BoundingBoxView : FrameworkElement
    {
        const double BorderThickness = 1.0;

        static readonly Geometry AnchorPointGeometry;

        static readonly Pen AnchorPointBorderPen = new Pen(Brushes.White, BorderThickness);

        public static readonly DependencyProperty BoundingBoxRectProperty = DependencyProperty.Register(
            nameof(BoundingBoxRect),
            typeof(PreviewBoundingBox),
            typeof(BoundingBoxView),
            new FrameworkPropertyMetadata(PreviewBoundingBox.Empty, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BorderThicknessRateProperty = DependencyProperty.Register(
            nameof(BorderThicknessRate),
            typeof(double),
            typeof(BoundingBoxView),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BorderColorProperty = DependencyProperty.Register(
            nameof(BorderColor),
            typeof(Color),
            typeof(BoundingBoxView),
            new FrameworkPropertyMetadata(Colors.Red) // NOTE: BoundingBoxRectが変わる前提で何もしない
        );

        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        public double BorderThicknessRate
        {
            get { return (double)GetValue(BorderThicknessRateProperty); }
            set { SetValue(BorderThicknessRateProperty, value); }
        }

        public PreviewBoundingBox BoundingBoxRect
        {
            get { return (PreviewBoundingBox)GetValue(BoundingBoxRectProperty); }
            set { SetValue(BoundingBoxRectProperty, value); }
        }

        static BoundingBoxView()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(BoundingBoxView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

            var anchorPointGeometry = new GeometryGroup();
            anchorPointGeometry.Children.Add(new EllipseGeometry(new Point(), 1.5, 1.5));
            anchorPointGeometry.Children.Add(new CombinedGeometry
            {
                GeometryCombineMode = GeometryCombineMode.Union,
                Geometry1 = new CombinedGeometry
                {
                    GeometryCombineMode = GeometryCombineMode.Exclude,
                    Geometry1 = new EllipseGeometry(new Point(), 5.0, 5.0),
                    Geometry2 = new EllipseGeometry(new Point(), 4.0, 4.0)
                },
                Geometry2 = new CombinedGeometry
                {
                    GeometryCombineMode = GeometryCombineMode.Exclude,
                    Geometry1 = new CombinedGeometry
                    {
                        GeometryCombineMode = GeometryCombineMode.Union,
                        Geometry1 = new RectangleGeometry(new Rect(-0.5, -7.0, 1.0, 14.0)),
                        Geometry2 = new RectangleGeometry(new Rect(-7.0, -0.5, 14.0, 1.0))
                    },
                    Geometry2 = new EllipseGeometry(new Point(), 5.0, 5.0)
                }
            });

            AnchorPointGeometry = anchorPointGeometry.FreezeCurrentObject();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var boundingBox = BoundingBoxRect;
            if (boundingBox.IsInvalid)
            {
                return;
            }

            var penBrush = new SolidColorBrush(BorderColor);

            if (!boundingBox.IsEmpty)
            {
                var boxLines = new StreamGeometry();
                using (var context = boxLines.Open())
                {
                    context.BeginFigure((Point)boundingBox.LeftTop, false, true);
                    context.LineTo((Point)boundingBox.RightTop, true, false);
                    context.LineTo((Point)boundingBox.RightBottom, true, false);
                    context.LineTo((Point)boundingBox.LeftBottom, true, false);
                }
                drawingContext.DrawGeometry(null, new Pen(penBrush, BorderThickness * BorderThicknessRate), boxLines);
            }

            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(BorderThicknessRate, BorderThicknessRate));
            transform.Children.Add(new TranslateTransform(boundingBox.AnchorPoint.X, boundingBox.AnchorPoint.Y));

            drawingContext.PushTransform(transform);
            drawingContext.DrawGeometry(null, AnchorPointBorderPen, AnchorPointGeometry);
            drawingContext.DrawGeometry(penBrush, null, AnchorPointGeometry);
            drawingContext.Pop();
        }
    }
}
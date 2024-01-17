using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using NiVE3.Extension;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.View.Part
{
    class BoundingBoxView : FrameworkElement
    {
        const double BorderThickness = 1.0;

        static readonly Geometry AnchorPointGeometry;

        static readonly Pen AnchorPointBorderPen = new Pen(Brushes.White, BorderThickness);

        public static readonly DependencyProperty BoundingBoxProperty = DependencyProperty.Register(
            nameof(BoundingBox),
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

        public PreviewBoundingBox BoundingBox
        {
            get { return (PreviewBoundingBox)GetValue(BoundingBoxProperty); }
            set { SetValue(BoundingBoxProperty, value); }
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

            var boundingBox = BoundingBox;
            if (boundingBox.IsInvalid)
            {
                return;
            }

            var penBrush = new SolidColorBrush(BorderColor);
            var pen = new Pen(penBrush, BorderThickness * BorderThicknessRate);
            var holdSizePen = new Pen(penBrush, BorderThickness);

            var thicknessRateTransform = new ScaleTransform(BorderThicknessRate, BorderThicknessRate);

            if (!boundingBox.IsEmpty)
            {
                foreach (var shape in boundingBox.BoundingBoxies)
                {
                    if (shape.Path.Length < 2)
                    {
                        continue;
                    }

                    var boxLines = new StreamGeometry();
                    using (var context = boxLines.Open())
                    {
                        context.BeginFigure((Point)shape.Path[0], false, shape.IsClosed);
                        foreach (var p in shape.Path.Skip(1))
                        {
                            context.LineTo((Point)p, true, false);
                        }
                    }

                    if (shape.IsHoldSize && shape.Center.HasValue)
                    {
                        var shapeTransform = new TransformGroup();
                        shapeTransform.Children.Add(new TranslateTransform(-shape.Center.Value.X, -shape.Center.Value.Y));
                        shapeTransform.Children.Add(thicknessRateTransform);
                        shapeTransform.Children.Add(new TranslateTransform(shape.Center.Value.X, shape.Center.Value.Y));
                        drawingContext.PushTransform(shapeTransform);
                        drawingContext.DrawGeometry(null, holdSizePen, boxLines);
                        drawingContext.Pop();
                    }
                    else
                    {
                        drawingContext.DrawGeometry(null, pen, boxLines);
                    }
                }
            }
            var transform = new TransformGroup();
            transform.Children.Add(thicknessRateTransform);
            transform.Children.Add(new TranslateTransform(boundingBox.Center.X, boundingBox.Center.Y));

            drawingContext.PushTransform(transform);
            drawingContext.DrawGeometry(null, AnchorPointBorderPen, AnchorPointGeometry);
            drawingContext.DrawGeometry(penBrush, null, AnchorPointGeometry);
            drawingContext.Pop();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Extension;

namespace NiVE3.View.Primitive
{
    class IndicatorView : FrameworkElement
    {
        static readonly PathGeometry MarkerGeometry;

        static readonly SolidColorBrush MarkerBrush = new SolidColorBrush(Color.FromRgb(86, 186, 255)).FreezeCurrentObject();

        static readonly Pen MarkerInnerLinePen = new Pen(Brushes.Black, 1.0).FreezeCurrentObject();

        static readonly Pen LinePen = new Pen(new SolidColorBrush(Color.FromRgb(228, 0, 0)), 1.0).FreezeCurrentObject();

        static IndicatorView()
        {
            var markerSegments = new PathSegment[]
            {
                new LineSegment(new Point(10.0, 0.0), false),
                new LineSegment(new Point(10.0, 7.0), false),
                new LineSegment(new Point(5.5, 12.0), false),
                new LineSegment(new Point(4.5, 12.0), false),
                new LineSegment(new Point(0.0, 7.0), false)
            };
            MarkerGeometry = new PathGeometry(new PathFigure[] { new PathFigure(new Point(0.0, 0.0), markerSegments, true) }).FreezeCurrentObject();

            IsHitTestVisibleProperty.OverrideMetadata(typeof(IndicatorView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.PushTransform(new TranslateTransform(ActualWidth * 0.5 - 5.0, 0.0));

            drawingContext.DrawGeometry(MarkerBrush, null, MarkerGeometry);
            drawingContext.DrawLine(MarkerInnerLinePen, new Point(5.0, 7.0), new Point(5.0, 12.0));
            drawingContext.DrawLine(LinePen, new Point(5.0, 12.0), new Point(5.0, ActualHeight));

            drawingContext.Pop();
        }
    }
}

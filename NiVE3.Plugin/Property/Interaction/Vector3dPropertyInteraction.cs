using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Interaction
{
    class Vector3dPropertyInteraction : PropertyInteractionBase
    {
        const double BorderThickness = 1.0;

        static readonly Geometry PointGeometry;

        static readonly Pen PointBorderPen = new Pen(Brushes.White, BorderThickness);

        static Vector3dPropertyInteraction()
        {
            var geometry = new CombinedGeometry
            {
                GeometryCombineMode = GeometryCombineMode.Union,
                Geometry1 = new CombinedGeometry
                {
                    GeometryCombineMode = GeometryCombineMode.Union,
                    Geometry1 = new RectangleGeometry(new Rect(-0.5, -6.0, 1.0, 12.0)),
                    Geometry2 = new RectangleGeometry(new Rect(-6.0, -0.5, 12.0, 1.0))
                },
                Geometry2 = new CombinedGeometry
                {
                    GeometryCombineMode = GeometryCombineMode.Exclude,
                    Geometry1 = new RectangleGeometry(new Rect(-4.0, -4.0, 8.0, 8.0)),
                    Geometry2 = new RectangleGeometry(new Rect(-3.0, -3.0, 6.0, 6.0))
                }
            };

            geometry.Freeze();
            PointGeometry = geometry;

            PointBorderPen.Freeze();
        }

        public Vector3dPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        public override bool MouseDown(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override bool MouseMove(Point mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override bool MouseUp(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Color tagColor, ICoordTransformerObject coordTransformer)
        {
            var value = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var screenPos = coordTransformer.LocalCoordToScreenCoord(value);

            var transform = new TranslateTransform(screenPos.X * previewImageScale.X + previewImagePosition.X, screenPos.Y * previewImageScale.Y + previewImagePosition.Y);

            drawingContext.PushTransform(transform);
            drawingContext.DrawGeometry(null, PointBorderPen, PointGeometry);
            drawingContext.DrawGeometry(new SolidColorBrush(tagColor), null, PointGeometry);
            drawingContext.Pop();
        }
    }
}

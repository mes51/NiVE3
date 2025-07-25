using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Interaction
{
    class Vector3dPropertyInteraction : PropertyInteractionBase
    {
        const double BorderThickness = 1.0;

        const double PointHandleArea = 5.0;

        static readonly Geometry PointGeometry;

        static readonly Pen PointBorderPen = new Pen(Brushes.White, BorderThickness);

        bool Is3D { get; }

        Vector2d InteractionStartPoint { get; set; }

        Vector3d InteractionStartValue { get; set; }

        Vector3d PrevCurrentRawValue { get; set; }

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

        public Vector3dPropertyInteraction(IPropertyInteractionViewModel viewModel, bool is3D) : base(viewModel)
        {
            Is3D = is3D;
        }

        public override bool HitTestInteraction(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer)
        {
            var value = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var screenPos = coordTransformer.LocalCoordToScreenCoord(value);

            return Math.Abs(screenPos.X - mousePositionInPreview.X) <= PointHandleArea && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= PointHandleArea;
        }

        public override bool MouseLeftButtonDown(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer)
        {
            var value = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var screenPos = coordTransformer.LocalCoordToScreenCoord(value);

            if (Math.Abs(screenPos.X - mousePositionInPreview.X) <= PointHandleArea && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= PointHandleArea)
            {
                InteractionStartPoint = mousePositionInPreview;
                InteractionStartValue = coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                PrevCurrentRawValue = value;
                IsInteracting = true;
                ViewModel.BeginEditCommand.Execute(null);

                return true;
            }
            else
            {
                return false;
            }
        }

        public override void MouseLeftButtonDrag(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting)
            {
                return;
            }

            var pos = coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            var diff = pos - InteractionStartValue;

            if (!Is3D)
            {
                diff = new Vector3d(diff.X, diff.Y, 0.0);
            }
            ViewModel.CurrentTimeRawValue = PrevCurrentRawValue + diff;
        }

        public override void MouseLeftButtonUp(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting)
            {
                return;
            }

            var pos = coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            var diff = pos - InteractionStartValue;

            if (!Is3D)
            {
                diff = new Vector3d(diff.X, diff.Y, 0.0);
            }
            ViewModel.CurrentTimeRawValue = PrevCurrentRawValue + diff;

            ViewModel.EndEditCommand.Execute(null);
            IsInteracting = false;
        }

        public override void AbortInteraction()
        {
            ViewModel.AbortEditCommand.Execute(null);
            IsInteracting = false;
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

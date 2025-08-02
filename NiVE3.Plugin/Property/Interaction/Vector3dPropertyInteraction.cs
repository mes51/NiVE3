using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property.Interaction
{
    class Vector3dPropertyInteraction : PropertyInteractionBase
    {
        const double BorderThickness = 1.0;

        const double PreviewPropertyPointSize = 2.0;

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

        public override bool HitTestInteraction(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var screenPos = coordTransformer.LocalCoordToScreenCoord(value);
            var hitArea = PointHandleArea / previewImageScale;

            return Math.Abs(screenPos.X - mousePositionInPreview.X) <= hitArea.X && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= hitArea.Y;
        }

        public override bool MouseLeftButtonDown(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var screenPos = coordTransformer.LocalCoordToScreenCoord(value);
            var hitArea = PointHandleArea / previewImageScale;

            if (Math.Abs(screenPos.X - mousePositionInPreview.X) <= hitArea.X && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= hitArea.Y)
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

        public override void MouseLeftButtonDrag(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
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

        public override void MouseLeftButtonUp(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
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

        public override void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Time globalTime, double frameRate, int previewFrameRange, Color tagColor, ICoordTransformerObject coordTransformer)
        {
            var points = new List<Point>(previewFrameRange * 2 + 1);
            for (var i = 0; i < previewFrameRange; i++)
            {
                var value = (Vector3d)(ViewModel.GetValue(globalTime - new Time(previewFrameRange - i - 1, frameRate)) ?? Vector3d.Zero);
                var screenPos = coordTransformer.LocalCoordToScreenCoord(value);
                points.Add(new Point(screenPos.X * previewImageScale.X + previewImagePosition.X, screenPos.Y * previewImageScale.Y + previewImagePosition.Y));
            }

            var currentValue = (Vector3d)(ViewModel.CurrentTimeValue ?? Vector3d.Zero);
            var currentScreenPos = coordTransformer.LocalCoordToScreenCoord(currentValue);
            points.Add(new Point(currentScreenPos.X * previewImageScale.X + previewImagePosition.X, currentScreenPos.Y * previewImageScale.Y + previewImagePosition.Y));

            for (var i = 0; i < previewFrameRange; i++)
            {
                var value = (Vector3d)(ViewModel.GetValue(globalTime + new Time(i + 1, frameRate)) ?? Vector3d.Zero);
                var screenPos = coordTransformer.LocalCoordToScreenCoord(value);
                points.Add(new Point(screenPos.X * previewImageScale.X + previewImagePosition.X, screenPos.Y * previewImageScale.Y + previewImagePosition.Y));
            }

            var brush = new SolidColorBrush(tagColor);

            if (points.Count > 1)
            {
                brush.Freeze();
                var pen = new Pen(brush, 1.0);

                var line = new StreamGeometry();
                using (var context = line.Open())
                {
                    context.BeginFigure(points[0], false, false);
                    for (var i = 1; i < points.Count; i++)
                    {
                        context.LineTo(points[i], true, false);
                    }
                }

                drawingContext.DrawGeometry(null, pen, line);

                for (var i = 0; i < points.Count; i++)
                {
                    if (i == previewFrameRange)
                    {
                        continue;
                    }

                    drawingContext.DrawEllipse(brush, null, points[i], PreviewPropertyPointSize, PreviewPropertyPointSize);
                }
            }

            var currentPos = points[previewFrameRange];
            var transform = new TranslateTransform(currentPos.X, currentPos.Y);

            drawingContext.PushTransform(transform);
            drawingContext.DrawGeometry(null, PointBorderPen, PointGeometry);
            drawingContext.DrawGeometry(new SolidColorBrush(tagColor), null, PointGeometry);
            drawingContext.Pop();
        }
    }
}

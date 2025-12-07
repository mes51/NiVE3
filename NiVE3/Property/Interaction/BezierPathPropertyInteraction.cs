using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Interaction;
using NiVE3.Plugin.ValueObject;
using static System.Windows.Forms.MonthCalendar;

namespace NiVE3.Property.Interaction
{
    class BezierPathPropertyInteraction : PropertyInteractionBase
    {
        const double LineThickness = 1.0;

        const double PointHandleArea = 5.0;

        static readonly Vector2d PointRadius = new Vector2d(3.0);

        static readonly Size PointSize = (Size)(PointRadius * 2.0);

        public BezierPathPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        BezierPath? PrevValue { get; set; }

        bool ClickIsBegin { get; set; }

        int ClickedPointIndex { get; set; }

        public override bool HitTestInteraction(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint, coordTransformer))
                {
                    return true;
                }

                foreach (var p in value.Points)
                {
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint, coordTransformer) ||
                        (!p.IsLinear && (IsHit(mousePositionInPreview, hitArea, p.ControlPoint1, coordTransformer) || IsHit(mousePositionInPreview, hitArea, p.ControlPoint2, coordTransformer))))
                    {
                        return true; 
                    }
                }

                return false;
            }
            else
            {
                return !IsHit(mousePositionInPreview, hitArea, value.BeginPoint, coordTransformer) || value.Points.Length > 1;
            }
        }

        public override bool MouseLeftButtonDown(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint, coordTransformer))
                {
                    PrevValue = value;
                    ClickIsBegin = true;
                    ClickedPointIndex = -1;
                    IsInteracting = true;
                    ViewModel.BeginEditCommand.Execute(null);
                    return true;
                }

                for (var i = 0; i < value.Points.Length; i++)
                {
                    var p = value.Points[i];
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint, coordTransformer) ||
                        (!p.IsLinear && (IsHit(mousePositionInPreview, hitArea, p.ControlPoint1, coordTransformer) || IsHit(mousePositionInPreview, hitArea, p.ControlPoint2, coordTransformer))))
                    {
                        PrevValue = value;
                        ClickIsBegin = false;
                        ClickedPointIndex = i;
                        IsInteracting = true;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }

                PrevValue = null;
                ClickIsBegin = false;
                ClickedPointIndex = -1;
                return false;
            }
            else
            {
                if (value.IsInvalid())
                {
                    PrevValue = value;
                    ClickIsBegin = true;
                    ClickedPointIndex = -1;
                    IsInteracting = true;
                    ViewModel.BeginEditCommand.Execute(null);
                }
                else if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint, coordTransformer) && value.Points.Length > 1)
                {
                    ViewModel.BeginEditCommand.Execute(null);
                    ViewModel.CurrentTimeRawValue = value.ClosePath();
                    ViewModel.EndEditCommand.Execute(null);

                    PrevValue = null;
                    ClickIsBegin = false;
                    ClickedPointIndex = -1;
                }
                else
                {
                    PrevValue = value;
                    ClickIsBegin = false;
                    ClickedPointIndex = value.Points.Length;
                    IsInteracting = true;
                    ViewModel.BeginEditCommand.Execute(null);
                }

                return true;
            }
        }

        public override void MouseLeftButtonDrag(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting || PrevValue == null)
            {
                return;
            }

            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            if (PrevValue.IsClosed)
            {
                if (ClickIsBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, PrevValue.Points, true);
                }
                else
                {
                    var point = PrevValue.Points[ClickedPointIndex];
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.SetItem(ClickedPointIndex, new BezierPoint(point.ControlPoint1, point.ControlPoint2, pos, point.IsLinear)), true);
                }
            }
            else
            {
                if (ClickIsBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, [], false);
                }
                else
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(new BezierPoint(Vector2d.Zero, Vector2d.Zero, pos, true)), false);
                }
            }
        }

        public override void MouseLeftButtonUp(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting || PrevValue == null)
            {
                return;
            }

            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            if (PrevValue.IsClosed)
            {
                if (ClickIsBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, PrevValue.Points, true);
                }
                else
                {
                    var point = PrevValue.Points[ClickedPointIndex];
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.SetItem(ClickedPointIndex, new BezierPoint(point.ControlPoint1, point.ControlPoint2, pos, point.IsLinear)), true);
                }
            }
            else
            {
                if (ClickIsBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, [], false);
                }
                else
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(new BezierPoint(Vector2d.Zero, Vector2d.Zero, pos, true)), false);
                }
            }

            ViewModel.EndEditCommand.Execute(null);
            IsInteracting = false;
            PrevValue = null;
            ClickIsBegin = false;
            ClickedPointIndex = -1;
        }

        public override void AbortInteraction()
        {
            ViewModel.AbortEditCommand.Execute(null);
            IsInteracting = false;
            PrevValue = null;
            ClickIsBegin = false;
            ClickedPointIndex = -1;
        }

        public override void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Time globalTime, double frameRate, int previewFrameRange, Color tagColor, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);

            if (value.IsInvalid())
            {
                return;
            }

            var brush = new SolidColorBrush(tagColor);
            var screenBeginPos = coordTransformer.LocalCoordToScreenCoord((Vector3d)value.BeginPoint, globalTime) * previewImageScale + previewImagePosition;
            drawingContext.DrawRectangle(brush, null, new Rect((Point)(screenBeginPos - PointRadius), PointSize));
            foreach (var p in value.Points)
            {
                var screenPos = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)p.EndPoint, globalTime) * previewImageScale + previewImagePosition - PointRadius);
                drawingContext.DrawRectangle(brush, null, new Rect(screenPos, PointSize));
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure((Point)screenBeginPos, false, value.IsClosed);
                var lastPoint = value.BeginPoint;
                foreach (var point in value.Points)
                {
                    var screenEndPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)point.EndPoint, globalTime) * previewImageScale + previewImagePosition);
                    if (point.IsLinear)
                    {
                        context.LineTo(screenEndPoint, true, true);
                    }
                    else
                    {
                        var controlPoint1 = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(lastPoint + point.ControlPoint1), globalTime) * previewImageScale + previewImagePosition);
                        var controlPoint2 = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.ControlPoint2), globalTime) * previewImageScale + previewImagePosition);
                        context.BezierTo(controlPoint1, controlPoint2, screenEndPoint, true, true);
                    }
                    lastPoint = point.EndPoint;
                } 
            }
            drawingContext.DrawGeometry(null, new Pen(brush, LineThickness), geometry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsHit(in Vector2d mousePositionInPreview, in Vector2d hitArea, in Vector2d point, ICoordTransformerObject coordTransformer)
        {
            var screenPos = coordTransformer.LocalCoordToScreenCoord((Vector3d)point);
            return Math.Abs(screenPos.X - mousePositionInPreview.X) <= hitArea.X && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= hitArea.Y;
        }
    }
}

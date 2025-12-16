using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
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
        const int BeginPointIndex = -2;

        const double LineThickness = 1.0;

        const double PointHandleArea = 8.0;

        const double ControlPointCircleRadius = 2.0;

        static readonly Vector2d PointRadius = new Vector2d(2.0);

        static readonly Vector2d LargePointRadius = new Vector2d(4.0);

        static readonly Size PointSize = (Size)(PointRadius * 2.0);

        static readonly Size LargePointSize = (Size)(LargePointRadius * 2.0);

        public BezierPathPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        BezierPath? PrevValue { get; set; }

        bool ClickNewBegin { get; set; }

        bool IsMoved { get; set; }

        Vector2d ClickedPosition { get; set; }

        BezierPoint? CurrentCreatingPoint { get; set; }

        List<int> SelectedPointIndices { get; } = [];

        public override bool HitTestInteraction(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer))
                {
                    return true;
                }

                foreach (var p in value.Points)
                {
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint, coordTransformer) ||
                        (!p.IsLinear && (IsHit(mousePositionInPreview, hitArea, p.PrevControlPoint, coordTransformer) || IsHit(mousePositionInPreview, hitArea, p.NextControlPoint, coordTransformer))))
                    {
                        return true; 
                    }
                }

                return false;
            }
            else
            {
                return !IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer) || value.Points.Length > 1;
            }
        }

        public override bool MouseLeftButtonDown(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (IsInteracting)
            {
                return true;
            }

            PrevValue = null;
            ClickNewBegin = false;
            CurrentCreatingPoint = null;

            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer))
                {
                    if (IsShiftKeyDown())
                    {
#pragma warning disable CA1868 // NOTE: 要素の削除や追加のみでは無いため無視する
                        if (SelectedPointIndices.Contains(BeginPointIndex))
#pragma warning restore CA1868
                        {
                            SelectedPointIndices.Remove(BeginPointIndex);
                            return false;
                        }
                        else
                        {
                            SelectedPointIndices.Add(BeginPointIndex);
                            SelectedPointIndices.Sort();
                            PrevValue = value;
                            IsInteracting = true;
                            ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            ViewModel.BeginEditCommand.Execute(null);
                            return true;
                        }
                    }
                    else
                    {
                        if (!SelectedPointIndices.Contains(BeginPointIndex))
                        {
                            SelectedPointIndices.Clear();
                            SelectedPointIndices.Add(BeginPointIndex);
                        }
                        PrevValue = value;
                        IsInteracting = true;
                        ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }

                for (var i = 0; i < value.Points.Length; i++)
                {
                    var p = value.Points[i];
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint, coordTransformer) ||
                        (!p.IsLinear && (IsHit(mousePositionInPreview, hitArea, p.PrevControlPoint, coordTransformer) || IsHit(mousePositionInPreview, hitArea, p.NextControlPoint, coordTransformer))))
                    {
                        if (IsShiftKeyDown())
                        {
#pragma warning disable CA1868 // NOTE: 要素の削除や追加のみでは無いため無視する
                            if (SelectedPointIndices.Contains(i))
#pragma warning restore CA1868
                            {
                                SelectedPointIndices.Remove(i);
                                return false;
                            }
                            else
                            {
                                SelectedPointIndices.Add(i);
                                SelectedPointIndices.Sort();
                                PrevValue = value;
                                IsInteracting = true;
                                ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                                ViewModel.BeginEditCommand.Execute(null);
                                return true;
                            }
                        }
                        else
                        {
                            if (!SelectedPointIndices.Contains(i))
                            {
                                SelectedPointIndices.Clear();
                                SelectedPointIndices.Add(i);
                            }
                            PrevValue = value;
                            IsInteracting = true;
                            ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            ViewModel.BeginEditCommand.Execute(null);
                        }
                    }
                }

                return false;
            }
            else
            {
                if (value.IsInvalid())
                {
                    PrevValue = value;
                    ClickNewBegin = true;
                    IsInteracting = true;
                    ViewModel.BeginEditCommand.Execute(null);

                    var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, [], false);
                    ClickedPosition = pos;
                }
                else if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer) && value.Points.Length > 1)
                {
                    ViewModel.BeginEditCommand.Execute(null);
                    ViewModel.CurrentTimeRawValue = value.ClosePath();
                    ViewModel.EndEditCommand.Execute(null);
                }
                else
                {
                    PrevValue = value;
                    IsInteracting = true;
                    ViewModel.BeginEditCommand.Execute(null);

                    var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(new BezierPoint(Vector2d.Zero, Vector2d.Zero, pos, true)), false);
                    ClickedPosition = pos;
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

            IsMoved = true;
            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            if (PrevValue.IsClosed)
            {
                var diff = pos - ClickedPosition;
                var newPoints = PrevValue.Points.ToArray();
                var newBeginPoint = PrevValue.BeginPoint;
                if (SelectedPointIndices.Contains(BeginPointIndex))
                {
                    newBeginPoint = new BezierPoint(newBeginPoint.PrevControlPoint, newBeginPoint.NextControlPoint, newBeginPoint.EndPoint + diff, newBeginPoint.IsLinear);
                }
                for (var i = 0; i < newPoints.Length; i++)
                {
                    if (SelectedPointIndices.Contains(i))
                    {
                        var oldPoint = newPoints[i];
                        newPoints[i] = new BezierPoint(oldPoint.PrevControlPoint, oldPoint.NextControlPoint, oldPoint.EndPoint + diff, oldPoint.IsLinear);
                    }
                }

                ViewModel.CurrentTimeRawValue = new BezierPath(newBeginPoint, newPoints, true);
            }
            else
            {
                var diff = pos - ClickedPosition;
                CurrentCreatingPoint = new BezierPoint(-diff, diff, ClickedPosition, false);
                if (ClickNewBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(CurrentCreatingPoint, [], false);
                }
                else
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(CurrentCreatingPoint), false);
                }
            }
        }

        public override void MouseLeftButtonUp(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting || PrevValue == null)
            {
                return;
            }

            if (!IsMoved && PrevValue.IsClosed)
            {
                ViewModel.AbortEditCommand.Execute(null);
                IsInteracting = false;
                PrevValue = null;
                return;
            }

            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
            if (PrevValue.IsClosed)
            {
                var diff = pos - ClickedPosition;
                var newPoints = PrevValue.Points.ToArray();
                var newBeginPoint = PrevValue.BeginPoint;
                if (SelectedPointIndices.Contains(BeginPointIndex))
                {
                    newBeginPoint = new BezierPoint(newBeginPoint.PrevControlPoint, newBeginPoint.NextControlPoint, newBeginPoint.EndPoint + diff, newBeginPoint.IsLinear);
                }
                for (var i = 0; i < newPoints.Length; i++)
                {
                    if (SelectedPointIndices.Contains(i))
                    {
                        var oldPoint = newPoints[i];
                        newPoints[i] = new BezierPoint(oldPoint.PrevControlPoint, oldPoint.NextControlPoint, oldPoint.EndPoint + diff, oldPoint.IsLinear);
                    }
                }

                ViewModel.CurrentTimeRawValue = new BezierPath(newBeginPoint, newPoints, true);
            }
            else
            {
                var diff = pos - ClickedPosition;
                CurrentCreatingPoint = IsMoved ? new BezierPoint(-diff, diff, ClickedPosition, false) : new BezierPoint(Vector2d.Zero, Vector2d.Zero, ClickedPosition, true);
                if (ClickNewBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(CurrentCreatingPoint, [], false);
                }
                else
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(CurrentCreatingPoint), false);
                }
            }

            ViewModel.EndEditCommand.Execute(null);
            IsInteracting = false;
            PrevValue = null;
            ClickNewBegin = false;
            IsMoved = false;
        }

        public override void AbortInteraction()
        {
            ViewModel.AbortEditCommand.Execute(null);
            IsInteracting = false;
            PrevValue = null;
            ClickNewBegin = false;
        }

        public override void ClearState()
        {
            SelectedPointIndices.Clear();
        }

        public override void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Time globalTime, double frameRate, int previewFrameRange, Color tagColor, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);

            if (value.IsInvalid())
            {
                return;
            }

            var selectedPoints = value.Points.Where((_, i) => SelectedPointIndices.Contains(i));
            if (SelectedPointIndices.Contains(BeginPointIndex))
            {
                selectedPoints = selectedPoints.Prepend(value.BeginPoint);
            }
            if (IsInteracting && CurrentCreatingPoint != null)
            {
                selectedPoints = selectedPoints.Append(CurrentCreatingPoint);
            }
            var fixedSelectedPoints = selectedPoints.ToArray();

            var brush = new SolidColorBrush(tagColor);
            var pen = new Pen(brush, LineThickness);
            foreach (var p in value.Points.Prepend(value.BeginPoint))
            {
                var pointRadius = PointRadius;
                var pointSize = PointSize;
                if (fixedSelectedPoints.Contains(p))
                {
                    pointRadius = LargePointRadius;
                    pointSize = LargePointSize;
                }
                var screenPos = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)p.EndPoint, globalTime) * previewImageScale + previewImagePosition - pointRadius);
                drawingContext.DrawRectangle(brush, null, new Rect(screenPos, pointSize));
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                var screenBeginPos = coordTransformer.LocalCoordToScreenCoord((Vector3d)value.BeginPoint.EndPoint, globalTime) * previewImageScale + previewImagePosition;
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
                        var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(lastPoint.EndPoint + lastPoint.NextControlPoint), globalTime) * previewImageScale + previewImagePosition);
                        var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.PrevControlPoint), globalTime) * previewImageScale + previewImagePosition);
                        context.BezierTo(prevControlPoint, nextControlPoint, screenEndPoint, true, true);
                    }
                    lastPoint = point;
                }

                if (value.IsClosed && !value.BeginPoint.IsLinear)
                {
                    var screenEndPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)value.BeginPoint.EndPoint, globalTime) * previewImageScale + previewImagePosition);
                    var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(lastPoint.EndPoint + lastPoint.NextControlPoint), globalTime) * previewImageScale + previewImagePosition);
                    var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(value.BeginPoint.EndPoint + value.BeginPoint.PrevControlPoint), globalTime) * previewImageScale + previewImagePosition);
                    context.BezierTo(prevControlPoint, nextControlPoint, screenEndPoint, true, true);
                }
            }
            drawingContext.DrawGeometry(null, pen, geometry);

            foreach (var point in fixedSelectedPoints.Where(p => !p.IsLinear))
            {
                var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.PrevControlPoint), globalTime) * previewImageScale + previewImagePosition);
                var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.NextControlPoint), globalTime) * previewImageScale + previewImagePosition);
                drawingContext.DrawEllipse(brush, null, prevControlPoint, ControlPointCircleRadius, ControlPointCircleRadius);
                drawingContext.DrawEllipse(brush, null, nextControlPoint, ControlPointCircleRadius, ControlPointCircleRadius);
                drawingContext.DrawLine(pen, prevControlPoint, nextControlPoint);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsHit(in Vector2d mousePositionInPreview, in Vector2d hitArea, in Vector2d point, ICoordTransformerObject coordTransformer)
        {
            var screenPos = coordTransformer.LocalCoordToScreenCoord((Vector3d)point);
            return Math.Abs(screenPos.X - mousePositionInPreview.X) <= hitArea.X && Math.Abs(screenPos.Y - mousePositionInPreview.Y) <= hitArea.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsShiftKeyDown()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }
    }
}

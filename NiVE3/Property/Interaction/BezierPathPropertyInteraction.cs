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

        const double MoveStartThreshold = 3.0;

        static readonly Vector2d PointRadius = new Vector2d(2.0);

        static readonly Vector2d LargePointRadius = new Vector2d(4.0);

        static readonly Size PointSize = (Size)(PointRadius * 2.0);

        static readonly Size LargePointSize = (Size)(LargePointRadius * 2.0);

        public BezierPathPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        BezierPath? PrevValue { get; set; }

        bool ClickNewBegin { get; set; }

        bool IsMoved { get; set; }

        PointPosition ClickedPointPosition { get; set; }

        Vector2d ClickedPosition { get; set; }

        BezierPoint? CurrentCreatingPoint { get; set; }

        Vector2d LastOppositeControlPointPosition { get; set; }

        Vector2d FirstMousePositionInPreview { get; set; }

        bool ResetFreeControlPoint { get; set; }

        List<int> SelectedPointIndices { get; } = [];

        bool IsKeyDowned { get; set; }

        public override bool HitTestInteraction(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                foreach (var p in value.Points.Prepend(value.BeginPoint))
                {
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint, coordTransformer))
                    {
                        return true; 
                    }
                }

                var selectedPoints = SelectedPointIndices.Where(i => i > -1 && !value.Points[i].IsLinear).Select(i => value.Points[i]);
                if (SelectedPointIndices.Contains(BeginPointIndex) && !value.BeginPoint.IsLinear)
                {
                    selectedPoints = selectedPoints.Prepend(value.BeginPoint);
                }
                foreach (var p in selectedPoints)
                {
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.PrevControlPoint, coordTransformer) || IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.NextControlPoint, coordTransformer))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return !IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer) || value.Points.Length > 0;
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
            ClickedPointPosition = PointPosition.None;
            FirstMousePositionInPreview = mousePositionInPreview;

            var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
            if (value.IsInvalid())
            {
                SelectedPointIndices.Clear();
            }
            else
            {
                SelectedPointIndices.RemoveAll(i => i >= value.Points.Length);
            }

            var hitArea = PointHandleArea / previewImageScale;
            if (value.IsClosed)
            {
                // すでにShiftかAltを押している場合はIsKeyDownedをtrueにする
                IsKeyDowned = IsShiftKeyDown() || IsAltKeyDown();

                // 制御点の編集
                var selectedPoints = SelectedPointIndices.Where(i => i > -1 && !value.Points[i].IsLinear).Select(i => (i, value.Points[i]));
                if (SelectedPointIndices.Contains(BeginPointIndex) && !value.BeginPoint.IsLinear)
                {
                    selectedPoints = selectedPoints.Prepend((BeginPointIndex, value.BeginPoint));
                }
                foreach (var (i, p) in selectedPoints)
                {
                    if (IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.PrevControlPoint, coordTransformer))
                    {
                        SelectedPointIndices.Clear();
                        SelectedPointIndices.Add(i);
                        PrevValue = value;
                        IsInteracting = true;
                        ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                        ClickedPointPosition = PointPosition.PrevControlPoint;
                        ResetFreeControlPoint = !p.IsFreeControlPoint;
                        LastOppositeControlPointPosition = p.NextControlPoint;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                    else if (IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.NextControlPoint, coordTransformer))
                    {
                        SelectedPointIndices.Clear();
                        SelectedPointIndices.Add(i);
                        PrevValue = value;
                        IsInteracting = true;
                        ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                        ClickedPointPosition = PointPosition.NextControlPoint;
                        ResetFreeControlPoint = !p.IsFreeControlPoint;
                        LastOppositeControlPointPosition = p.PrevControlPoint;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }

                // 始点の編集
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
                            ClickedPointPosition = PointPosition.EndPoint;
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
                        ClickedPointPosition = PointPosition.EndPoint;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }

                // 各点の編集
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
                                ClickedPointPosition = PointPosition.EndPoint;
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
                            ClickedPointPosition = PointPosition.EndPoint;
                            ViewModel.BeginEditCommand.Execute(null);
                        }
                    }
                }

                return false;
            }
            else
            {
                SelectedPointIndices.Clear();
                if (value.IsInvalid())
                {
                    PrevValue = value;
                    ClickNewBegin = true;
                    IsInteracting = true;
                    ClickedPointPosition = PointPosition.EndPoint;
                    SelectedPointIndices.Add(BeginPointIndex);
                    ViewModel.BeginEditCommand.Execute(null);

                    var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, [], false);
                    ClickedPosition = pos;
                }
                else if (IsHit(mousePositionInPreview, hitArea, value.BeginPoint.EndPoint, coordTransformer) && value.Points.Length > 0)
                {
                    SelectedPointIndices.Add(BeginPointIndex);

                    ViewModel.BeginEditCommand.Execute(null);
                    ViewModel.CurrentTimeRawValue = value.ClosePath();
                    ViewModel.EndEditCommand.Execute(null);
                }
                else
                {
                    PrevValue = value;
                    IsInteracting = true;
                    ClickedPointPosition = PointPosition.EndPoint;
                    SelectedPointIndices.Add(value.Points.Length);
                    ViewModel.BeginEditCommand.Execute(null);

                    if (IsShiftKeyDown())
                    {
                        var lastPoint = value.Points.Length > 0 ? value.Points[^1] : value.BeginPoint;
                        var lastMousePosition = coordTransformer.LocalCoordToScreenCoord((Vector3d)lastPoint.EndPoint);
                        var mouseDiff = Vector2d.Abs(mousePositionInPreview - lastMousePosition);
                        if (mouseDiff.X >= mouseDiff.Y)
                        {
                            mousePositionInPreview = new Vector2d(mousePositionInPreview.X, lastMousePosition.Y);
                        }
                        else
                        {
                            mousePositionInPreview = new Vector2d(lastMousePosition.X, mousePositionInPreview.Y);
                        }
                    }

                    var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(new BezierPoint(Vector2d.Zero, Vector2d.Zero, pos, true, false)), false);
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

            if (!IsMoved && Vector2d.Distance(FirstMousePositionInPreview, mousePositionInPreview) < MoveStartThreshold)
            {
                return;
            }

            IsMoved = true;
            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);
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
                ClickedPointPosition = PointPosition.None;
                IsInteracting = false;
                PrevValue = null;
                IsKeyDowned = false;
                return;
            }

            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);

            ViewModel.EndEditCommand.Execute(null);
            ClickedPointPosition = PointPosition.None;
            IsInteracting = false;
            PrevValue = null;
            ClickNewBegin = false;
            IsMoved = false;
            IsKeyDowned = false;
        }

        public override bool KeyDown(Key key, Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (IsKeyDowned)
            {
                return false;
            }

            if (key == Key.Delete && SelectedPointIndices.Count > 0)
            {
                var value = (BezierPath)(ViewModel.CurrentTimeValue ?? BezierPath.Empty);
                var newPoints = value.Points.Where((_, index) => !SelectedPointIndices.Contains(index)).ToArray();
                ViewModel.BeginEditCommand.Execute(null);
                if (newPoints.Length < 1 && SelectedPointIndices.Contains(BeginPointIndex))
                {
                    ViewModel.CurrentTimeRawValue = BezierPath.Empty;
                }
                else
                {
                    if (SelectedPointIndices.Contains(BeginPointIndex))
                    {
                        ViewModel.CurrentTimeRawValue = new BezierPath(newPoints[0], newPoints[1..], newPoints.Length > 2 && value.IsClosed);
                    }
                    else
                    {
                        ViewModel.CurrentTimeRawValue = new BezierPath(value.BeginPoint, newPoints, newPoints.Length > 1 && value.IsClosed);
                    }
                }
                ViewModel.EndEditCommand.Execute(null);
                SelectedPointIndices.Clear();
                return true;
            }

            if (!IsInteracting || PrevValue == null)
            {
                return false;
            }

            IsKeyDowned = true;
            IsMoved = true;
            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);
            return true;
        }

        public override bool KeyUp(Key key, Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting || PrevValue == null || key == Key.Delete)
            {
                if (key != Key.Delete)
                {
                    IsKeyDowned = false;
                }
                return false;
            }

            IsKeyDowned = false;
            IsMoved = true;
            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);
            return true;
        }

        public override void AbortInteraction()
        {
            ViewModel.AbortEditCommand.Execute(null);
            IsInteracting = false;
            PrevValue = null;
            ClickNewBegin = false;
            IsMoved = false;
            IsKeyDowned = false;
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

                if (value.IsClosed && (!value.BeginPoint.IsLinear || !lastPoint.IsLinear))
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
                var endPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)point.EndPoint, globalTime) * previewImageScale + previewImagePosition);
                var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.PrevControlPoint), globalTime) * previewImageScale + previewImagePosition);
                var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)(point.EndPoint + point.NextControlPoint), globalTime) * previewImageScale + previewImagePosition);
                drawingContext.DrawEllipse(brush, null, prevControlPoint, ControlPointCircleRadius, ControlPointCircleRadius);
                drawingContext.DrawEllipse(brush, null, nextControlPoint, ControlPointCircleRadius, ControlPointCircleRadius);
                drawingContext.DrawLine(pen, prevControlPoint, endPoint);
                drawingContext.DrawLine(pen, endPoint, nextControlPoint);
            }
        }

        void UpdateCurrentRawValue(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer)
        {
            if (PrevValue == null)
            {
                return;
            }

            var isAltDown = IsAltKeyDown();
            if (PrevValue.IsClosed)
            {
                switch (ClickedPointPosition)
                {
                    case PointPosition.EndPoint:
                        {
                            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            var diff = pos - ClickedPosition;
                            var newPoints = PrevValue.Points.ToArray();
                            var newBeginPoint = PrevValue.BeginPoint;
                            if (SelectedPointIndices.Contains(BeginPointIndex))
                            {
                                newBeginPoint = new BezierPoint(newBeginPoint.PrevControlPoint, newBeginPoint.NextControlPoint, newBeginPoint.EndPoint + diff, newBeginPoint.IsLinear, newBeginPoint.IsFreeControlPoint);
                            }
                            for (var i = 0; i < newPoints.Length; i++)
                            {
                                if (SelectedPointIndices.Contains(i))
                                {
                                    var oldPoint = newPoints[i];
                                    newPoints[i] = new BezierPoint(oldPoint.PrevControlPoint, oldPoint.NextControlPoint, oldPoint.EndPoint + diff, oldPoint.IsLinear, oldPoint.IsFreeControlPoint);
                                }
                            }

                            ViewModel.CurrentTimeRawValue = new BezierPath(newBeginPoint, newPoints, true);
                        }
                        break;
                    case PointPosition.PrevControlPoint:
                        {
                            var isBegin = SelectedPointIndices.Contains(BeginPointIndex);
                            var targetPoint = isBegin ? PrevValue.BeginPoint : PrevValue.Points[SelectedPointIndices[0]];
                            if (IsShiftKeyDown())
                            {
                                var lastMousePosition = coordTransformer.LocalCoordToScreenCoord((Vector3d)targetPoint.EndPoint);
                                var mouseDiff = Vector2d.Abs(mousePositionInPreview - lastMousePosition);
                                if (mouseDiff.X >= mouseDiff.Y)
                                {
                                    mousePositionInPreview = new Vector2d(mousePositionInPreview.X, lastMousePosition.Y);
                                }
                                else
                                {
                                    mousePositionInPreview = new Vector2d(lastMousePosition.X, mousePositionInPreview.Y);
                                }
                            }

                            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            var diff = pos - ClickedPosition;
                            var newControlPoint = pos - targetPoint.EndPoint;
                            if (!ResetFreeControlPoint && isAltDown)
                            {
                                ResetFreeControlPoint = true;
                            }
                            if (ResetFreeControlPoint && !isAltDown)
                            {
                                LastOppositeControlPointPosition = -newControlPoint;
                            }
                            var newPoint = new BezierPoint(newControlPoint, LastOppositeControlPointPosition, targetPoint.EndPoint, false, !ResetFreeControlPoint || isAltDown);
                            if (isBegin)
                            {
                                ViewModel.CurrentTimeRawValue = new BezierPath(newPoint, PrevValue.Points, PrevValue.IsClosed);
                            }
                            else
                            {
                                ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.SetItem(SelectedPointIndices[0], newPoint), PrevValue.IsClosed);
                            }
                        }
                        break;
                    case PointPosition.NextControlPoint:
                        {
                            var isBegin = SelectedPointIndices.Contains(BeginPointIndex);
                            var targetPoint = isBegin ? PrevValue.BeginPoint : PrevValue.Points[SelectedPointIndices[0]];
                            if (IsShiftKeyDown())
                            {
                                var lastMousePosition = coordTransformer.LocalCoordToScreenCoord((Vector3d)targetPoint.EndPoint);
                                var mouseDiff = Vector2d.Abs(mousePositionInPreview - lastMousePosition);
                                if (mouseDiff.X >= mouseDiff.Y)
                                {
                                    mousePositionInPreview = new Vector2d(mousePositionInPreview.X, lastMousePosition.Y);
                                }
                                else
                                {
                                    mousePositionInPreview = new Vector2d(lastMousePosition.X, mousePositionInPreview.Y);
                                }
                            }

                            var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            var diff = pos - ClickedPosition;
                            var newControlPoint = pos - targetPoint.EndPoint;
                            if (!ResetFreeControlPoint && isAltDown)
                            {
                                ResetFreeControlPoint = true;
                            }
                            if (ResetFreeControlPoint && !isAltDown)
                            {
                                LastOppositeControlPointPosition = -newControlPoint;
                            }
                            var newPoint = new BezierPoint(LastOppositeControlPointPosition, newControlPoint, targetPoint.EndPoint, false, !ResetFreeControlPoint || isAltDown);
                            if (isBegin)
                            {
                                ViewModel.CurrentTimeRawValue = new BezierPath(newPoint, PrevValue.Points, PrevValue.IsClosed);
                            }
                            else
                            {
                                ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.SetItem(SelectedPointIndices[0], newPoint), PrevValue.IsClosed);
                            }
                        }
                        break;
                }
            }
            else
            {
                if (IsShiftKeyDown())
                {
                    var lastMousePosition = coordTransformer.LocalCoordToScreenCoord((Vector3d)ClickedPosition);
                    var mouseDiff = Vector2d.Abs(mousePositionInPreview - lastMousePosition);
                    if (mouseDiff.X >= mouseDiff.Y)
                    {
                        mousePositionInPreview = new Vector2d(mousePositionInPreview.X, lastMousePosition.Y);
                    }
                    else
                    {
                        mousePositionInPreview = new Vector2d(lastMousePosition.X, mousePositionInPreview.Y);
                    }
                }

                var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                var diff = pos - ClickedPosition;
                if (!isAltDown)
                {
                    LastOppositeControlPointPosition = -diff;
                }
                CurrentCreatingPoint = IsMoved ? new BezierPoint(LastOppositeControlPointPosition, diff, ClickedPosition, false, isAltDown) : new BezierPoint(Vector2d.Zero, Vector2d.Zero, ClickedPosition, true, false);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsAltKeyDown()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }
    }

    enum PointPosition
    {
        None,
        EndPoint,
        PrevControlPoint,
        NextControlPoint
    }
}

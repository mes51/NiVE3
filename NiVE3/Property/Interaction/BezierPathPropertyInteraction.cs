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
using NiVE3.Util;

namespace NiVE3.Property.Interaction
{
    class BezierPathPropertyInteraction : PropertyInteractionBase
    {
        const int BeginPointIndex = -2;

        const double LineThickness = 1.0;

        const double PointHandleArea = 8.0;

        const double ControlPointCircleRadius = 2.0;

        const double MoveStartThreshold = 3.0;

        const double HandleLengthRatio = 0.3;

        static readonly Vector2d PointRadius = new Vector2d(2.0);

        static readonly Vector2d LargePointRadius = new Vector2d(4.0);

        static readonly Size PointSize = (Size)(PointRadius * 2.0);

        static readonly Size LargePointSize = (Size)(LargePointRadius * 2.0);

        static readonly DateTime OldestClickTime = new DateTime(0);

        public BezierPathPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        BezierPath? PrevValue { get; set; }

        EditMode EditMode { get; set; }

        bool IsMoved { get; set; }

        PointPosition ClickedPointPosition { get; set; }

        Vector2d ClickedPosition { get; set; }

        BezierPoint? CurrentCreatingPoint { get; set; }

        Vector2d LastOppositeControlPointPosition { get; set; }

        Vector2d FirstMousePositionInPreview { get; set; }

        bool ResetFreeControlPoint { get; set; }

        List<int> SelectedPointIndices { get; } = [];

        int LastClickPointIndexForDoubleClick { get; set; }

        DateTime LastClickTime { get; set; }

        // NOTE: 途中でダブルクリックの間隔が変更されても拾えるようにするため、インスタンス生成時に取得する
        int DoubleClickTime { get; set; } = (int)NativeMethods.GetDoubleClickTime();

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

                var selectedPoints = SelectedPointIndices.Where(i => i > -1 && i < value.Points.Length && !value.Points[i].IsLinear).Select(i => value.Points[i]);
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

                var (_, minDistance, _, _) = FindNearestPointInPath(value, (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview));
                if (minDistance <= PointHandleArea)
                {
                    return true;
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
            EditMode = EditMode.None;
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
            if (!SelectedPointIndices.Contains(LastClickPointIndexForDoubleClick))
            {
                LastClickPointIndexForDoubleClick = -1;
                LastClickTime = OldestClickTime;
            }

            // 制御点の編集
            if (TryEnterEditControlPoint(mousePositionInPreview, previewImageScale, coordTransformer, value))
            {
                return true;
            }

            // 始点の編集(パスが閉じている場合)
            if (TryEnterEditClosedPathBeginPoint(mousePositionInPreview, previewImageScale, coordTransformer, value))
            {
                return true;
            }

            // 各点の編集
            if (TryEnterEditPoint(mousePositionInPreview, previewImageScale, coordTransformer, value))
            {
                return true;
            }

            // 既存の点の編集ではない&閉じていないパスの場合は点の追加
            if (TryEnterEditOpenPath(mousePositionInPreview, previewImageScale, coordTransformer, value))
            {
                return true;
            }

            // 閉じているパスへの点の追加
            if (TryAddPoint(mousePositionInPreview, previewImageScale, coordTransformer, value))
            {
                return true;
            }

            return false;
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

            if (!IsMoved && EditMode != EditMode.NewPoint && EditMode != EditMode.NewBegin)
            {
                ViewModel.AbortEditCommand.Execute(null);
                ClickedPointPosition = PointPosition.None;
                IsInteracting = false;
                EditMode = EditMode.None;
                PrevValue = null;
                return;
            }

            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);

            ViewModel.EndEditCommand.Execute(null);
            ClickedPointPosition = PointPosition.None;
            IsInteracting = false;
            LastClickTime = OldestClickTime;
            EditMode = EditMode.None;
            PrevValue = null;
            IsMoved = false;
        }

        public override bool KeyDown(Key key, Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
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

            IsMoved = true;
            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);
            return true;
        }

        public override bool KeyUp(Key key, Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            if (!IsInteracting || PrevValue == null || key == Key.Delete)
            {
                return false;
            }

            IsMoved = true;
            UpdateCurrentRawValue(mousePositionInPreview, coordTransformer);
            return true;
        }

        public override void AbortInteraction()
        {
            ViewModel.AbortEditCommand.Execute(null);
            ClickedPointPosition = PointPosition.None;
            IsInteracting = false;
            LastClickTime = OldestClickTime;
            EditMode = EditMode.None;
            PrevValue = null;
            IsMoved = false;
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
                    if (point.IsLinear && lastPoint.IsLinear)
                    {
                        context.LineTo(screenEndPoint, true, true);
                    }
                    else
                    {
                        var baseNextControlPoint = (Vector3d)(lastPoint.EndPoint + (lastPoint.IsLinear ? Vector2d.Zero : lastPoint.NextControlPoint));
                        var basePrevControlPoint = (Vector3d)(point.EndPoint + (point.IsLinear ? Vector2d.Zero : point.PrevControlPoint));
                        var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord(baseNextControlPoint, globalTime) * previewImageScale + previewImagePosition);
                        var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord(basePrevControlPoint, globalTime) * previewImageScale + previewImagePosition);
                        context.BezierTo(prevControlPoint, nextControlPoint, screenEndPoint, true, true);
                    }
                    lastPoint = point;
                }

                if (value.IsClosed && (!value.BeginPoint.IsLinear || !lastPoint.IsLinear))
                {
                    var baseNextControlPoint = (Vector3d)(lastPoint.EndPoint + (lastPoint.IsLinear ? Vector2d.Zero : lastPoint.NextControlPoint));
                    var basePrevControlPoint = (Vector3d)(value.BeginPoint.EndPoint + (value.BeginPoint.IsLinear ? Vector2d.Zero : value.BeginPoint.PrevControlPoint));
                    var screenEndPoint = (Point)(coordTransformer.LocalCoordToScreenCoord((Vector3d)value.BeginPoint.EndPoint, globalTime) * previewImageScale + previewImagePosition);
                    var prevControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord(baseNextControlPoint, globalTime) * previewImageScale + previewImagePosition);
                    var nextControlPoint = (Point)(coordTransformer.LocalCoordToScreenCoord(basePrevControlPoint, globalTime) * previewImageScale + previewImagePosition);
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
            if (EditMode == EditMode.EditPoint)
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

                            ViewModel.CurrentTimeRawValue = new BezierPath(newBeginPoint, newPoints, PrevValue.IsClosed);
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
                if (EditMode == EditMode.NewBegin)
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(CurrentCreatingPoint, [], false);
                }
                else
                {
                    ViewModel.CurrentTimeRawValue = new BezierPath(PrevValue.BeginPoint, PrevValue.Points.Append(CurrentCreatingPoint), false);
                }
            }
        }

        bool TryEnterEditControlPoint(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer, BezierPath path)
        {
            var hitArea = PointHandleArea / previewImageScale;

            var selectedPoints = SelectedPointIndices.Where(i => i > -1 && !path.Points[i].IsLinear).Select(i => (i, path.Points[i]));
            if (SelectedPointIndices.Contains(BeginPointIndex) && !path.BeginPoint.IsLinear)
            {
                selectedPoints = selectedPoints.Prepend((BeginPointIndex, path.BeginPoint));
            }
            foreach (var (i, p) in selectedPoints)
            {
                if (IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.PrevControlPoint, coordTransformer))
                {
                    ClickedPointPosition = PointPosition.PrevControlPoint;
                    ResetFreeControlPoint = !p.IsFreeControlPoint;
                    LastOppositeControlPointPosition = p.NextControlPoint;
                }
                else if (IsHit(mousePositionInPreview, hitArea, p.EndPoint + p.NextControlPoint, coordTransformer))
                {
                    ClickedPointPosition = PointPosition.NextControlPoint;
                    ResetFreeControlPoint = !p.IsFreeControlPoint;
                    LastOppositeControlPointPosition = p.PrevControlPoint;
                }

                if (ClickedPointPosition != PointPosition.None)
                {
                    SelectedPointIndices.Clear();
                    SelectedPointIndices.Add(i);
                    LastClickPointIndexForDoubleClick = -1; // NOTE: 制御点の編集はダブルクリック扱いはしない
                    LastClickTime = DateTime.Now;
                    PrevValue = path;
                    IsInteracting = true;
                    EditMode = EditMode.EditPoint;
                    ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.BeginEditCommand.Execute(null);
                    return true;
                }
            }

            return false;
        }

        bool TryEnterEditClosedPathBeginPoint(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer, BezierPath path)
        {
            var hitArea = PointHandleArea / previewImageScale;

            if (path.IsClosed && IsHit(mousePositionInPreview, hitArea, path.BeginPoint.EndPoint, coordTransformer))
            {
                if (IsShiftKeyDown())
                {
#pragma warning disable CA1868 // NOTE: 要素の削除や追加のみでは無いため無視する
                    if (SelectedPointIndices.Contains(BeginPointIndex))
#pragma warning restore CA1868
                    {
                        SelectedPointIndices.Remove(BeginPointIndex);
                        if (LastClickPointIndexForDoubleClick == BeginPointIndex)
                        {
                            LastClickPointIndexForDoubleClick = -1;
                        }
                        return false;
                    }
                    else
                    {
                        SelectedPointIndices.Add(BeginPointIndex);
                        SelectedPointIndices.Sort();
                        LastClickPointIndexForDoubleClick = BeginPointIndex;
                        LastClickTime = DateTime.Now;
                        PrevValue = path;
                        IsInteracting = true;
                        EditMode = EditMode.EditPoint;
                        ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                        ClickedPointPosition = PointPosition.EndPoint;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }
                else
                {
                    if (SelectedPointIndices.Contains(BeginPointIndex) && LastClickPointIndexForDoubleClick == BeginPointIndex && (DateTime.Now - LastClickTime).Milliseconds <= DoubleClickTime)
                    {
                        ViewModel.BeginEditCommand.Execute(null);
                        ViewModel.CurrentTimeRawValue = TogglePointIsLinear(path, LastClickPointIndexForDoubleClick);
                        ViewModel.EndEditCommand.Execute(null);
                        LastClickPointIndexForDoubleClick = -1;
                        LastClickTime = OldestClickTime;
                        return true;
                    }
                    else
                    {
                        if (!SelectedPointIndices.Contains(BeginPointIndex))
                        {
                            SelectedPointIndices.Clear();
                            SelectedPointIndices.Add(BeginPointIndex);
                        }
                        LastClickPointIndexForDoubleClick = BeginPointIndex;
                        LastClickTime = DateTime.Now;
                        PrevValue = path;
                        IsInteracting = true;
                        EditMode = EditMode.EditPoint;
                        ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                        ClickedPointPosition = PointPosition.EndPoint;
                        ViewModel.BeginEditCommand.Execute(null);
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        bool TryEnterEditPoint(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer, BezierPath path)
        {
            var hitArea = PointHandleArea / previewImageScale;

            for (var i = 0; i < path.Points.Length; i++)
            {
                var p = path.Points[i];
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
                            if (LastClickPointIndexForDoubleClick == i)
                            {
                                LastClickPointIndexForDoubleClick = -1;
                            }
                            return false;
                        }
                        else
                        {
                            SelectedPointIndices.Add(i);
                            SelectedPointIndices.Sort();
                            LastClickPointIndexForDoubleClick = BeginPointIndex;
                            LastClickTime = DateTime.Now;
                            PrevValue = path;
                            IsInteracting = true;
                            EditMode = EditMode.EditPoint;
                            ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            ClickedPointPosition = PointPosition.EndPoint;
                            ViewModel.BeginEditCommand.Execute(null);
                            return true;
                        }
                    }
                    else
                    {
                        if (SelectedPointIndices.Contains(i) && LastClickPointIndexForDoubleClick == i && (DateTime.Now - LastClickTime).Milliseconds <= DoubleClickTime)
                        {
                            ViewModel.BeginEditCommand.Execute(null);
                            ViewModel.CurrentTimeRawValue = TogglePointIsLinear(path, LastClickPointIndexForDoubleClick);
                            ViewModel.EndEditCommand.Execute(null);
                            LastClickPointIndexForDoubleClick = -1;
                            LastClickTime = OldestClickTime;
                            return true;
                        }
                        else
                        {
                            if (!SelectedPointIndices.Contains(i))
                            {
                                SelectedPointIndices.Clear();
                                SelectedPointIndices.Add(i);
                            }
                            LastClickPointIndexForDoubleClick = i;
                            LastClickTime = DateTime.Now;
                            PrevValue = path;
                            IsInteracting = true;
                            EditMode = EditMode.EditPoint;
                            ClickedPosition = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                            ClickedPointPosition = PointPosition.EndPoint;
                            ViewModel.BeginEditCommand.Execute(null);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool TryEnterEditOpenPath(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer, BezierPath path)
        {
            if (!path.IsClosed)
            {
                var hitArea = PointHandleArea / previewImageScale;

                SelectedPointIndices.Clear();
                if (path.IsInvalid())
                {
                    PrevValue = path;
                    EditMode = EditMode.NewBegin;
                    IsInteracting = true;
                    ClickedPointPosition = PointPosition.EndPoint;
                    SelectedPointIndices.Add(BeginPointIndex);
                    LastClickPointIndexForDoubleClick = BeginPointIndex;
                    LastClickTime = DateTime.Now;
                    ViewModel.BeginEditCommand.Execute(null);

                    var pos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                    ViewModel.CurrentTimeRawValue = new BezierPath(pos, [], false);
                    ClickedPosition = pos;
                }
                else if (IsHit(mousePositionInPreview, hitArea, path.BeginPoint.EndPoint, coordTransformer) && path.Points.Length > 0)
                {
                    SelectedPointIndices.Add(BeginPointIndex);
                    LastClickPointIndexForDoubleClick = BeginPointIndex;
                    LastClickTime = DateTime.Now;

                    ViewModel.BeginEditCommand.Execute(null);
                    ViewModel.CurrentTimeRawValue = path.ClosePath();
                    ViewModel.EndEditCommand.Execute(null);
                }
                else
                {
                    PrevValue = path;
                    IsInteracting = true;
                    EditMode = EditMode.NewPoint;
                    ClickedPointPosition = PointPosition.EndPoint;
                    SelectedPointIndices.Add(path.Points.Length);
                    LastClickPointIndexForDoubleClick = path.Points.Length;
                    LastClickTime = DateTime.Now;
                    ViewModel.BeginEditCommand.Execute(null);

                    if (IsShiftKeyDown())
                    {
                        var lastPoint = path.Points.Length > 0 ? path.Points[^1] : path.BeginPoint;
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
            else
            {
                return false;
            }
        }

        bool TryAddPoint(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer, BezierPath path)
        {
            if (path.IsClosed)
            {
                var localPos = (Vector2d)coordTransformer.ScreenCoordToLocalCoord(mousePositionInPreview);
                var (nearestPoint, minDistance, nearestT, pointIndex) = FindNearestPointInPath(path, localPos);

                if (minDistance < PointHandleArea)
                {
                    SelectedPointIndices.Clear();
                    ViewModel.BeginEditCommand.Execute(null);

                    var nextIndex = pointIndex == BeginPointIndex ? 0 : (pointIndex == path.Points.Length - 1 ? BeginPointIndex : pointIndex + 1);
                    var prevPoint = pointIndex == BeginPointIndex ? path.BeginPoint : path.Points[pointIndex];
                    var nextPoint = nextIndex == BeginPointIndex ? path.BeginPoint : path.Points[nextIndex];
                    var selectIndex = nextIndex;
                    if (!prevPoint.IsLinear || !nextPoint.IsLinear)
                    {
                        var prevControlPoint = prevPoint.EndPoint + (prevPoint.IsLinear ? Vector2d.Zero : prevPoint.NextControlPoint);
                        var nextControlPoint = nextPoint.EndPoint + (nextPoint.IsLinear ? Vector2d.Zero : nextPoint.PrevControlPoint);
                        var p01 = Vector2d.Lerp(prevPoint.EndPoint, prevControlPoint, nearestT);
                        var p12 = Vector2d.Lerp(prevControlPoint, nextControlPoint, nearestT);
                        var p23 = Vector2d.Lerp(nextControlPoint, nextPoint.EndPoint, nearestT);

                        var leftHandle = Vector2d.Lerp(p01, p12, nearestT);
                        var rightHandle = Vector2d.Lerp(p12, p23, nearestT);
                        var newPoint = new BezierPoint(leftHandle - nearestPoint, rightHandle - nearestPoint, nearestPoint, false, true);
                        var newPrevPoint = new BezierPoint(prevPoint.PrevControlPoint, p01 - prevPoint.EndPoint, prevPoint.EndPoint, prevPoint.IsLinear, true);
                        var newNextPoint = new BezierPoint(p23 - nextPoint.EndPoint, nextPoint.NextControlPoint, nextPoint.EndPoint, nextPoint.IsLinear, true);

                        var newPath = (pointIndex, nextIndex) switch
                        {
                            (BeginPointIndex, _) => new BezierPath(newPrevPoint, path.Points.SetItem(nextIndex, newNextPoint).Insert(nextIndex, newPoint), path.IsClosed),
                            (_, BeginPointIndex) => new BezierPath(newNextPoint, path.Points.SetItem(pointIndex, newPrevPoint).Insert(path.Points.Length, newPoint), path.IsClosed),
                            (_, _) => new BezierPath(path.BeginPoint, path.Points.SetItem(pointIndex, newPrevPoint).SetItem(nextIndex, newNextPoint).Insert(nextIndex, newPoint), path.IsClosed)
                        };
                        if (nextIndex == BeginPointIndex)
                        {
                            selectIndex = newPath.Points.Length - 1;
                        }
                        ViewModel.CurrentTimeRawValue = newPath;
                    }
                    else
                    {
                        var newPoint = new BezierPoint(Vector2d.Zero, Vector2d.Zero, nearestPoint, true, false);
                        if (pointIndex == BeginPointIndex)
                        {
                            ViewModel.CurrentTimeRawValue = new BezierPath(path.BeginPoint, path.Points.Prepend(newPoint), path.IsClosed);
                        }
                        else
                        {
                            ViewModel.CurrentTimeRawValue = new BezierPath(path.BeginPoint, path.Points.Insert(pointIndex + 1, newPoint), path.IsClosed);
                        }
                    }
                    SelectedPointIndices.Add(selectIndex);
                    ViewModel.EndEditCommand.Execute(null);
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Vector2d nearestPoint, double minDistance, double nearestT, int pointIndex) FindNearestPointInPath(BezierPath path, Vector2d targetPos)
        {
            var minDistance = double.MaxValue;
            var nearestPoint = Vector2d.Zero;
            var nearestT = 0.0;
            var pointIndex = -1;
            foreach (var (p, n, i) in path.Points.Prepend(path.BeginPoint).Zip(path.Points.Append(path.BeginPoint), Enumerable.Range(-1, path.Points.Length + 1)))
            {
                var (np, t) = SegmentUtil.FindNearestPointInSegment(p, n, targetPos);
                var distance = Vector2d.DistanceSquared(targetPos, np);
                if (minDistance > distance)
                {
                    minDistance = distance;
                    nearestPoint = np;
                    nearestT = t;
                    if (i == -1)
                    {
                        pointIndex = BeginPointIndex;
                    }
                    else
                    {
                        pointIndex = i;
                    }
                }
            }

            return (nearestPoint, Math.Sqrt(minDistance), nearestT, pointIndex);
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

        static BezierPath TogglePointIsLinear(BezierPath path, int pointIndex)
        {
            if (pointIndex == -1 || pointIndex >= path.Points.Length)
            {
                // bug
                throw new ArgumentException("point index is invalid", nameof(pointIndex));
            }

            var point = pointIndex == BeginPointIndex ? path.BeginPoint : path.Points[pointIndex];
            if (point.IsLinear)
            {
                var prevPoint = (path.IsClosed, pointIndex) switch
                {
                    (_, BeginPointIndex) when path.Points.Length < 1 => path.BeginPoint,
                    (_, _) when pointIndex > 0 => path.Points[pointIndex - 1],
                    (_, 0) => path.BeginPoint,
                    (true, BeginPointIndex) => path.Points[^1],

                    (_, _) => path.BeginPoint
                };
                var nextPoint = (path.IsClosed, pointIndex) switch
                {
                    (_, BeginPointIndex) when path.Points.Length > 0 => path.Points[0],
                    (_, BeginPointIndex) when path.Points.Length < 1 => path.BeginPoint,
                    (_, _) when pointIndex < path.Points.Length - 1 => path.Points[pointIndex + 1],
                    (false, _) when path.Points.Length > 0 => path.Points[^1],

                    (_, _) => path.BeginPoint,
                };

                var diff = nextPoint.EndPoint - prevPoint.EndPoint;
                var diffLength = diff.Length();
                if (diffLength <= 0.0)
                {
                    point = new BezierPoint(Vector2d.Zero, Vector2d.Zero, point.EndPoint, false, false);
                }
                else
                {
                    var dir = diff / diffLength;

                    var newHandleLength = 0.0;
                    if (prevPoint == point)
                    {
                        newHandleLength = Vector2d.Distance(point.EndPoint, nextPoint.EndPoint) * HandleLengthRatio;
                    }
                    else if (nextPoint == point)
                    {
                        newHandleLength = Vector2d.Distance(point.EndPoint, prevPoint.EndPoint) * HandleLengthRatio;
                    }
                    else
                    {
                        newHandleLength = Math.Min(Vector2d.Distance(point.EndPoint, prevPoint.EndPoint), Vector2d.Distance(point.EndPoint, nextPoint.EndPoint)) * HandleLengthRatio;
                    }
                    point = new BezierPoint(-dir * newHandleLength, dir * newHandleLength, point.EndPoint, false, false);
                }
            }
            else
            {
                point = new BezierPoint(Vector2d.Zero, Vector2d.Zero, point.EndPoint, true, false);
            }

            if (pointIndex == BeginPointIndex)
            {
                return new BezierPath(point, path.Points, path.IsClosed);
            }
            else
            {
                return new BezierPath(path.BeginPoint, path.Points.SetItem(pointIndex, point), path.IsClosed);
            }
        }
    }

    enum PointPosition
    {
        None,
        EndPoint,
        PrevControlPoint,
        NextControlPoint
    }

    enum EditMode
    {
        None,
        EditPoint,
        NewBegin,
        NewPoint
    }

    static class SegmentUtil
    {
        public static (Vector2d point, double t) FindNearestPointInSegment(BezierPoint firstPoint, BezierPoint secondPoint, in Vector2d targetPoint)
        {
            var prevControlPoint = firstPoint.EndPoint + (firstPoint.IsLinear ? Vector2d.Zero : firstPoint.NextControlPoint);
            var nextControlPoint = secondPoint.EndPoint + (secondPoint.IsLinear ? Vector2d.Zero : secondPoint.PrevControlPoint);
            var t = FindT(firstPoint.EndPoint, secondPoint.EndPoint, prevControlPoint, nextControlPoint, targetPoint);

            return (CalcBezierPosition(firstPoint.EndPoint, secondPoint.EndPoint, prevControlPoint, nextControlPoint, t), t);
        }

        static double FindT(in Vector2d a, in Vector2d b, in Vector2d c1, in Vector2d c2, in Vector2d targetPoint)
        {
            const int SegmentCount = 10;
            const int IterationCount = 20;

            var bestT = 0.0;
            var minDistance = double.MaxValue;

            for (var i = 0; i < SegmentCount; i++)
            {
                var t = (double)i / SegmentCount;
                var posInBezier = CalcBezierPosition(a, b, c1, c2, t);
                var distance = Vector2d.DistanceSquared(targetPoint, posInBezier);

                if (minDistance > distance)
                {
                    bestT = t;
                    minDistance = distance;
                }
            }

            var step = 0.5 / SegmentCount;
            for (var i = 0; i < IterationCount; i++, step *= 0.5)
            {
                var leftT = Math.Max(bestT - step, 0.0);
                var rightT = Math.Min(bestT + step, 1.0);

                var left = CalcBezierPosition(a, b, c1, c2, leftT);
                var right = CalcBezierPosition(a, b, c1, c2, rightT);

                var leftDistance = Vector2d.DistanceSquared(targetPoint, left);
                var rightDistance = Vector2d.DistanceSquared(targetPoint, right);

                if (minDistance > leftDistance)
                {
                    minDistance = leftDistance;
                    bestT = leftT;
                }
                if (minDistance > rightDistance)
                {
                    minDistance = rightDistance;
                    bestT = rightT;
                }
            }

            return bestT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2d CalcBezierPosition(in Vector2d a, in Vector2d b, in Vector2d c1, in Vector2d c2, double t)
        {
            var u = 1.0 - t;

            return (a * u * u * u) + (3.0 * u * u * t * c1) + (3.0 * u * t * t * c2) + (t * t * t * b);
        }
    }
}

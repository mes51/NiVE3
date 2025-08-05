using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Config;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel.Visualize
{
    static class LayerTranslateVisualizer
    {
        const double PreviewPropertyPointSize = 2.0;

        public static void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, LayerViewModel layer, Time globalTime, double frameRate, Color tagColor, ICoordTransformerObject coordTransformer)
        {
            var previewFrameRange = ApplicationSetting.Setting.DisplayFrameRangePropertyInPreview;
            if (previewFrameRange < 1)
            {
                return;
            }

            var anchorPoint = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformAnchorPointId) as PropertyViewModel;
            if (anchorPoint == null)
            {
                return;
            }

            var points = new List<Point>(previewFrameRange * 2 + 1);
            for (var i = 0; i < previewFrameRange; i++)
            {
                var time = globalTime - new Time(previewFrameRange - i - 1, frameRate);
                var value = (Vector3d)(anchorPoint.GetValue(time - anchorPoint.SourceStartPoint, time) ?? Vector3d.Zero);
                var screenPos = coordTransformer.LocalCoordToScreenCoord(value, time);
                points.Add(new Point(screenPos.X * previewImageScale.X + previewImagePosition.X, screenPos.Y * previewImageScale.Y + previewImagePosition.Y));
            }

            var currentValue = (Vector3d)(anchorPoint.CurrentTimeValue ?? Vector3d.Zero);
            var currentScreenPos = coordTransformer.LocalCoordToScreenCoord(currentValue);
            points.Add(new Point(currentScreenPos.X * previewImageScale.X + previewImagePosition.X, currentScreenPos.Y * previewImageScale.Y + previewImagePosition.Y));

            for (var i = 0; i < previewFrameRange; i++)
            {
                var time = globalTime + new Time(i + 1, frameRate);
                var value = (Vector3d)(anchorPoint.GetValue(time - anchorPoint.SourceStartPoint, time) ?? Vector3d.Zero);
                var screenPos = coordTransformer.LocalCoordToScreenCoord(value, time);
                points.Add(new Point(screenPos.X * previewImageScale.X + previewImagePosition.X, screenPos.Y * previewImageScale.Y + previewImagePosition.Y));
            }

            var brush = new SolidColorBrush(tagColor);
            var pen = new Pen(brush, 1.0);

            if (points.Count > 1)
            {
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
        }
    }
}

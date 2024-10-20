using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Acornima;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NiVE3.Extension;

namespace NiVE3.View.Primitive.Editor
{
    // https://stackoverflow.com/a/12639677
    class ErrorDisplayService : IBackgroundRenderer, IVisualLineTransformer
    {
        public KnownLayer Layer => KnownLayer.Selection;

        public bool HasErrorMaker => Errors.Count > 0;

        TextSegmentCollection<ErrorMarker> Errors { get; }

        Pen LinePen { get; } = new Pen(Brushes.Red, 1.0).FreezeCurrentObject();

        public ErrorDisplayService(TextDocument document)
        {
            Errors = new TextSegmentCollection<ErrorMarker>(document);
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            const double WaveInterval = 2.5;
            const double WaveAmplitude = 2.5;

            if (!textView.VisualLinesValid || textView.VisualLines.Count < 1)
            {
                return;
            }

            var viewBegin = textView.VisualLines.First().FirstDocumentLine.Offset;
            var viewEnd = textView.VisualLines.Last().LastDocumentLine.Offset;
            foreach (var marker in Errors)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    var beginPoint = rect.BottomLeft;
                    var endPoint = rect.BottomRight;

                    var pointCount = (int)Math.Ceiling((endPoint.X - beginPoint.X) / WaveInterval);

                    var geometry = new StreamGeometry();
                    using (var context = geometry.Open())
                    {
                        context.BeginFigure(beginPoint, false, false);

                        for (var i = 1; i < pointCount; i += 4)
                        {
                            context.QuadraticBezierTo(new Point(beginPoint.X + i * WaveInterval, beginPoint.Y + WaveAmplitude), new Point(beginPoint.X + (i + 1) * WaveInterval, beginPoint.Y), true, true);
                            context.QuadraticBezierTo(new Point(beginPoint.X + (i + 2) * WaveInterval, beginPoint.Y - WaveAmplitude), new Point(beginPoint.X + (i + 3) * WaveInterval, beginPoint.Y), true, true);
                        }
                    }

                    rect.Inflate(0.0, WaveAmplitude);
                    rect.Offset(0.0, WaveAmplitude * 0.5);
                    drawingContext.PushClip(new RectangleGeometry(rect));
                    drawingContext.DrawGeometry(null, LinePen, geometry.FreezeCurrentObject());
                    drawingContext.Pop();
                }
            }
        }

        public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements) { }

        public void SetError(string errorMessage, int startOffset, int length)
        {
            Clear();
            if (string.IsNullOrEmpty(errorMessage) || length < 1)
            {
                return;
            }

            Errors.Add(new ErrorMarker(startOffset, length, errorMessage));
        }

        public void Clear()
        {
            Errors.Clear();
        }

        public ErrorMarker? GetMarker(int offset)
        {
            return Errors.FindSegmentsContaining(offset).FirstOrDefault();
        }
    }

    class ErrorMarker : TextSegment
    {
        public string ErrorMessage { get; }

        public ErrorMarker(int startOffset, int length, string errorMessage)
        {
            StartOffset = startOffset;
            Length = length;
            ErrorMessage = errorMessage;
        }
    }
}

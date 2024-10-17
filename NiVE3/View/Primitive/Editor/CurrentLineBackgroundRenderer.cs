using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit;
using System.Windows.Media;
using System.Windows;
using NiVE3.Extension;

namespace NiVE3.View.Primitive.Editor
{
    class CurrentLineBackgroundRenderer : IBackgroundRenderer
    {
        double strokeWidth = 1.0;
        public double StrokeWidth
        {
            get => strokeWidth;
            set
            {
                if (strokeWidth != value)
                {
                    strokeWidth = value;
                    IsDirty = true;
                }
            }
        }

        Color currentLineColor = Colors.LightGray;
        public Color CurrentLineColor
        {
            get => currentLineColor;
            set
            {
                if (currentLineColor != value)
                {
                    currentLineColor = value;
                    IsDirty = true;
                }
            }
        }


        Pen Pen { get; set; } = new Pen(new SolidColorBrush(Colors.LightGray), 1.0).FreezeCurrentObject();

        bool IsDirty { get; set; }

        public KnownLayer Layer => KnownLayer.Background;

        TextEditor Editor { get; }

        public CurrentLineBackgroundRenderer(TextEditor editor)
        {
            Editor = editor;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (Editor.Document == null)
            {
                return;
            }

            if (IsDirty)
            {
                Pen = new Pen(new SolidColorBrush(CurrentLineColor), StrokeWidth).FreezeCurrentObject();
                IsDirty = false;
            }

            textView.EnsureVisualLines();
            var currentLine = Editor.Document.GetLineByOffset(Editor.CaretOffset);

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
            {
                drawingContext.DrawRectangle(Brushes.Transparent, Pen, new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height)));
            }
        }
    }
}

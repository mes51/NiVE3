using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// WPF の低レベルテキスト整形 API (TextFormatter) を使った ITextLayout の既定実装。
    /// TextBox と同じ整形エンジンなので、サロゲートペア・結合文字・カーニングを含めて
    /// 正確なヒットテストとキャレット位置が得られる。
    /// 現状は折り返しなし (ドキュメントの 1 行 = 視覚行 1 行)。
    /// </summary>
    sealed class FormatterTextLayout : ITextLayout
    {
        /// <summary>
        /// 折り返しなしのため十分大きな段落幅を渡す (TextFormatter の上限未満の値)
        /// </summary>
        const double MaxParagraphWidth = 1_000_000;

        [field: ThreadStatic]
        static TextFormatter? Formatter { get; set; }

        sealed class LineInfo
        {
            public int Start { get; set; }

            /// <summary>
            /// 末尾の '\n' を含まない
            /// </summary>
            public int Length { get; set; }

            public double Top { get; set; }

            public required TextLine Line { get; set; }
        }

        double FontSize { get; }

        List<LineInfo> Lines { get; } = [];

        public double Width { get; }

        public double Height { get; }

        public Size Extent => new(Width, Height);

        public bool DrawsText => true;

        public int LineCount => Lines.Count;

        public FormatterTextLayout(string text, Typeface typeface, double fontSize, Brush foreground, double pixelsPerDip)
        {
            FontSize = fontSize;

            var runProperties = new SimpleTextRunProperties(typeface, fontSize, foreground, pixelsPerDip);
            var paragraphProperties = new SimpleTextParagraphProperties(runProperties);
            var source = new SimpleTextSource(text, runProperties, pixelsPerDip);
            var formatter = Formatter ??= TextFormatter.Create(TextFormattingMode.Display);

            var y = 0.0;
            var maxWidth = 0.0;
            var position = 0;
            while (true)
            {
                var newlineIndex = text.IndexOf('\n', position);
                var lineLength = (newlineIndex < 0 ? text.Length : newlineIndex) - position;

                var line = formatter.FormatLine(source, position, MaxParagraphWidth, paragraphProperties, null);
                Lines.Add(new LineInfo { Start = position, Length = lineLength, Top = y, Line = line });

                y += line.Height;
                maxWidth = Math.Max(maxWidth, line.WidthIncludingTrailingWhitespace);

                if (newlineIndex < 0)
                {
                    break;
                }
                position = newlineIndex + 1;
            }

            Width = maxWidth;
            Height = y;
        }

        public void Dispose()
        {
            foreach (var lineInfo in Lines)
            {
                lineInfo.Line.Dispose();
            }
            Lines.Clear();
        }

        public int GetLineIndexFromOffset(int offset)
        {
            var low = 0;
            var high = Lines.Count - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (Lines[middle].Start <= offset)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            return low;
        }

        public (int Start, int Length) GetLineRange(int lineIndex)
        {
            var lineInfo = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
            return (lineInfo.Start, lineInfo.Length);
        }

        public (Point Top, Point Bottom) GetCaretLine(int offset)
        {
            var rect = GetCaretRect(offset);
            return (rect.TopLeft, rect.BottomLeft);
        }

        public double GetCaretFlowPosition(int offset)
        {
            return GetCaretRect(offset).X;
        }

        public bool TryGetCaretLocalFrame(int offset, out Point localPosition, out double localHeight, out Matrix transform)
        {
            var rect = GetCaretRect(offset);
            localPosition = rect.TopLeft;
            localHeight = rect.Height;
            transform = Matrix.Identity;
            return true;
        }

        Rect GetCaretRect(int offset)
        {
            var lineInfo = Lines[GetLineIndexFromOffset(offset)];
            offset = Math.Clamp(offset, lineInfo.Start, lineInfo.Start + lineInfo.Length);

            var x = offset >= lineInfo.Start + lineInfo.Length
                ? lineInfo.Line.WidthIncludingTrailingWhitespace
                : lineInfo.Line.GetDistanceFromCharacterHit(new CharacterHit(offset, 0));

            return new Rect(x, lineInfo.Top, 1, lineInfo.Line.Height);
        }

        public int GetOffsetAt(Point point)
        {
            return GetOffsetAtFlowPosition(GetLineIndexFromY(point.Y), point.X);
        }

        public int GetOffsetAtFlowPosition(int lineIndex, double flowPosition)
        {
            var lineInfo = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
            if (flowPosition <= 0)
            {
                return lineInfo.Start;
            }
            if (flowPosition >= lineInfo.Line.WidthIncludingTrailingWhitespace)
            {
                return lineInfo.Start + lineInfo.Length;
            }

            var hit = lineInfo.Line.GetCharacterHitFromDistance(flowPosition);
            var offset = hit.FirstCharacterIndex + hit.TrailingLength;
            return Math.Clamp(offset, lineInfo.Start, lineInfo.Start + lineInfo.Length);
        }

        public IReadOnlyList<TextQuad> GetRangeQuads(int start, int end)
        {
            var quads = new List<TextQuad>();
            foreach (var rect in GetRangeRects(start, end))
            {
                quads.Add(new TextQuad(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft));
            }
            return quads;
        }

        public IReadOnlyList<TextQuad> GetBoundingQuads()
        {
            return Array.Empty<TextQuad>();
        }

        List<Rect> GetRangeRects(int start, int end)
        {
            var rects = new List<Rect>();
            if (end <= start)
            {
                return rects;
            }

            // 選択範囲に改行が含まれる場合に行末へ描く小さなマーカーの幅
            var newlineMarkerWidth = FontSize * 0.35;

            foreach (var lineInfo in Lines)
            {
                var lineEnd = lineInfo.Start + lineInfo.Length;
                if (lineEnd < start)
                {
                    continue;
                }
                if (lineInfo.Start > end)
                {
                    break;
                }

                var rangeStart = Math.Max(start, lineInfo.Start);
                var rangeEnd = Math.Min(end, lineEnd);
                if (rangeEnd > rangeStart)
                {
                    foreach (var bounds in lineInfo.Line.GetTextBounds(rangeStart, rangeEnd - rangeStart))
                    {
                        var rect = bounds.Rectangle;
                        rects.Add(new Rect(rect.X, lineInfo.Top + rect.Y, rect.Width, rect.Height));
                    }
                }

                // 選択が行末を越えて次の行へ続く場合、改行分のマーカーを描く
                if (end > lineEnd && start <= lineEnd)
                {
                    rects.Add(new Rect(lineInfo.Line.WidthIncludingTrailingWhitespace, lineInfo.Top, newlineMarkerWidth, lineInfo.Line.Height));
                }
            }
            return rects;
        }

        public void DrawText(DrawingContext drawingContext, Point origin)
        {
            foreach (var lineInfo in Lines)
            {
                lineInfo.Line.Draw(drawingContext, new Point(origin.X, origin.Y + lineInfo.Top), InvertAxes.None);
            }
        }

        int GetLineIndexFromY(double y)
        {
            if (y < 0)
            {
                return 0;
            }
            for (var i = 0; i < Lines.Count; i++)
            {
                if (y < Lines[i].Top + Lines[i].Line.Height)
                {
                    return i;
                }
            }
            return Lines.Count - 1;
        }

        /// <summary>
        /// TextFormatter にテキストを供給するソース。'\n' を段落終端として扱う。
        /// </summary>
        sealed class SimpleTextSource : TextSource
        {
            string Text { get; }

            TextRunProperties Properties { get; }

            public SimpleTextSource(string text, TextRunProperties properties, double pixelsPerDip)
            {
                Text = text;
                Properties = properties;
                PixelsPerDip = pixelsPerDip;
            }

            public override TextRun GetTextRun(int textSourceCharacterIndex)
            {
                if (textSourceCharacterIndex >= Text.Length)
                {
                    return new TextEndOfParagraph(1, Properties);
                }
                if (Text[textSourceCharacterIndex] == '\n')
                {
                    return new TextEndOfParagraph(1, Properties);
                }

                var end = Text.IndexOf('\n', textSourceCharacterIndex);
                if (end < 0)
                {
                    end = Text.Length;
                }
                return new TextCharacters(Text, textSourceCharacterIndex, end - textSourceCharacterIndex, Properties);
            }

            public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
            {
                return new TextSpan<CultureSpecificCharacterBufferRange>(
                    0,
                    new CultureSpecificCharacterBufferRange(
                        CultureInfo.CurrentUICulture,
                        new CharacterBufferRange("", 0, 0)));
            }

            public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
            {
                return textSourceCharacterIndex;
            }
        }

        sealed class SimpleTextRunProperties : TextRunProperties
        {
            Typeface RunTypeface { get; }

            double FontSize { get; }

            Brush Foreground { get; }

            public SimpleTextRunProperties(Typeface typeface, double fontSize, Brush foreground, double pixelsPerDip)
            {
                RunTypeface = typeface;
                FontSize = fontSize;
                Foreground = foreground;
                PixelsPerDip = pixelsPerDip;
            }

            public override Typeface Typeface => RunTypeface;

            public override double FontRenderingEmSize => FontSize;

            public override double FontHintingEmSize => FontSize;

            public override TextDecorationCollection? TextDecorations => null;

            public override Brush ForegroundBrush => Foreground;

            public override Brush? BackgroundBrush => null;

            public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;

            public override TextEffectCollection? TextEffects => null;
        }

        sealed class SimpleTextParagraphProperties : TextParagraphProperties
        {
            TextRunProperties DefaultProperties { get; }

            public SimpleTextParagraphProperties(TextRunProperties defaultProperties)
            {
                DefaultProperties = defaultProperties;
            }

            public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

            public override TextAlignment TextAlignment => TextAlignment.Left;

            /// <summary>
            /// 0 = 自動
            /// </summary>
            public override double LineHeight => 0;

            public override bool FirstLineInParagraph => true;

            public override TextRunProperties DefaultTextRunProperties => DefaultProperties;

            public override TextWrapping TextWrapping => TextWrapping.NoWrap;

            public override TextMarkerProperties? TextMarkerProperties => null;

            public override double Indent => 0;
        }
    }
}
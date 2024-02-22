using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Text
{
    class StructuredExtendedTextRun
    {
        public string SourceText { get; }

        public int TotalElementCount { get; }

        TextStyle DefaultStyle { get; }

        LineExtendedTextRun[] Lines { get; }

        public StructuredExtendedTextRun(string sourceText, TextStyle defaultStyle, TextStyleRun[] styles)
        {
            SourceText = sourceText;
            DefaultStyle = defaultStyle;
            TotalElementCount = new StringInfo(sourceText).LengthInTextElements;

            var count = 0;
            var lines = new List<LineExtendedTextRun>();
            foreach (var line in sourceText.Replace("\r", "").Split("\n"))
            {
                var stringInfo = new StringInfo(line);
                var lineElementCount = stringInfo.LengthInTextElements;

                var targetStyles = styles.SkipWhile(s => s.Start < count).TakeWhile(s => s.End > count + lineElementCount).ToArray();
                lines.Add(new LineExtendedTextRun(line, count, defaultStyle, targetStyles));

                count += lineElementCount + 1;
            }

            Lines = lines.ToArray();
        }

        public ExtendedTextRun[] Flatten()
        {
            var currentTextRun = new TextStyleRun(0, TotalElementCount, DefaultStyle).ToTextRun();
            var result = new List<ExtendedTextRun>();
            var start = 0;
            var end = 0;

            foreach (var line in Lines)
            {
                foreach (var textRun in line.TextRuns)
                {
                    end++;
                    if (!currentTextRun.EqualsWithoutRun(textRun))
                    {
                        var newTextRun = textRun.Copy();
                        newTextRun.Start = start;
                        newTextRun.End = end;
                        result.Add(newTextRun);
                        start = end;
                    }
                }

                end++;
            }
            if (start != end - 1)
            {
                var newRun = currentTextRun.Copy();
                newRun.Start = start;
                newRun.End = end - 1;
                result.Add(newRun);
            }

            return result.ToArray();
        }

        public ExtendedTextRun[] GetAllRuns()
        {
            var result = new List<ExtendedTextRun>();
            foreach (var line in Lines)
            {
                result.AddRange(line.TextRuns);
                var newLineRun = result.Count > 0 ? result.Last().Copy() : new TextStyleRun(0, 0, DefaultStyle).ToTextRun();
                newLineRun.Start = newLineRun.End;
                newLineRun.End++;
                result.Add(newLineRun);
            }
            return Lines.SelectMany(l => l.TextRuns).ToArray();
        }
    }

    class LineExtendedTextRun
    {
        public ExtendedTextRun[] TextRuns { get; }

        string LineText { get; }

        public LineExtendedTextRun(string lineText, int startIndex, TextStyle defaultStyle, TextStyleRun[] styles)
        {
            LineText = lineText;

            var elementCount = new StringInfo(lineText).LengthInTextElements;
            var textRuns = new List<ExtendedTextRun>();
            for (var i = 0; i < elementCount; i++)
            {
                var style = styles.FirstOrDefault(s => s.Start <= i && s.End > i);
                if (style == null)
                {
                    style = new TextStyleRun(i + startIndex, i + startIndex + 1, defaultStyle);
                }
                textRuns.Add(style.ToTextRun());
            }

            TextRuns = textRuns.ToArray();
        }
    }
}

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

        public string[] TextElements { get; }

        public int TotalElementCount { get; }

        public int TotalElementCountWithoutNewLine { get; }

        TextStyle DefaultStyle { get; }

        LineExtendedTextRun[] Lines { get; }

        public StructuredExtendedTextRun(string sourceText, TextStyle defaultStyle, TextStyleRun[] styles)
        {
            SourceText = sourceText;
            DefaultStyle = defaultStyle;
            TotalElementCount = new StringInfo(sourceText).LengthInTextElements;

            var elements = new List<string>();
            var stringInfoEnumerator = StringInfo.GetTextElementEnumerator(sourceText);
            while (stringInfoEnumerator.MoveNext())
            {
                var s = stringInfoEnumerator.Current.ToString();
                if (s != null)
                {
                    elements.Add(s);
                }
            }
            TextElements = elements.ToArray();

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
            TotalElementCountWithoutNewLine = TotalElementCount - Lines.Length + 1;
        }

        public ExtendedTextRun[] Flatten()
        {
            var currentTextRun = new TextStyleRun(0, TotalElementCount, DefaultStyle).ToTextRun();
            var result = new List<ExtendedTextRun>();

            foreach (var line in Lines)
            {
                foreach (var textRun in line.TextRuns)
                {
                    if (result.Count < 1)
                    {
                        result.Add(textRun.Copy());
                        currentTextRun = result.Last();
                    }
                    else if (!currentTextRun.EqualsWithoutRun(textRun))
                    {
                        result.Last().End = textRun.Start;
                        result.Add(textRun.Copy());
                        currentTextRun = result.Last();
                    }
                }
            }
            if (currentTextRun.End != TotalElementCount)
            {
                currentTextRun.End = TotalElementCount;
            }
            else if (result.Count < 1)
            {
                result.Add(currentTextRun);
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

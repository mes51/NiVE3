using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Text
{
    class StructuredExtendedTextRun
    {
        static readonly Rune WhiteSpace = new Rune(' ');

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
            TextElements = [..elements];

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

            Lines = [..lines];
            TotalElementCountWithoutNewLine = TotalElementCount - Lines.Length + 1;
        }

        private StructuredExtendedTextRun(string sourceText, TextStyle defaultStyle, ExtendedTextRun[] styles)
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
            TextElements = [..elements];

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

            Lines = [..lines];
            TotalElementCountWithoutNewLine = TotalElementCount - Lines.Length + 1;
        }

        public StructuredExtendedTextRun ReconstructTextRunWithOffset()
        {
            if (Lines.All(l => l.TextRuns.All(s => s.CharacterOffset == 0)))
            {
                return this;
            }

            var styles = Flatten();
            var runeStyles = new List<ExtendedTextRun>();
            var sb = new StringBuilder();
            var index = 0;
            var runeIndex = 0;
            foreach (var line in SourceText.Replace("\r", "").Split("\n"))
            {
                if (index != 0)
                {
                    sb.AppendLine();
                }
                var ge = StringInfo.GetTextElementEnumerator(line);
                while (ge.MoveNext())
                {
                    var grapheme = (string)ge.Current;
                    var style = styles.First(s => s.Start <= index && index < s.End);
                    var runeStyle = style.Copy();
                    runeStyle.Start = runeIndex;
                    foreach (var rune in grapheme.EnumerateRunes())
                    {
                        if (style.CharacterOffset == 0)
                        {
                            sb.Append(rune);
                        }
                        else
                        {
                            var newCode = rune.Value + style.CharacterOffset;
                            if (rune.IsAscii && style.RestrictAscii)
                            {
                                var asciiRune = new Rune((int)(unchecked((uint)newCode) % 128));
                                if (Rune.IsControl(asciiRune))
                                {
                                    sb.Append(' ');
                                }
                                else
                                {
                                    sb.Append(asciiRune);
                                }
                            }
                            else
                            {
                                if (Rune.IsValid(newCode))
                                {
                                    sb.Append(new Rune(newCode));
                                }
                                else
                                {
                                    sb.Append(style.WhiteSpaceReplacementChar ? WhiteSpace : Rune.ReplacementChar);
                                }
                            }
                        }
                        runeIndex++;
                    }
                    index++;
                    runeStyle.End = runeIndex;
                    runeStyles.Add(runeStyle);
                }

                index++;
                runeIndex++;
            }

            var reconstructedStyles = new List<ExtendedTextRun>();
            index = 0;
            runeIndex = 0;
            foreach (var line in sb.ToString().Replace("\r", "").Split("\n"))
            {
                var ge = StringInfo.GetTextElementEnumerator(line);
                while (ge.MoveNext())
                {
                    var grapheme = (string)ge.Current;
                    var style = runeStyles.SkipWhile(s => s.Start < runeIndex && s.End > runeIndex).First();
                    var reconstructedStyle = style.Copy();
                    reconstructedStyle.Start = index;
                    reconstructedStyle.End = index + 1;
                    reconstructedStyles.Add(reconstructedStyle);
                    index++;
                    runeIndex += grapheme.EnumerateRunes().Count();
                }

                index++;
                runeIndex++;
            }

            return new StructuredExtendedTextRun(sb.ToString(), DefaultStyle, reconstructedStyles.ToArray());
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

            return [..result];
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

        public LineExtendedTextRun(string lineText, int startIndex, TextStyle defaultStyle, TextStyleRun[] styles)
        {
            var elementCount = new StringInfo(lineText).LengthInTextElements;
            var textRuns = new List<ExtendedTextRun>();
            for (var i = 0; i < elementCount; i++)
            {
                var style = styles.FirstOrDefault(s => s.Start <= i && s.End > i) ?? new TextStyleRun(i + startIndex, i + startIndex + 1, defaultStyle);
                textRuns.Add(style.ToTextRun());
            }

            TextRuns = [..textRuns];
        }

        public LineExtendedTextRun(string lineText, int startIndex, TextStyle defaultStyle, ExtendedTextRun[] styles)
        {
            var elementCount = new StringInfo(lineText).LengthInTextElements;
            var textRuns = new List<ExtendedTextRun>();
            for (var i = 0; i < elementCount; i++)
            {
                var style = styles.FirstOrDefault(s => s.Start <= i && s.End > i) ?? new TextStyleRun(i + startIndex, i + startIndex + 1, defaultStyle).ToTextRun();
                textRuns.Add(style);
            }

            TextRuns = [..textRuns];
        }
    }
}

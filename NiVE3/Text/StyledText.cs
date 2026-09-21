using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;

namespace NiVE3.Text
{
    record StyledText(string Text, TextStyle DefaultStyle, TextStyleRun[] Styles)
    {
        public static StyledText Empty = new StyledText("", TextStyle.Empty, []);

        public StyledText ChangeText(string newText)
        {
            var newLength = newText.GetGraphemeCount();
            var newStyles = new List<TextStyleRun>();
            foreach (var s in Styles)
            {
                if (s.Start >= newLength)
                {
                    break;
                }

                if (s.End >= newLength)
                {
                    newStyles.Add(new TextStyleRun(s.Start, newLength, DefaultStyle));
                    break;
                }
                else
                {
                    newStyles.Add(s);
                }
            }
            return new StyledText(newText, DefaultStyle, [..newStyles]);
        }

        public IDictionary<string, object?> Serialize()
        {
            return new Dictionary<string, object?>
            {
                { nameof(Text), Text },
                { nameof(DefaultStyle), DefaultStyle.Serialize() },
                { nameof(Styles), Styles.Select(s => s.Serialize()) },
            };
        }

        public static StyledText Deserialize(IDictionary<string, object?> dic)
        {
            return new StyledText(
                (string)(dic[nameof(Text)] ?? ""),
                dic[nameof(DefaultStyle)] is IDictionary<string, object?> defaultStyle ? TextStyle.Deserialize(defaultStyle) : TextStyle.Empty,
                dic[nameof(Styles)] is Array styles ? styles.Cast<IDictionary<string, object?>>().Select(TextStyleRun.Deserialize).ToArray() : []
            );
        }

        public virtual bool Equals(StyledText? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Text == other.Text &&
                DefaultStyle == other.DefaultStyle &&
                Styles.SequenceEqual(other.Styles);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(Text);
            hashCode.Add(DefaultStyle);
            foreach (var s in Styles)
            {
                hashCode.Add(s);
            }

            return hashCode.ToHashCode();
        }

        public static StyledText ApplyEdit(StyledText baseStyledText, int offset, string removedText, string insertedText)
        {
            var baseText = baseStyledText.Text;
            var newText = string.Concat(baseText.AsSpan(0, offset), insertedText, baseText.AsSpan(offset + removedText.Length));

            if (baseStyledText.Styles.Length < 1)
            {
                return new StyledText(newText, baseStyledText.DefaultStyle, []);
            }

            var removedLength = removedText.Length;
            var insertedLength = insertedText.Length;
            var removeEnd = offset + removedText.Length;
            var newStyleRuns = new List<TextStyleRun>(baseStyledText.Styles.Length);
            foreach (var textRun in baseStyledText.Styles)
            {
                var s = textRun.Start;
                var e = textRun.End;
                if (s > offset)
                {
                    if (textRun.Start >= removeEnd)
                    {
                        s = textRun.Start - removedLength;
                    }
                    else
                    {
                        s = offset;
                    }
                }
                if (e > offset)
                {
                    if (textRun.End >= removeEnd)
                    {
                        e = textRun.End - removedLength;
                    }
                    else
                    {
                        e = offset;
                    }
                }

                if (insertedLength > 0 && e >= offset)
                {
                    if (e == offset)
                    {
                        if (s < offset)
                        {
                            e += insertedLength;
                        }
                    }
                    else if (s <= offset)
                    {
                        e += insertedLength;
                    }
                    else
                    {
                        s += insertedLength;
                        e += insertedLength;
                    }
                }

                if (e > s)
                {
                    newStyleRuns.Add(new TextStyleRun(s, e, textRun.Style));
                }
            }

            return new StyledText(newText, baseStyledText.DefaultStyle, TextStyleRun.Merge(newStyleRuns));
        }

        public static StyledText ApplyStyle(StyledText baseStyledText, int start, int length, Func<TextStyle, TextStyle> styleTransformer)
        {
            if (length < 1)
            {
                return baseStyledText;
            }

            var end = start + length;

            var result = new List<TextStyleRun>();
            var inside = new List<TextStyleRun>();
            foreach (var run in baseStyledText.Styles)
            {
                if (run.Start < start)
                {
                    result.Add(new TextStyleRun(run.Start, Math.Min(run.End, start), run.Style));
                }
                if (run.End > end)
                {
                    result.Add(new TextStyleRun(Math.Max(run.Start, end), run.End, run.Style));
                }

                var s = Math.Max(run.Start, start);
                var e = Math.Min(run.End, end);
                if (e > s)
                {
                    inside.Add(new TextStyleRun(s, e, styleTransformer(run.Style)));
                }
            }

            var pos = start;
            var transformedDefaultStyle = styleTransformer(baseStyledText.DefaultStyle);
            foreach (var segment in inside.OrderBy(r => r.Start))
            {
                if (segment.Start > pos)
                {
                    result.Add(new TextStyleRun(pos, segment.Start, transformedDefaultStyle));
                }

                result.Add(segment);
                pos = segment.End;
            }
            if (pos < end)
            {
                result.Add(new TextStyleRun(pos, end, transformedDefaultStyle));
            }

            result.Sort((a, b) => a.Start.CompareTo(b.Start));
            return new StyledText(baseStyledText.Text, baseStyledText.DefaultStyle, TextStyleRun.Merge(result));
        }

        public static StyledText ChangeDefaultStyle(StyledText baseStyledText, Func<TextStyle, TextStyle> styleTransformer)
        {
            var newTextStyleRun = baseStyledText.Styles.Select(s => new TextStyleRun(s.Start, s.End, styleTransformer(s.Style)));
            var newDefaultStyle = styleTransformer(baseStyledText.DefaultStyle);

            return new StyledText(baseStyledText.Text, newDefaultStyle, [..newTextStyleRun]);
        }
    }
}

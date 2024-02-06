using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Text
{
    record StyledText(string Text, TextStyle DefaultStyle, TextStyleRun[] Styles)
    {
        public static StyledText Empty = new StyledText("", TextStyle.Empty, Array.Empty<TextStyleRun>());

        public StyledText ChangeText(string newText)
        {
            var newLength = StringInfo.GetNextTextElementLength(newText);
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
                }
                else
                {
                    newStyles.Add(s);
                }
            }
            return new StyledText(newText, DefaultStyle, newStyles.ToArray());
        }

        public static StyledText Deserialize(IDictionary<string, object?> dic)
        {
            return new StyledText(
                (string)(dic[nameof(Text)] ?? ""),
                dic[nameof(DefaultStyle)] is IDictionary<string, object?> defaultStyle ? TextStyle.Deserialize(defaultStyle) : TextStyle.Empty,
                dic[nameof(Styles)] is Array styles ? styles.Cast<IDictionary<string, object?>>().Select(TextStyleRun.Deserialize).ToArray() : Array.Empty<TextStyleRun>()
            );
        }
    }
}

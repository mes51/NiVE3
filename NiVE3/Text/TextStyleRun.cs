using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts;

namespace NiVE3.Text
{
    record TextStyleRun(int Start, int End, TextStyle Style)
    {
        public static TextStyleRun Deserialize(IDictionary<string, object?> dic)
        {
            return new TextStyleRun(
                (int)(dic[nameof(Start)] ?? 0),
                (int)(dic[nameof(End)] ?? 0),
                dic[nameof(Style)] is IDictionary<string, object?> style ? TextStyle.Deserialize(style) : TextStyle.Empty
            );
        }

        public TextRun ToTextRun()
        {
            return new TextRun()
            {
                Start = Start,
                End = End,
                Font = new Font(
                    (FontInfo.FindByUniqueId(Style.FontUniqueId) ?? FontInfo.FallbackFont).FontFamily,
                    Style.FontSize,
                    (Style.IsEnableBold, Style.IsEnableBold) switch
                    {
                        (true, true) => FontStyle.BoldItalic,
                        (true, false) => FontStyle.Bold,
                        (false, false) => FontStyle.Italic,
                        _ => FontStyle.Regular
                    }
                ),
            };
        }
    }
}

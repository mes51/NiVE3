using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts;

namespace NiVE3.Text
{
    record TextStyleRun(int Start, int End, TextStyle Style)
    {
        public IDictionary<string, object?> Serialize()
        {
            return new Dictionary<string, object?>
            {
                { nameof(Start), Start },
                { nameof(End), End },
                { nameof(Style), Style.Serialize() },
            };
        }

        public static TextStyleRun Deserialize(IDictionary<string, object?> dic)
        {
            return new TextStyleRun(
                (int)(dic[nameof(Start)] ?? 0),
                (int)(dic[nameof(End)] ?? 0),
                dic[nameof(Style)] is IDictionary<string, object?> style ? TextStyle.Deserialize(style) : TextStyle.Empty
            );
        }

        public ExtendedTextRun ToTextRun()
        {
            return new ExtendedTextRun()
            {
                Start = Start,
                End = End,
                Font = new Font(
                    (FontInfo.FindByUniqueId(Style.FontUniqueId) ?? FontInfo.FallbackFont).FontFamily,
                    Style.FontSize,
                    (Style.IsEnableBold, Style.IsEnableItalic) switch
                    {
                        (true, true) => FontStyle.BoldItalic,
                        (true, false) => FontStyle.Bold,
                        (false, true) => FontStyle.Italic,
                        _ => FontStyle.Regular
                    }
                ),
                LetterSpacing = Style.LetterSpacing,
                VerticalScale = Style.VerticalScale,
                HorizontalScale = Style.HorizontalScale,
                TextLineDrawOrder = Style.TextLineDrawOrder,
                TextLineWidth = Style.TextLineWidth,
                FillColor = Style.FillColor,
                TextLineColor = Style.TextLineColor
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Text
{
    record TextStyle(
        string FontUniqueId,
        float FontSize,
        float LineHeight,
        float LetterSpacing,
        float VerticalScale,
        float HorizontalScale,
        TextLineDrawOrder TextLineDrawOrder,
        bool IsEnableBold,
        bool IsEnableItalic,
        TextAlign TextAlign
    )
    {
        public static TextStyle Empty = new TextStyle("", 0.0F, 0.0F, 0.0F, 0.0F, 0.0F, TextLineDrawOrder.None, false, false, TextAlign.Left);

        public static TextStyle Deserialize(IDictionary<string, object?> dic)
        {
            return new TextStyle(
                (string)(dic[nameof(FontUniqueId)] ?? ""),
                (float)(double)(dic[nameof(FontSize)] ?? 0.0),
                (float)(double)(dic[nameof(LineHeight)] ?? 0.0),
                (float)(double)(dic[nameof(LetterSpacing)] ?? 0.0),
                (float)(double)(dic[nameof(VerticalScale)] ?? 100.0),
                (float)(double)(dic[nameof(HorizontalScale)] ?? 100.0),
                (TextLineDrawOrder)Enum.Parse(typeof(TextLineDrawOrder), (string)(dic[nameof(TextAlign)] ?? TextLineDrawOrder.None.ToString())),
                (bool)(dic[nameof(IsEnableBold)] ?? false),
                (bool)(dic[nameof(IsEnableItalic)] ?? false),
                (TextAlign)Enum.Parse(typeof(TextAlign), (string)(dic[nameof(TextAlign)] ?? TextAlign.Left.ToString()))
            );
        }
    }

    enum TextLineDrawOrder
    {
        None,
        BeforeFill,
        AfterFill
    }

    enum TextAlign
    {
        Left,
        Center,
        Right
    }
}

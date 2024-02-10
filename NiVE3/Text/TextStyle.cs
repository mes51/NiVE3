using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        float TextLineWidth,
        bool IsEnableBold,
        bool IsEnableItalic,
        TextAlign TextAlign,
        Vector4 FillColor,
        Vector4 OutlineColor
    )
    {
        public static TextStyle Empty = new TextStyle("", 0.0F, 0.0F, 0.0F, 0.0F, 0.0F, TextLineDrawOrder.None, 0.0F, false, false, TextAlign.Left, Vector4.Zero, Vector4.Zero);

        static readonly IDictionary<string, object?> DeserializedTransparentColor = new Dictionary<string, object?>
        {
            { nameof(Vector4.X), 0.0F },
            { nameof(Vector4.Y), 0.0F },
            { nameof(Vector4.Z), 0.0F },
            { nameof(Vector4.W), 0.0F },
        };

        public IDictionary<string, object?> Serialize()
        {
            var fillColorDic = new Dictionary<string, object?>
            {
                { nameof(Vector4.X), FillColor.X },
                { nameof(Vector4.Y), FillColor.Y },
                { nameof(Vector4.Z), FillColor.Z },
                { nameof(Vector4.W), FillColor.W }
            };
            var outlineColorDic = new Dictionary<string, object?>
            {
                { nameof(Vector4.X), OutlineColor.X },
                { nameof(Vector4.Y), OutlineColor.Y },
                { nameof(Vector4.Z), OutlineColor.Z },
                { nameof(Vector4.W), OutlineColor.W }
            };
            var result = new Dictionary<string, object?>
            {
                { nameof(FontUniqueId), FontUniqueId },
                { nameof(FontSize), FontSize },
                { nameof(LineHeight), LineHeight },
                { nameof(LetterSpacing), LetterSpacing },
                { nameof(HorizontalScale), HorizontalScale },
                { nameof(VerticalScale), VerticalScale },
                { nameof(TextLineDrawOrder), TextLineDrawOrder },
                { nameof(TextLineWidth), TextLineWidth },
                { nameof(IsEnableBold), IsEnableBold },
                { nameof(IsEnableItalic), IsEnableItalic },
                { nameof(TextAlign), TextAlign },
                { nameof(FillColor), fillColorDic },
                { nameof(OutlineColor), outlineColorDic }
            };

            return result;
        }

        public static TextStyle Deserialize(IDictionary<string, object?> dic)
        {
            var fillColorDic = dic[nameof(FillColor)] as IDictionary<string, object?> ?? DeserializedTransparentColor;
            var outlineColorDic = dic[nameof(OutlineColor)] as IDictionary<string, object?> ?? DeserializedTransparentColor;
            return new TextStyle(
                (string)(dic[nameof(FontUniqueId)] ?? ""),
                (float)Convert.ToDouble(dic[nameof(FontSize)] ?? 0.0),
                (float)Convert.ToDouble(dic[nameof(LineHeight)] ?? 0.0),
                (float)Convert.ToDouble(dic[nameof(LetterSpacing)] ?? 0.0),
                (float)Convert.ToDouble(dic[nameof(VerticalScale)] ?? 100.0),
                (float)Convert.ToDouble(dic[nameof(HorizontalScale)] ?? 100.0),
                (TextLineDrawOrder)Convert.ToInt32(dic[nameof(TextLineDrawOrder)] ?? 0),
                (float)Convert.ToDouble(dic[nameof(TextLineWidth)] ?? 0.0),
                (bool)(dic[nameof(IsEnableBold)] ?? false),
                (bool)(dic[nameof(IsEnableItalic)] ?? false),
                (TextAlign)Convert.ToInt32(dic[nameof(TextAlign)] ?? 0),
                new Vector4(
                    (float)Convert.ToDouble(fillColorDic[nameof(Vector4.X)] ?? 0.0),
                    (float)Convert.ToDouble(fillColorDic[nameof(Vector4.Y)] ?? 0.0),
                    (float)Convert.ToDouble(fillColorDic[nameof(Vector4.Z)] ?? 0.0),
                    (float)Convert.ToDouble(fillColorDic[nameof(Vector4.W)] ?? 0.0)
                ),
                new Vector4(
                    (float)Convert.ToDouble(outlineColorDic[nameof(Vector4.X)] ?? 0.0),
                    (float)Convert.ToDouble(outlineColorDic[nameof(Vector4.Y)] ?? 0.0),
                    (float)Convert.ToDouble(outlineColorDic[nameof(Vector4.Z)] ?? 0.0),
                    (float)Convert.ToDouble(outlineColorDic[nameof(Vector4.W)] ?? 0.0)
                )
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

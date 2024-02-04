using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.IR.Values;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Shared.Extension;

namespace NiVE3.Property.Types
{
    class SourceTextPropertyType : IPropertyType
    {
        public static readonly SourceTextPropertyType Instance = new SourceTextPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        private SourceTextPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.IndexOfLast(k => k.Time <= t);
            if (baseKeyFrameIndex < 0)
            {
                return keyFrames[0].Value;
            }
            else if (baseKeyFrameIndex >= keyFrames.Count - 1)
            {
                return keyFrames[baseKeyFrameIndex].Value;
            }
            return keyFrames[baseKeyFrameIndex].Value;
        }

        public bool TryConvertFrom(object otherValue, out object convertedValue)
        {
            if (otherValue is DecoratedText)
            {
                convertedValue = otherValue;
                return true;
            }
            else if (otherValue is string s)
            {
                convertedValue = new DecoratedText(s, TextStyle.Empty, Array.Empty<TextStyleRun>());
                return true;
            }
            else
            {
                convertedValue = DecoratedText.Empty;
                return false;
            }
        }

        public object? SerializeValue(object? value)
        {
            return value;
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue is not IDictionary<string, object?> dictionary)
            {
                return null;
            }

            return DecoratedText.Deserialize(dictionary);
        }
    }

    record DecoratedText(string Text, TextStyle DefaultStyle, TextStyleRun[] Styles)
    {
        public static DecoratedText Empty = new DecoratedText("", TextStyle.Empty, Array.Empty<TextStyleRun>());

        public DecoratedText ChangeText(string newText)
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
            return new DecoratedText(newText, DefaultStyle, newStyles.ToArray());
        }

        public static DecoratedText Deserialize(IDictionary<string, object?> dic)
        {
            return new DecoratedText(
                (string)(dic[nameof(Text)]  ?? ""),
                dic[nameof(DefaultStyle)] is IDictionary<string, object?> defaultStyle ? TextStyle.Deserialize(defaultStyle) : TextStyle.Empty,
                dic[nameof(Styles)] is Array styles ? styles.Cast<IDictionary<string, object?>>().Select(TextStyleRun.Deserialize).ToArray() : Array.Empty<TextStyleRun>()
            );
        }
    }

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
    }

    record TextStyle(
        string FontUniqueId,
        double FontSize,
        double LineHeight,
        double LetterSpacing,
        double VerticalScale,
        double HorizontalScale,
        TextLineDrawOrder TextLineDrawOrder,
        bool IsEnableBold,
        bool IsEnableItalic,
        bool IsEnableLigature,
        TextAlign TextAlign
    )
    {
        public static TextStyle Empty = new TextStyle("", 0.0, 0.0, 0.0, 0.0, 0.0, TextLineDrawOrder.None, false, false, false, TextAlign.Left);

        public static TextStyle Deserialize(IDictionary<string, object?> dic)
        {
            return new TextStyle(
                (string)(dic[nameof(FontUniqueId)] ?? ""),
                (double)(dic[nameof(FontSize)] ?? 0.0),
                (double)(dic[nameof(LineHeight)] ?? 0.0),
                (double)(dic[nameof(LetterSpacing)] ?? 0.0),
                (double)(dic[nameof(VerticalScale)] ?? 100.0),
                (double)(dic[nameof(HorizontalScale)] ?? 100.0),
                (TextLineDrawOrder)Enum.Parse(typeof(TextLineDrawOrder), (string)(dic[nameof(TextAlign)] ?? TextLineDrawOrder.None.ToString())),
                (bool)(dic[nameof(IsEnableBold)] ?? false),
                (bool)(dic[nameof(IsEnableItalic)] ?? false),
                (bool)(dic[nameof(IsEnableLigature)] ?? false),
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

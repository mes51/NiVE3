using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using NiVE3.Plugin.Internal.Util;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Types;
using NiVE3.Shared.Extension;
using NiVE3.Text;

namespace NiVE3.Property.Types
{
    class SourceTextPropertyType : IPropertyType
    {
        public static readonly SourceTextPropertyType Instance = new SourceTextPropertyType();

        public InterpolationType SupportedInterpolationTypes => InterpolationType.None;

        public bool IsSupportedExpression => true;

        private SourceTextPropertyType() { }

        public object? Interpolate(IReadOnlyList<KeyFrame> keyFrames, double t)
        {
            var baseKeyFrameIndex = keyFrames.FindLastIndex(k => k.Time <= t);
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

        public object? SerializeValue(object? value)
        {
            if (value is not StyledText styledText)
            {
                return null;
            }

            return styledText.Serialize();
        }

        public object? DeserializeValue(object? serializedValue)
        {
            if (serializedValue is not IDictionary<string, object?> dictionary)
            {
                return null;
            }

            return StyledText.Deserialize(dictionary);
        }

        public static object? ReplaceDefaultStyle(object? value, TextStyle newStyle)
        {
            if (value is not StyledText styledText)
            {
                return null;
            }

            return new StyledText(styledText.Text, newStyle, styledText.Styles);
        }

        public static TextStyle? GetDefaultStyle(object? value)
        {
            if (value is not StyledText styledText)
            {
                return null;
            }

            return styledText.DefaultStyle;
        }

        public Span<byte> ConvertToHashBase(object? value)
        {
            if (value is not StyledText styledText)
            {
                return [];
            }

            var hashBase = new List<byte>();

            hashBase.AddRange(Encoding.Unicode.GetBytes(styledText.Text));
            foreach (var run in styledText.Styles)
            {
                hashBase.AddRange(BitConverter.GetBytes(run.Start));
                hashBase.AddRange(BitConverter.GetBytes(run.End));
            }

            foreach (var style in styledText.Styles.Select(r => r.Style).Prepend(styledText.DefaultStyle))
            {
                hashBase.AddRange(Encoding.Unicode.GetBytes(style.FontUniqueId));
                hashBase.AddRange(BitConverter.GetBytes(style.FontSize));
                hashBase.AddRange(BitConverter.GetBytes(style.LineHeight));
                hashBase.AddRange(BitConverter.GetBytes(style.LetterSpacing));
                hashBase.AddRange(BitConverter.GetBytes(style.VerticalScale));
                hashBase.AddRange(BitConverter.GetBytes(style.HorizontalScale));
                hashBase.AddRange(BitConverter.GetBytes((int)style.TextLineDrawOrder));
                hashBase.AddRange(BitConverter.GetBytes(style.TextLineWidth));
                hashBase.AddRange(BitConverter.GetBytes(style.IsEnableBold));
                hashBase.AddRange(BitConverter.GetBytes(style.IsEnableItalic));
                hashBase.AddRange(BitConverter.GetBytes((int)style.TextAlign));
                hashBase.AddRange(style.FillColor.ConvertToSpan());
                hashBase.AddRange(style.TextLineColor.ConvertToSpan());
            }

            return hashBase.ToArray();
        }

        public bool TryConvertFromExpressionValue(object? expressionValue, object? rawValue, out object? value)
        {
            if (rawValue is not StyledText baseText)
            {
                value = null;
                return false;
            }

            if (expressionValue is string text)
            {
                value = baseText.ChangeText(text);
                return true;
            }
            else if (expressionValue is IDictionary<string, object?> dictionary)
            {
                if (!dictionary.TryGetValue("text", out string? styledText) || !dictionary.TryGetValue("defaultStyle", out IDictionary<string, object?>? defaultStyleData))
                {
                    value = null;
                    return false;
                }

                var defaultStyle = ConvertExpressionValueToStyle(defaultStyleData, baseText.DefaultStyle);
                var styles = new List<TextStyleRun>();
                if (dictionary.TryGetValue("styles", out object[]? styleRunsData))
                {
                    foreach (var styleRunData in styleRunsData.OfType<IDictionary<string, object?>>())
                    {
                        if (styleRunData.TryGetValue("run", out object[]? run) && run.Length > 1 && styleRunData.TryGetValue("style", out IDictionary<string, object?>? styleData))
                        {
                            var begin = Convert.ToInt32(run[0]);
                            var end = Convert.ToInt32(run[1]);
                            if (begin == end)
                            {
                                continue;
                            }
                            else if (end < begin)
                            {
                                (begin, end) = (end, begin);
                            }
                            styles.Add(new TextStyleRun(begin, end, ConvertExpressionValueToStyle(styleData, baseText.DefaultStyle)));
                        }
                    }
                }

                value = new StyledText(styledText, defaultStyle, [..styles]);
                return true;
            }

            value = null;
            return false;
        }

        public object? ConvertToExpressionValue(object? value)
        {
            if (value is not StyledText text)
            {
                return null;
            }

            var styles = text.Styles.Select(s =>
            {
                return (object)new Dictionary<string, object?>
                {
                    { "style", ConvertStyleToExpressionValue(s.Style) },
                    { "run", new object[] { s.Start, s.End } }
                };
            }).ToArray();
            return new Dictionary<string, object?>
            {
                { "text", text.Text },
                { "defaultStyle", ConvertStyleToExpressionValue(text.DefaultStyle) },
                { "styles", styles }
            };
        }

        static object ConvertStyleToExpressionValue(TextStyle style)
        {
            return new Dictionary<string, object?>
            {
                { "font", style.FontUniqueId },
                { "fontSize", style.FontSize },
                { "lineHeight", style.LineHeight },
                { "verticalScale", style.VerticalScale },
                { "horizontalScale", style.HorizontalScale },
                { "order", (int)style.TextLineDrawOrder },
                { "lineWidth", style.TextLineWidth },
                { "isEnableBold", style.IsEnableBold },
                { "isEnableItalic", style.IsEnableItalic },
                { "align", (int)style.TextAlign },
                { "fillColor", style.FillColor },
                { "lineColor", style.TextLineColor }
            };
        }

        static TextStyle ConvertExpressionValueToStyle(IDictionary<string, object?> expressionValue, TextStyle defaultStyle)
        {
            if (!expressionValue.TryGetValue("font", out string? font))
            {
                font = defaultStyle.FontUniqueId;
            }
            if (!expressionValue.TryGetValue("fontSize", out double fontSize))
            {
                fontSize = defaultStyle.FontSize;
            }
            if (!expressionValue.TryGetValue("lineHeight", out double lineHeight))
            {
                lineHeight = defaultStyle.LineHeight;
            }
            if (!expressionValue.TryGetValue("verticalScale", out double verticalScale))
            {
                verticalScale = defaultStyle.VerticalScale;
            }
            if (!expressionValue.TryGetValue("horizontalScale", out double horizontalScale))
            {
                horizontalScale = defaultStyle.HorizontalScale;
            }
            var order = expressionValue.TryGetValue("order", out double orderValue) ? (TextLineDrawOrder)(int)orderValue : defaultStyle.TextLineDrawOrder;
            if (!expressionValue.TryGetValue("lineWidth", out double lineWidth))
            {
                lineWidth = defaultStyle.TextLineWidth;
            }
            if (!expressionValue.TryGetValue("isEnableBold", out bool isEnableBold))
            {
                isEnableBold = defaultStyle.IsEnableBold;
            }
            if (!expressionValue.TryGetValue("isEnableItalic", out bool isEnableItalic))
            {
                isEnableItalic = defaultStyle.IsEnableItalic;
            }
            var align = expressionValue.TryGetValue("align", out double alignValue) ? (TextAlign)(int)alignValue : defaultStyle.TextAlign;
            var fillColor = expressionValue.TryGetValue("fillColor", out object[]? fillColorValue) ? ExpressionObjectArrayToVector4(fillColorValue) : defaultStyle.FillColor;
            var lineColor = expressionValue.TryGetValue("lineColor", out object[]? lineColorValue) ? ExpressionObjectArrayToVector4(lineColorValue) : defaultStyle.TextLineColor;

            return new TextStyle(font, (float)fontSize, (float)lineHeight, defaultStyle.LetterSpacing, (float)verticalScale, (float)horizontalScale, order, (float)lineWidth, isEnableBold, isEnableItalic, align, fillColor, lineColor);
        }

        static Vector4 ExpressionObjectArrayToVector4(object[] values)
        {
            var doubleValues = values.OfType<double>().ToArray();
            switch (doubleValues.Length)
            {
                case 1:
                    return new Vector4((float)doubleValues[0], 0.0F, 0.0F, 0.0F);
                case 2:
                    return new Vector4((float)doubleValues[0], (float)doubleValues[1], 0.0F, 0.0F);
                case 3:
                    return new Vector4((float)doubleValues[0], (float)doubleValues[1], (float)doubleValues[2], 0.0F);
                case 4:
                    return new Vector4((float)doubleValues[0], (float)doubleValues[1], (float)doubleValues[2], (float)doubleValues[3]);
                default:
                    return Vector4.Zero;
            }
        }
    }
}

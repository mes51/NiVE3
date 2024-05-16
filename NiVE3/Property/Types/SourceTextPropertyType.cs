using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ILGPU.IR.Values;
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
            if (otherValue is StyledText)
            {
                convertedValue = otherValue;
                return true;
            }
            else if (otherValue is string s)
            {
                convertedValue = new StyledText(s, TextStyle.Empty, []);
                return true;
            }
            else
            {
                convertedValue = StyledText.Empty;
                return false;
            }
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
                hashBase.AddRange(style.FillColor.ConvertToSpan().ToArray());
                hashBase.AddRange(style.TextLineColor.ConvertToSpan().ToArray());
            }

            return hashBase.ToArray();
        }
    }
}

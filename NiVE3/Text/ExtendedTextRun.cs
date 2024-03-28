using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using SixLabors.Fonts;

namespace NiVE3.Text
{
    class ExtendedTextRun : TextRun, IEquatable<ExtendedTextRun>
    {
        public float LetterSpacing { get; set; }

        public float VerticalScale { get; set; }

        public float HorizontalScale { get; set; }

        public TextLineDrawOrder TextLineDrawOrder { get; set; }

        public float TextLineWidth { get; set; }

        public Vector4 FillColor { get; set; }

        public Vector4 TextLineColor { get; set; }

        public Vector2 AnchorPoint { get; set; }

        public Vector2 Position { get; set; }

        public Vector2 Scale { get; set; } = Vector2.One;

        public float Angle { get; set; }

        public float Skew { get; set; }

        public float SkewAxis { get; set; }

        public float Opacity { get; set; } = 1.0F;

        public int CharacterOffset { get; set; }

        public bool WhiteSpaceReplacementChar { get; set; }

        public bool RestrictAscii { get; set; }

        public Vector2 Blur { get; set; }

        public ExtendedTextRun Copy()
        {
            return new ExtendedTextRun
            {
                Start = Start,
                End = End,
                Font = Font,
                TextAttributes = TextAttributes,
                TextDecorations = TextDecorations,
                LetterSpacing = LetterSpacing,
                VerticalScale = VerticalScale,
                HorizontalScale = HorizontalScale,
                TextLineDrawOrder = TextLineDrawOrder,
                TextLineWidth = TextLineWidth,
                FillColor = FillColor,
                TextLineColor = TextLineColor,
                AnchorPoint = AnchorPoint,
                Position = Position,
                Scale = Scale,
                Angle = Angle,
                Skew = Skew,
                SkewAxis = SkewAxis,
                Opacity = Opacity,
                CharacterOffset = CharacterOffset,
                WhiteSpaceReplacementChar = WhiteSpaceReplacementChar,
                RestrictAscii = RestrictAscii,
                Blur = Blur,
            };
        }

        public bool EqualsWithoutRun(ExtendedTextRun other)
        {
            return (Font == other.Font ||
                Font?.Family == other.Font?.Family &&
                Font?.IsBold == other.Font?.IsBold &&
                Font?.IsItalic == other.Font?.IsItalic &&
                Font?.Size == other.Font?.Size) &&
                TextAttributes == other.TextAttributes &&
                TextDecorations == other.TextDecorations &&
                LetterSpacing == other.LetterSpacing &&
                VerticalScale == other.VerticalScale &&
                HorizontalScale == other.HorizontalScale &&
                TextLineDrawOrder == other.TextLineDrawOrder &&
                TextLineWidth == other.TextLineWidth &&
                FillColor == other.FillColor &&
                TextLineColor == other.TextLineColor &&
                AnchorPoint == other.AnchorPoint &&
                Position == other.Position &&
                Scale == other.Scale &&
                Angle == other.Angle &&
                Skew == other.Skew &&
                SkewAxis == other.SkewAxis &&
                Opacity == other.Opacity &&
                CharacterOffset == other.CharacterOffset &&
                WhiteSpaceReplacementChar == other.WhiteSpaceReplacementChar &&
                RestrictAscii == other.RestrictAscii &&
                Blur == other.Blur;
        }

        public bool Equals(ExtendedTextRun? other)
        {
            return other != null &&
                Start == other.Start &&
                End == other.End &&
                (Font == other.Font ||
                    Font?.Family == other.Font?.Family &&
                    Font?.IsBold == other.Font?.IsBold &&
                    Font?.IsItalic == other.Font?.IsItalic &&
                    Font?.Size == other.Font?.Size) &&
                TextAttributes == other.TextAttributes &&
                TextDecorations == other.TextDecorations &&
                LetterSpacing == other.LetterSpacing &&
                VerticalScale == other.VerticalScale &&
                HorizontalScale == other.HorizontalScale &&
                TextLineDrawOrder == other.TextLineDrawOrder &&
                TextLineWidth == other.TextLineWidth &&
                FillColor == other.FillColor &&
                TextLineColor == other.TextLineColor &&
                AnchorPoint == other.AnchorPoint &&
                Position == other.Position &&
                Scale == other.Scale &&
                Angle == other.Angle &&
                Skew == other.Skew &&
                SkewAxis == other.SkewAxis &&
                Opacity == other.Opacity &&
                CharacterOffset == other.CharacterOffset &&
                WhiteSpaceReplacementChar == other.WhiteSpaceReplacementChar &&
                RestrictAscii == other.RestrictAscii &&
                Blur == other.Blur;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ExtendedTextRun);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(Start);
            hashCode.Add(End);
            hashCode.Add(Font?.Family);
            hashCode.Add(Font?.IsBold);
            hashCode.Add(Font?.IsItalic);
            hashCode.Add(Font?.Size);
            hashCode.Add(TextAttributes);
            hashCode.Add(TextDecorations);
            hashCode.Add(LetterSpacing);
            hashCode.Add(VerticalScale);
            hashCode.Add(HorizontalScale);
            hashCode.Add(TextLineDrawOrder);
            hashCode.Add(TextLineWidth);
            hashCode.Add(FillColor);
            hashCode.Add(TextLineColor);
            hashCode.Add(AnchorPoint);
            hashCode.Add(Position);
            hashCode.Add(Scale);
            hashCode.Add(Angle);
            hashCode.Add(Skew);
            hashCode.Add(SkewAxis);
            hashCode.Add(Opacity);
            hashCode.Add(CharacterOffset);
            hashCode.Add(WhiteSpaceReplacementChar);
            hashCode.Add(RestrictAscii);
            hashCode.Add(Blur);

            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}

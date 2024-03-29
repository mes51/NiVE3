using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Shared.Extension;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.Text
{
    class StyledGlyphBuilder : IGlyphRenderer
    {
        List<GlyphPath> Glyphs { get; } = [];

        Vector2 CurrentPoint { get; set; }

        Vector2 BaseAnchorPointRate { get; }

        GlyphRendererParameters CurrentParameters { get; set; }

        PathBuilder Builder { get; }

        (Vector2 first, Vector2 second, float length)[]? TextPathPoints { get; }

        float TextBoxWidth { get; }

        float TextBoxHeight { get; }

        float ShiftedLetterSpacing { get; set; }

        bool CurrentIsDiscardGlyph { get; set; }

        public StyledGlyphBuilder(float textBoxWidth, float textBoxHeight, Vector2 baseAnchorPointRate, ISimplePath? textPath = null)
        {
            TextBoxWidth = textBoxWidth;
            TextBoxHeight = textBoxHeight;
            BaseAnchorPointRate = baseAnchorPointRate;
            Builder = new PathBuilder();
            if (textPath != null && textPath.Points.Length > 1)
            {
                var points = textPath.Points.ToArray();
                var nextPoints = points.Skip(1);
                if (textPath.IsClosed)
                {
                    nextPoints.Append(points[0]);
                }
                TextPathPoints = points.Zip(nextPoints, (f, s) =>
                {
                    var fv = (Vector2)f;
                    var sv = (Vector2)s;
                    return (fv, sv, (sv - fv).Length());
                }).ToArray();
            }
        }

        public void BeginFigure()
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }
            Builder.StartFigure();
        }

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            CurrentParameters = parameters;
            Builder.Clear();

            if ((TextBoxWidth > 0.0F && bounds.X + bounds.Width > TextBoxWidth) || (TextBoxHeight > 0.0F && bounds.Y + bounds.Height > TextBoxHeight))
            {
                CurrentIsDiscardGlyph = true;
                return true;
            }
            else
            {
                CurrentIsDiscardGlyph = false;
            }

            var transform = Matrix3x2.Identity;

            if (CurrentParameters.TextRun is ExtendedTextRun textRun)
            {
                var baseAnchorPointX = bounds.X + bounds.Width * BaseAnchorPointRate.X * textRun.HorizontalScale * 0.01F;
                var baseAnchorPointY = bounds.Y + bounds.Height * BaseAnchorPointRate.Y * textRun.VerticalScale * 0.01F;
                var skewRad = textRun.SkewAxis / 180.0F * MathF.PI;
                transform = Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y - bounds.Height) *
                    Matrix3x2.CreateScale(textRun.HorizontalScale * 0.01F, textRun.VerticalScale * 0.01F) *
                    Matrix3x2.CreateTranslation(bounds.X, bounds.Y + bounds.Height);
                var affine = Matrix3x2.CreateTranslation(-baseAnchorPointX - textRun.AnchorPoint.X, -baseAnchorPointY - textRun.AnchorPoint.Y) *
                    Matrix3x2.CreateScale(textRun.Scale.X, textRun.Scale.Y) *
                    Matrix3x2.CreateSkew(MathF.Cos(skewRad) * textRun.Skew, MathF.Sin(skewRad) * textRun.Skew) *
                    Matrix3x2.CreateRotation(textRun.Angle / 180.0F * MathF.PI) *
                    Matrix3x2.CreateTranslation(baseAnchorPointX + textRun.Position.X, baseAnchorPointY + textRun.Position.Y);
                transform *= affine;
            }

            if (TextPathPoints != null)
            {
                var centerX = bounds.Width * 0.5F;
                var posX = bounds.X + centerX + ShiftedLetterSpacing;
                var hit = false;

                while (!hit)
                {
                    foreach (var (first, second, length) in TextPathPoints)
                    {
                        if (posX > length)
                        {
                            posX -= length;
                            continue;
                        }

                        var pos = Vector2.Lerp(first, second, posX / length);
                        var diff = first - second;
                        var rad = MathF.Atan2(diff.Y, diff.X);
                        var location = new Vector2(bounds.X, bounds.Y);

                        transform *= Matrix3x2.CreateTranslation(pos - location + new Vector2(-centerX, bounds.Top)) * Matrix3x2.CreateRotation(rad - MathF.PI, pos);

                        hit = true;
                        break;
                    }
                }
            }
            else
            {
                transform *= Matrix3x2.CreateTranslation(ShiftedLetterSpacing, 0.0F);
            }

            Builder.SetTransform(transform);

            // NOTE: 現状行頭かどうかを完全に判定する手段が無いため、LetterSpacingはしばらく無効化
            //ShiftedLetterSpacing += (parameters.TextRun as ExtendedTextRun)?.LetterSpacing ?? 0.0F;

            return true;
        }

        public void BeginText(in FontRectangle bounds)
        {
            ShiftedLetterSpacing = 0.0F;
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.AddCubicBezier(CurrentPoint, secondControlPoint, thirdControlPoint, point);
            CurrentPoint = point;
        }

        public TextDecorations EnabledDecorations()
        {
            return CurrentParameters.TextRun.TextDecorations;
        }

        public void EndFigure()
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.CloseFigure();
        }

        public void EndGlyph()
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            var textRun = CurrentParameters.TextRun as ExtendedTextRun;
            textRun ??= new ExtendedTextRun
            {
                Start = CurrentParameters.TextRun.Start,
                End = CurrentParameters.TextRun.End,
                Font = CurrentParameters.TextRun.Font,
                TextAttributes = CurrentParameters.TextRun.TextAttributes,
                TextDecorations = CurrentParameters.TextRun.TextDecorations,
                VerticalScale = 100.0F,
                HorizontalScale = 100.0F,
                TextLineDrawOrder = TextLineDrawOrder.BeforeFill,
                FillColor = Vector4.One
            };
            Glyphs.Add(new GlyphPath(Builder.Build(), textRun));
        }

        public void EndText() { }

        public void LineTo(Vector2 point)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.AddLine(CurrentPoint, point);
            CurrentPoint = point;
        }

        public void MoveTo(Vector2 point)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.StartFigure();
            CurrentPoint = point;
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.AddQuadraticBezier(CurrentPoint, secondControlPoint, point);
            CurrentPoint = point;
        }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            // NOTE: 今のところ非対応
            //       対応するのであればTextRunごとに分割して適用する(多分Glyphsに混ぜると困るので別にする必要も有)
        }

        public GlyphPath[] GetGlyphs()
        {
            return [..Glyphs];
        }

        public GlyphPath[] GetRenderableGlyhps()
        {
            return Glyphs.Where(g => g.Path.Bounds.Width > 0.0F && g.Path.Bounds.Height > 0.0F && (g.FlattenedPath.Length > 0 || g.FlattenedOutlinePath.Length > 0)).ToArray();
        }
    }

    record GlyphPath(IPath Path, ExtendedTextRun TextRun)
    {
        public ISimplePath[] FlattenedPath { get; } = Path.Flatten().Where(p => p.Points.Length > 1).ToArray();

        public ISimplePath[] FlattenedOutlinePath { get; } = TextRun.TextLineWidth > 0.0F ?
            Path.GenerateOutline(TextRun.TextLineWidth).Flatten().Where(p => p.Points.Length > 1).ToArray() :
            [];
    }
}

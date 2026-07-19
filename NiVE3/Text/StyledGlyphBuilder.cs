using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Shared.Extension;
using SixLabors.Fonts;
using SixLabors.Fonts.Rendering;
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

        TextLayoutPath? TextPath { get; }

        float TextBoxWidth { get; }

        float TextBoxHeight { get; }

        float DownSampling { get; }

        float ShiftedLetterSpacing { get; set; }

        bool CurrentIsDiscardGlyph { get; set; }

        public StyledGlyphBuilder(float textBoxWidth, float textBoxHeight, double downSamplingRate, Vector2 baseAnchorPointRate, TextLayoutPath? textPath = null)
        {
            TextBoxWidth = textBoxWidth;
            TextBoxHeight = textBoxHeight;
            DownSampling = 1.0F / (float)downSamplingRate;
            BaseAnchorPointRate = baseAnchorPointRate;
            TextPath = textPath;
            Builder = new PathBuilder();
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

            var transform = Matrix3x2.CreateScale(DownSampling, DownSampling);

            if (CurrentParameters.TextRun is ExtendedTextRun textRun)
            {
                var baseAnchorPointX = bounds.X + bounds.Width * BaseAnchorPointRate.X * textRun.HorizontalScale * 0.01F;
                var baseAnchorPointY = bounds.Y + bounds.Height * BaseAnchorPointRate.Y * textRun.VerticalScale * 0.01F;
                var skewRad = textRun.SkewAxis / 180.0F * MathF.PI;
                transform *= Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y - bounds.Height) *
                    Matrix3x2.CreateScale(textRun.HorizontalScale * 0.01F, textRun.VerticalScale * 0.01F) *
                    Matrix3x2.CreateTranslation(bounds.X, bounds.Y + bounds.Height);
                var affine = Matrix3x2.CreateTranslation(-baseAnchorPointX - textRun.AnchorPoint.X, -baseAnchorPointY - textRun.AnchorPoint.Y) *
                    Matrix3x2.CreateScale(textRun.Scale.X, textRun.Scale.Y) *
                    Matrix3x2.CreateSkew(MathF.Cos(skewRad) * textRun.Skew, MathF.Sin(skewRad) * textRun.Skew) *
                    Matrix3x2.CreateRotation(textRun.Angle / 180.0F * MathF.PI) *
                    Matrix3x2.CreateTranslation(baseAnchorPointX + textRun.Position.X, baseAnchorPointY + textRun.Position.Y);
                transform *= affine;
            }

            if (TextPath != null)
            {
                var centerX = bounds.Width * 0.5F;
                var posX = bounds.X + centerX + ShiftedLetterSpacing;

                transform *= TextPath.AlignToPath(posX, new Vector2(bounds.X + centerX, bounds.Y - bounds.Top));
            }
            else
            {
                transform *= Matrix3x2.CreateTranslation(ShiftedLetterSpacing, 0.0F);
            }

            var matrix4x4 = new Matrix4x4(
                transform.M11, transform.M12, 0.0F, 0.0F,
                transform.M21, transform.M22, 0.0F, 0.0F,
                0.0F, 0.0F, 1.0F, 0.0F,
                transform.M31, transform.M32, 0.0F, 1.0F
            );

            Builder.SetTransform(matrix4x4);

            // NOTE: 現状行頭かどうかを完全に判定する手段が無いため、LetterSpacingはしばらく無効化
            //ShiftedLetterSpacing += (parameters.TextRun as ExtendedTextRun)?.LetterSpacing ?? 0.0F;

            return true;
        }

        public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds) { }

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

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        {
            if (CurrentIsDiscardGlyph)
            {
                return;
            }

            Builder.AddArc(CurrentPoint, radiusX, radiusY, rotation, largeArc, sweep, point);
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

        public void EndLayer() { }

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

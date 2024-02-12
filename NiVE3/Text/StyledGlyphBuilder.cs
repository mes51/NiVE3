using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace NiVE3.Text
{
    class StyledGlyphBuilder : IGlyphRenderer
    {
        List<GlyphPath> Glyphs { get; } = new List<GlyphPath>();

        Vector2 CurrentPoint { get; set; }

        GlyphRendererParameters CurrentParameters { get; set; }

        PathBuilder Builder { get; }

        (Vector2 first, Vector2 second, float length)[]? TextPathPoints { get; }

        public StyledGlyphBuilder() : this(Matrix3x2.Identity) { }

        public StyledGlyphBuilder(in Matrix3x2 matrix, ISimplePath? textPath = null)
        {
            Builder = new PathBuilder(matrix);
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
            Builder.StartFigure();
        }

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            CurrentParameters = parameters;
            Builder.Clear();

            var letterSpacing = (parameters.TextRun as ExtendedTextRun)?.LetterSpacing * Glyphs.Count ?? 0.0F;
            var transform = Matrix3x2.Identity;
            if (TextPathPoints != null)
            {
                var centerX = bounds.Width * 0.5F;
                var posX = bounds.X + centerX + letterSpacing;
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

                        transform = Matrix3x2.CreateTranslation(pos - location + new Vector2(-centerX, bounds.Top)) * Matrix3x2.CreateRotation(rad - MathF.PI, pos);

                        hit = true;
                    }
                }
            }
            else
            {
                transform = Matrix3x2.CreateTranslation(letterSpacing, 0.0F);
            }

            Builder.SetTransform(transform);

            return true;
        }

        public void BeginText(in FontRectangle bounds) { }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            Builder.AddCubicBezier(CurrentPoint, secondControlPoint, thirdControlPoint, point);
            CurrentPoint = point;
        }

        public TextDecorations EnabledDecorations()
        {
            return CurrentParameters.TextRun.TextDecorations;
        }

        public void EndFigure()
        {
            Builder.CloseFigure();
        }

        public void EndGlyph()
        {
            var textRun = CurrentParameters.TextRun as ExtendedTextRun;
            if (textRun == null)
            {
                textRun = new ExtendedTextRun
                {
                    Start = CurrentParameters.TextRun.Start,
                    End = CurrentParameters.TextRun.End,
                    Font = CurrentParameters.TextRun.Font,
                    TextAttributes = CurrentParameters.TextRun.TextAttributes,
                    TextDecorations = CurrentParameters.TextRun.TextDecorations,
                    VerticalScale = 100.0F,
                    HorizontalScale = 100.0F,
                    TextLineDrawOrder = TextLineDrawOrder.None,
                    FillColor = Vector4.One
                };
            }
            Glyphs.Add(new GlyphPath(Builder.Build(), textRun));
        }

        public void EndText() { }

        public void LineTo(Vector2 point)
        {
            Builder.AddLine(CurrentPoint, point);
            CurrentPoint = point;
        }

        public void MoveTo(Vector2 point)
        {
            Builder.StartFigure();
            CurrentPoint = point;
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            Builder.AddQuadraticBezier(CurrentPoint, secondControlPoint, point);
            CurrentPoint = point;
        }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        {
            // NOTE: 今のところ非対応
            //       対応するのであればTextRunごとに分割して適用する(多分Glyphsに混ぜると困るので別にする必要も有)
        }

        public GlyphPath[] GetGlyphs()
        {
            return Glyphs.ToArray();
        }

        public GlyphPath[] GetRenderableGlyhps()
        {
            return Glyphs.Where(g => g.Path.Bounds.Width > 0.0F && g.Path.Bounds.Height > 0.0F && (g.FlattenedPath.Length > 0 || g.FlattenedOutlinePath.Length > 0)).ToArray();
        }
    }

    record GlyphPath(IPath Path, ExtendedTextRun TextRun)
    {
        public ISimplePath[] FlattenedPath { get; } = Path.Flatten().Where(p => p.Points.Length > 1).ToArray();

        public ISimplePath[] FlattenedOutlinePath { get; } = TextRun.TextLineDrawOrder != TextLineDrawOrder.None && TextRun.TextLineWidth > 0.0F ?
            Path.GenerateOutline(TextRun.TextLineWidth).Flatten().Where(p => p.Points.Length > 1).ToArray() :
            Array.Empty<ISimplePath>();
    }
}

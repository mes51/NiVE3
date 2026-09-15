using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using NiVE3.View.Primitive.PreviewText;
using SixLabors.Fonts;
using SixLabors.Fonts.Rendering;
using SixLabors.ImageSharp.Drawing;
using Rect = System.Windows.Rect;

namespace NiVE3.Text
{
    class CharacterGeometryBuilder : IGlyphRenderer
    {
        Dictionary<int, (string, Matrix3x2, List<Rect>)> CharacterGeometries { get; } = [];

        Vector2 BaseAnchorPointRate { get; }

        GlyphRendererParameters CurrentParameters { get; set; }

        TextLayoutPath? TextPath { get; }

        float TextBoxWidth { get; }

        float TextBoxHeight { get; }

        float ShiftedLetterSpacing { get; set; }

        IReadOnlyDictionary<int, GraphemeCluster> GraphemeClusters { get; }

        public CharacterGeometryBuilder(IReadOnlyDictionary<int, GraphemeCluster> clusters, float textBoxWidth, float textBoxHeight, Vector2 baseAnchorPointRate, TextLayoutPath? textPath = null)
        {
            TextBoxWidth = textBoxWidth;
            TextBoxHeight = textBoxHeight;
            BaseAnchorPointRate = baseAnchorPointRate;
            TextPath = textPath;

            GraphemeClusters = clusters;
        }

        public void BeginFigure() { }

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            CurrentParameters = parameters;

            if ((TextBoxWidth > 0.0F && bounds.X + bounds.Width > TextBoxWidth) || (TextBoxHeight > 0.0F && bounds.Y + bounds.Height > TextBoxHeight))
            {
                return true;
            }

            var transform = Matrix3x2.Identity;

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

            if (GraphemeClusters.TryGetValue(parameters.GraphemeIndex, out var gc))
            {
                if (!CharacterGeometries.TryGetValue(parameters.GraphemeIndex, out var geometry))
                {
                    geometry = (gc.Cluster, transform, []);
                    CharacterGeometries.Add(parameters.GraphemeIndex, geometry);
                }
                geometry.Item3.Add(new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
                return true;
            }
            else
            {
                return false;
            }
        }

        public void BeginText(in FontRectangle bounds)
        {
            ShiftedLetterSpacing = 0.0F;
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point) { }

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point) { }

        public void BeginLayer(Paint? paint, FillRule fillRule) { }

        public void BeginGroup(CompositeMode mode) { }

        public void EndGroup() { }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections) { }

        public TextDecorations EnabledDecorations()
        {
            return CurrentParameters.TextRun.TextDecorations;
        }

        public void EndFigure() { }

        public void EndGlyph() { }

        public void EndLayer() { }

        public void EndText() { }

        public void LineTo(Vector2 point) { }

        public void MoveTo(Vector2 point) { }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point) { }

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness) { }

        public IEnumerable<CharacterGeometry> GetGeometries(PreviewTextCoordTransformer transfomer)
        {
            // 縦書きでは列が左 (文字列の後ろの行) から描画されるなど、描画順は文字列順とは限らない。
            // PreviewTextBox は文字列順のジオメトリを前提とするため、書記素インデックス順に並べる。
            return CharacterGeometries.OrderBy(pair => pair.Key).Select(pair => pair.Value).Select(t => new CharacterGeometry(t.Item1, t.Item3.Aggregate(Rect.Empty, (m, r) =>
            {
                m.Union(r);
                return m;
            }), t.Item2, transfomer));
        }
    }

    record GraphemeCluster(string Cluster, int GraphemeIndex, FontRectangle Advance);
}

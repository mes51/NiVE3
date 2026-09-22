using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Numerics;
using NiVE3.View.Primitive.PreviewText;

namespace NiVE3.Test.View.Primitive.PreviewText
{
    /// <summary>
    /// 文字のパスのバウンディングボックス (インク矩形) ベースの CharacterGeometry に対する
    /// GeometryTextLayout の行検出・キャレット位置・ヒットテスト・選択矩形のテスト
    /// </summary>
    public class GeometryTextLayoutTest
    {
        /// <summary>
        /// ローカル座標 = スクリーン座標とみなす恒等変換
        /// </summary>
        sealed class IdentityTransformer : PreviewTextCoordTransformer
        {
            public IdentityTransformer() : base(null!, null!, null!, default) { }

            public override Vector2d LocalCoordToScreenCoord(Vector3d localCoord)
            {
                return new Vector2d(localCoord.X, localCoord.Y);
            }

            public override Vector3d ScreenCoordToLocalCoord(Vector2d screenPosition, Vector2d scale, Vector2d origin)
            {
                return new Vector3d(screenPosition.X, screenPosition.Y, 0.0);
            }
        }

        static readonly PreviewTextCoordTransformer Transformer = new IdentityTransformer();

        static CharacterGeometry Glyph(string grapheme, double left, double top, double width, double height)
        {
            return new CharacterGeometry(grapheme, new Rect(left, top, width, height), Matrix3x2.Identity, Transformer);
        }

        /// <summary>
        /// "ab cd" を 1 行に並べたインク矩形。文字ごとに上下端が異なり、文字間に隙間があり、空白は幅 0。
        /// a: [10,20] b: [24,34] 空白: x=36 幅 0、c: [40,50] d: [54,64]
        /// </summary>
        static CharacterGeometry[] CreateSingleLineGeometries()
        {
            return
            [
                Glyph("a", 10, 10, 10, 10),
                Glyph("b", 24, 2, 10, 18),
                Glyph(" ", 36, 20, 0, 0),
                Glyph("c", 40, 10, 10, 10),
                Glyph("d", 54, 2, 10, 20),
            ];
        }

        [Test]
        public void TestInkBoundsWithDifferentHeightsStayOnOneLine()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(1));
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 5)));
            });
        }

        [Test]
        public void TestCaretPositionsAreAtGapMidpoints()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());

            Assert.Multiple(() =>
            {
                // 行頭はインクの左端
                Assert.That(layout.GetCaretFlowPosition(0), Is.EqualTo(10.0));
                // a と b の隙間 [20,24] の中点
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(22.0));
                // b と空白の隙間 [34,36] の中点
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(35.0));
                // 空白と c の隙間 [36,40] の中点
                Assert.That(layout.GetCaretFlowPosition(3), Is.EqualTo(38.0));
                Assert.That(layout.GetCaretFlowPosition(4), Is.EqualTo(52.0));
                // 行末はインクの右端
                Assert.That(layout.GetCaretFlowPosition(5), Is.EqualTo(64.0));
            });
        }

        [Test]
        public void TestCaretLineUsesLineInkUnion()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());
            var (top, bottom) = layout.GetCaretLine(1);

            Assert.Multiple(() =>
            {
                Assert.That(top, Is.EqualTo(new Point(22.0, 2.0)));
                Assert.That(bottom, Is.EqualTo(new Point(22.0, 22.0)));
            });
        }

        [Test]
        public void TestHitTestInGapsBetweenInkIsMonotonic()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());

            Assert.Multiple(() =>
            {
                // a のインク中心 (15) より左は a の手前
                Assert.That(layout.GetOffsetAt(new Point(12, 15)), Is.EqualTo(0));
                // a の中心より右、b の中心 (29) より左 (隙間を含む) は a と b の間
                Assert.That(layout.GetOffsetAt(new Point(18, 15)), Is.EqualTo(1));
                Assert.That(layout.GetOffsetAt(new Point(22, 15)), Is.EqualTo(1));
                Assert.That(layout.GetOffsetAt(new Point(26, 15)), Is.EqualTo(1));
                // b の中心より右、空白 (36) より左は b と空白の間
                Assert.That(layout.GetOffsetAt(new Point(33, 15)), Is.EqualTo(2));
                // 空白より右、c の中心 (45) より左は空白と c の間
                Assert.That(layout.GetOffsetAt(new Point(38, 15)), Is.EqualTo(3));
                // d の中心 (59) より右は行末
                Assert.That(layout.GetOffsetAt(new Point(62, 15)), Is.EqualTo(5));
                // 行の外 (上下) でも最も近い行として扱う
                Assert.That(layout.GetOffsetAt(new Point(62, -50)), Is.EqualTo(5));
            });
        }

        [Test]
        public void TestHitTestBeyondLineEndsClampsToLineBoundaries()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());

            Assert.Multiple(() =>
            {
                Assert.That(layout.GetOffsetAt(new Point(-100, 15)), Is.EqualTo(0));
                Assert.That(layout.GetOffsetAt(new Point(1000, 15)), Is.EqualTo(5));
            });
        }

        [Test]
        public void TestRangeQuadsAreContiguousAcrossGaps()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());
            var quads = layout.GetRangeQuads(0, 4);

            Assert.That(quads, Has.Count.EqualTo(1));
            var bounds = quads[0].GetBounds();
            Assert.Multiple(() =>
            {
                // a のセル左端 (インク左端) から c のセル右端 (c と d の隙間の中点) まで
                Assert.That(bounds.Left, Is.EqualTo(10.0));
                Assert.That(bounds.Right, Is.EqualTo(52.0));
                Assert.That(bounds.Top, Is.EqualTo(2.0));
                Assert.That(bounds.Bottom, Is.EqualTo(22.0));
            });
        }

        [Test]
        public void TestRangeQuadForZeroWidthSpaceHasWidth()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());
            var quads = layout.GetRangeQuads(2, 3);

            Assert.That(quads, Has.Count.EqualTo(1));
            var bounds = quads[0].GetBounds();
            Assert.Multiple(() =>
            {
                Assert.That(bounds.Left, Is.EqualTo(35.0));
                Assert.That(bounds.Right, Is.EqualTo(38.0));
            });
        }

        [Test]
        public void TestExplicitNewlineSplitsLines()
        {
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 10),
                Glyph("b", 14, 0, 10, 10),
                Glyph("c", 0, 30, 10, 12),
            ];
            var layout = new GeometryTextLayout("ab\nc", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 2)));
                Assert.That(layout.GetLineRange(1), Is.EqualTo((3, 1)));
                Assert.That(layout.GetLineIndexFromOffset(2), Is.EqualTo(0));
                Assert.That(layout.GetLineIndexFromOffset(3), Is.EqualTo(1));
            });
        }

        [Test]
        public void TestWrappedLineIsDetectedByXMovingBack()
        {
            // 改行文字なしで、c 以降が行頭 (X が戻り、下に移動) に折り返している
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 2, 10, 10),
                Glyph("b", 14, 0, 10, 12),
                Glyph("c", 0, 30, 10, 12),
                Glyph("d", 14, 34, 10, 6),
            ];
            var layout = new GeometryTextLayout("abcd", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 2)));
                Assert.That(layout.GetLineRange(1), Is.EqualTo((2, 2)));
                // 上下移動: 1 行目の b の手前 (X=12) から 2 行目へ
                Assert.That(layout.GetOffsetAtFlowPosition(1, layout.GetCaretFlowPosition(1)), Is.EqualTo(3));
                Assert.That(layout.GetOffsetAt(new Point(3, 36)), Is.EqualTo(2));
                Assert.That(layout.GetOffsetAt(new Point(30, 36)), Is.EqualTo(4));
            });
        }

        [Test]
        public void TestApostropheAboveXHeightDoesNotSplitLine()
        {
            // 縦方向に重ならないインク (上付きの記号) でも X が進んでいれば同じ行
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 10, 10, 10),
                Glyph("'", 12, 0, 3, 5),
                Glyph("b", 18, 0, 10, 20),
            ];
            var layout = new GeometryTextLayout("a'b", geometries);

            Assert.That(layout.LineCount, Is.EqualTo(1));
        }

        [Test]
        public void TestLineWithOnlyZeroHeightInkInheritsHeightFromNeighbor()
        {
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 20),
                Glyph(" ", 0, 40, 0, 0),
                Glyph("b", 0, 60, 10, 20),
            ];
            var layout = new GeometryTextLayout("a\n \nb", geometries);
            var (top, bottom) = layout.GetCaretLine(2);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(3));
                // 1 行目の下に 1 行目と同じ高さで積まれる
                Assert.That(top.Y, Is.EqualTo(20.0));
                Assert.That(bottom.Y, Is.EqualTo(40.0));
            });
        }

        [Test]
        public void TestEmptyRectGeometryIsTreatedAsMissing()
        {
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 10),
                new CharacterGeometry(" ", Rect.Empty, Matrix3x2.Identity, Transformer),
                Glyph("b", 30, 0, 10, 10),
            ];
            var layout = new GeometryTextLayout("a b", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(1));
                // 空白のジオメトリは無いものとして扱い、a と b の隙間の中点が境界になる
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(20.0));
                Assert.That(layout.GetOffsetAt(new Point(15, 5)), Is.EqualTo(1));
                Assert.That(layout.GetOffsetAt(new Point(25, 5)), Is.EqualTo(2));
            });
        }

        [Test]
        public void TestOverlappingInkKeepsCellsOrdered()
        {
            // カーニングでインクが重なる ("AV" など) 場合でもセルが逆転しない
            CharacterGeometry[] geometries =
            [
                Glyph("A", 0, 0, 12, 10),
                Glyph("V", 8, 0, 12, 10),
            ];
            var layout = new GeometryTextLayout("AV", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.GetCaretFlowPosition(0), Is.EqualTo(0.0));
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(10.0));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(20.0));
            });
        }

        [Test]
        public void TestGraphemeWithCarriageReturnMatchesNormalizedText()
        {
            // 本文が "\r\n" 改行だと行末の書記素に '\r' が付くが、表示テキストは '\n' に正規化されている
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 10),
                Glyph("b\r", 14, 0, 10, 10),
                Glyph("c", 0, 30, 10, 10),
                Glyph("d", 14, 30, 10, 10),
            ];
            var layout = new GeometryTextLayout("ab\ncd", geometries);
            var bounds = layout.GetBoundingQuads()[0].GetBounds();

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(24.0));
                Assert.That(layout.GetCaretFlowPosition(5), Is.EqualTo(24.0));
                Assert.That(bounds.Right, Is.EqualTo(26.0));
                Assert.That(bounds.Bottom, Is.EqualTo(42.0));
                Assert.That(layout.GetOffsetAt(new Point(20, 35)), Is.EqualTo(5));
            });
        }

        [Test]
        public void TestCrLfTextIsSplitIntoLinesWithoutNormalization()
        {
            // 本文の "\r\n" は正規化されず 2 文字のまま保持され、1 つの改行として行を区切る
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 10),
                Glyph("b", 14, 0, 10, 10),
                Glyph("c", 0, 30, 10, 10),
                Glyph("d", 14, 30, 10, 10),
            ];
            var layout = new GeometryTextLayout("ab\r\ncd", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                // 1 行目は改行を含まない [0, 2)、2 行目は [4, 6)
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 2)));
                Assert.That(layout.GetLineRange(1), Is.EqualTo((4, 2)));
                Assert.That(layout.GetLineIndexFromOffset(2), Is.EqualTo(0));
                Assert.That(layout.GetLineIndexFromOffset(4), Is.EqualTo(1));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(24.0));
                Assert.That(layout.GetCaretFlowPosition(4), Is.EqualTo(0.0));
                Assert.That(layout.GetOffsetAt(new Point(20, 35)), Is.EqualTo(6));
                // 改行を跨ぐ選択は 2 行分の矩形になる
                Assert.That(layout.GetRangeQuads(1, 5), Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void TestUnmatchedGeometryIsSkippedWithoutLosingFollowingGlyphs()
        {
            // テキストに対応しないエントリが混ざっていても、以降のグリフは照合できる
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 10),
                Glyph("x", 14, 0, 10, 10),
                Glyph("b", 28, 0, 10, 10),
            ];
            var layout = new GeometryTextLayout("ab", geometries);

            Assert.Multiple(() =>
            {
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(19.0));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(38.0));
            });
        }

        [Test]
        public void TestSpaceWithAdvanceWidthAndZeroHeight()
        {
            // SixLabors.Fonts は空白に「幅 = 文字送り、高さ 0」の矩形を返す
            CharacterGeometry[] geometries =
            [
                Glyph("a", 0, 0, 10, 20),
                Glyph(" ", 12, 18, 8, 0),
                Glyph("b", 22, 0, 10, 20),
            ];
            var layout = new GeometryTextLayout("a b", geometries);
            var (top, bottom) = layout.GetCaretLine(1);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(1));
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(11.0));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(21.0));
                Assert.That(top.Y, Is.EqualTo(0.0));
                Assert.That(bottom.Y, Is.EqualTo(20.0));
            });
        }

        /// <summary>
        /// 縦書き "あい\nうえ" のインク矩形 (文字列順)。1 列目 (あ, い) が右 (X 40〜60)、2 列目 (う, え) が左 (X 0〜20)。
        /// </summary>
        static CharacterGeometry[] CreateVerticalGeometries()
        {
            return
            [
                Glyph("あ", 40, 0, 20, 20),
                Glyph("い", 42, 30, 16, 20),
                Glyph("う", 4, 0, 12, 24),
                Glyph("え", 0, 30, 20, 20),
            ];
        }

        [Test]
        public void TestVerticalColumnsAndCaretPositions()
        {
            var layout = new GeometryTextLayout("あい\nうえ", CreateVerticalGeometries(), null, isVertical: true);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 2)));
                Assert.That(layout.GetLineRange(1), Is.EqualTo((3, 2)));
                // 流れ方向 (Y) の位置: 列頭はインクの上端、文字間はインクの隙間の中点、列末はインクの下端
                Assert.That(layout.GetCaretFlowPosition(0), Is.EqualTo(0.0));
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(25.0));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(50.0));
                Assert.That(layout.GetCaretFlowPosition(3), Is.EqualTo(0.0));
                // キャレットは列の幅の水平な線分
                Assert.That(layout.GetCaretLine(0), Is.EqualTo((new Point(40.0, 0.0), new Point(60.0, 0.0))));
                Assert.That(layout.GetCaretLine(1), Is.EqualTo((new Point(40.0, 25.0), new Point(60.0, 25.0))));
                Assert.That(layout.GetCaretLine(3), Is.EqualTo((new Point(0.0, 0.0), new Point(20.0, 0.0))));
            });
        }

        [Test]
        public void TestVerticalHitTest()
        {
            var layout = new GeometryTextLayout("あい\nうえ", CreateVerticalGeometries(), null, isVertical: true);

            Assert.Multiple(() =>
            {
                // 1 列目: あ の中心 (Y=10) より上は あ の手前
                Assert.That(layout.GetOffsetAt(new Point(50, 5)), Is.EqualTo(0));
                // あ の中心より下、い の中心 (Y=40) より上は あ と い の間
                Assert.That(layout.GetOffsetAt(new Point(50, 22)), Is.EqualTo(1));
                Assert.That(layout.GetOffsetAt(new Point(50, 45)), Is.EqualTo(2));
                // 2 列目
                Assert.That(layout.GetOffsetAt(new Point(10, 5)), Is.EqualTo(3));
                Assert.That(layout.GetOffsetAt(new Point(10, 45)), Is.EqualTo(5));
                // 列の隙間は最も近い列で判定する
                Assert.That(layout.GetOffsetAt(new Point(33, 22)), Is.EqualTo(1));
                Assert.That(layout.GetOffsetAt(new Point(25, 5)), Is.EqualTo(3));
                // 列移動: 1 列目の あ と い の間 (Y=25) から 2 列目へ
                Assert.That(layout.GetOffsetAtFlowPosition(1, layout.GetCaretFlowPosition(1)), Is.EqualTo(4));
            });
        }

        [Test]
        public void TestVerticalRangeAndBoundingQuads()
        {
            var layout = new GeometryTextLayout("あい\nうえ", CreateVerticalGeometries(), null, isVertical: true);
            var rangeQuads = layout.GetRangeQuads(0, 2);
            var boundingQuads = layout.GetBoundingQuads();

            Assert.Multiple(() =>
            {
                Assert.That(rangeQuads, Has.Count.EqualTo(1));
                Assert.That(rangeQuads[0].GetBounds(), Is.EqualTo(new Rect(40, 0, 20, 50)));
                Assert.That(boundingQuads, Has.Count.EqualTo(1));
                Assert.That(boundingQuads[0].GetBounds(), Is.EqualTo(new Rect(-2, -2, 64, 54)));
            });
        }

        [Test]
        public void TestVerticalWrappedColumnIsDetectedByYMovingBack()
        {
            // 改行文字なしで、う 以降が左の列 (Y が戻り、X が左へ移動) に折り返している
            CharacterGeometry[] geometries =
            [
                Glyph("あ", 40, 0, 20, 20),
                Glyph("い", 40, 30, 20, 20),
                Glyph("う", 0, 0, 20, 20),
                Glyph("え", 0, 30, 20, 20),
            ];
            var layout = new GeometryTextLayout("あいうえ", geometries, null, isVertical: true);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(2));
                Assert.That(layout.GetLineRange(0), Is.EqualTo((0, 2)));
                Assert.That(layout.GetLineRange(1), Is.EqualTo((2, 2)));
            });
        }

        [Test]
        public void TestVerticalEmptyColumnIsPlacedLeftOfPreviousColumn()
        {
            CharacterGeometry[] geometries =
            [
                Glyph("あ", 40, 0, 20, 20),
                Glyph("い", 0, 0, 20, 20),
            ];
            var layout = new GeometryTextLayout("あ\n\nい", geometries, null, isVertical: true);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(3));
                // 空の列は 1 列目の左に、1 列目と同じ幅で置かれる
                Assert.That(layout.GetCaretLine(2), Is.EqualTo((new Point(20.0, 0.0), new Point(40.0, 0.0))));
            });
        }

        [Test]
        public void TestVerticalSpaceWithZeroWidthAndAdvanceHeight()
        {
            // 縦書きの空白は「幅 0、高さ = 文字送り」の矩形になる
            CharacterGeometry[] geometries =
            [
                Glyph("あ", 40, 0, 20, 20),
                Glyph(" ", 50, 24, 0, 8),
                Glyph("い", 40, 36, 20, 20),
            ];
            var layout = new GeometryTextLayout("あ い", geometries, null, isVertical: true);

            Assert.Multiple(() =>
            {
                Assert.That(layout.LineCount, Is.EqualTo(1));
                Assert.That(layout.GetCaretFlowPosition(1), Is.EqualTo(22.0));
                Assert.That(layout.GetCaretFlowPosition(2), Is.EqualTo(34.0));
                Assert.That(layout.GetCaretLine(1), Is.EqualTo((new Point(40.0, 22.0), new Point(60.0, 22.0))));
            });
        }

        [Test]
        public void TestVerticalEmptyTextCaretUsesWidth()
        {
            var emptyCaret = new CharacterGeometry("", new Rect(0, 0, 32, 0), Matrix3x2.Identity, Transformer);
            var layout = new GeometryTextLayout("", [], emptyCaret, isVertical: true);

            Assert.That(layout.GetCaretLine(0), Is.EqualTo((new Point(0.0, 0.0), new Point(32.0, 0.0))));
        }

        [Test]
        public void TestBoundingQuadsUseInkUnion()
        {
            var layout = new GeometryTextLayout("ab cd", CreateSingleLineGeometries());
            var quads = layout.GetBoundingQuads();

            Assert.That(quads, Has.Count.EqualTo(1));
            var bounds = quads[0].GetBounds();
            Assert.Multiple(() =>
            {
                Assert.That(bounds.Left, Is.EqualTo(8.0));
                Assert.That(bounds.Top, Is.EqualTo(0.0));
                Assert.That(bounds.Right, Is.EqualTo(66.0));
                Assert.That(bounds.Bottom, Is.EqualTo(24.0));
            });
        }
    }
}
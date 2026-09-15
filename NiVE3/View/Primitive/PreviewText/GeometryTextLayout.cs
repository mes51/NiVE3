using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// レンダラ側から渡された CharacterGeometry 配列に基づく ITextLayout 実装。
    ///
    /// 各グリフは「ローカル Bounds (平面レイアウト空間) → GraphemeTransform (Matrix3x2、
    /// パス沿いレイアウトなどの文字単位変換) → レイヤーローカル座標 → PreviewTextCoordTransformer
    /// (ModelView/Projection によるスクリーン座標変換)」のチェーンでスクリーンへ写像される。
    /// スクリーン→レイアウトローカルの逆変換は PreviewTextCoordTransformer.ScreenCoordToLocalCoord と
    /// GraphemeTransform の逆行列で行う。
    ///
    /// Bounds は文字のパス (インク) のバウンディングボックスであり、文字送り幅や行の高さは含まない前提。
    /// そのため、隣接するグリフのインク同士の隙間の中点を境界とする「セル」を行ごとに導出し、
    /// キャレット位置・選択範囲・ヒットテストにはセルを使う。行の太さは行内のインクの union から求める。
    ///
    /// 横書き・縦書きの両方に対応する。文字が進む方向を「流れ方向 (flow)」、行が積まれる方向を
    /// 「直交方向 (cross)」と呼び、横書きでは flow = X / cross = Y (行は下へ)、
    /// 縦書きでは flow = Y / cross = X (列は左へ) として同じ処理を共有する。
    ///
    /// テキスト本体はレンダリング済み画像として外部に表示されている前提で、
    /// このレイアウトはヒットテスト・キャレット・選択範囲・バウンディングボックスの
    /// 計算のみを担当する (DrawsText = false)。
    ///
    /// ジオメトリとテキストの対応付けは、各エントリの Grapheme 文字列を表示テキストと
    /// 順に照合して行う (ジオメトリは文字列順に並んでいる必要がある)。改行や、レンダラが省略した
    /// 空白などはジオメトリなしの「穴」として扱い、前後のグリフの端から位置を補間する。
    /// </summary>
    sealed class GeometryTextLayout : ITextLayout
    {
        sealed class GlyphInfo
        {
            public int Offset { get; set; }

            public int Length { get; set; }

            public int LineIndex { get; set; }

            /// <summary>
            /// 文字のパスのバウンディングボックス (平面レイアウト空間)
            /// </summary>
            public Rect LocalBounds { get; set; }

            /// <summary>
            /// インクの流れ方向の開始位置
            /// </summary>
            public double InkStart { get; set; }

            /// <summary>
            /// インクの流れ方向の終了位置
            /// </summary>
            public double InkEnd { get; set; }

            /// <summary>
            /// インクの直交方向の開始位置
            /// </summary>
            public double InkCrossStart { get; set; }

            /// <summary>
            /// インクの直交方向の終了位置
            /// </summary>
            public double InkCrossEnd { get; set; }

            /// <summary>
            /// セルの流れ方向の開始位置。行内で前のグリフとのインクの隙間の中点。行頭ではインクの開始位置。
            /// </summary>
            public double CellStart { get; set; }

            /// <summary>
            /// セルの流れ方向の終了位置。行内で次のグリフとのインクの隙間の中点。行末ではインクの終了位置。
            /// </summary>
            public double CellEnd { get; set; }

            public Matrix3x2 CharTransform { get; set; }

            public required PreviewTextCoordTransformer Transformer { get; set; }

            /// <summary>
            /// 文字列順の並び方向とローカルの流れ方向 (+flow) の写像方向が逆 (パスの逆順レイアウトなど)。
            /// true の場合、このグリフの「文字列順で手前」の境界はセルの終了側になる。
            /// </summary>
            public bool FlowReversed { get; set; }

            /// <summary>
            /// インクの流れ方向の中心。ヒットテストでキャレットをグリフの前後どちらに置くかの判定に使う。
            /// </summary>
            public double InkCenter => (InkStart + InkEnd) / 2;

            /// <summary>
            /// インクの直交方向の中心
            /// </summary>
            public double InkCrossCenter => (InkCrossStart + InkCrossEnd) / 2;

            /// <summary>
            /// 文字列順で「手前」のキャレット境界 (流れ方向の位置)
            /// </summary>
            public double CaretBefore => FlowReversed ? CellEnd : CellStart;

            /// <summary>
            /// 文字列順で「奥」のキャレット境界 (流れ方向の位置)
            /// </summary>
            public double CaretAfter => FlowReversed ? CellStart : CellEnd;
        }

        sealed class LineInfo
        {
            public int Start { get; set; }

            /// <summary>
            /// 末尾の '\n' を含まない
            /// </summary>
            public int Length { get; set; }

            public List<GlyphInfo> Glyphs { get; } = [];

            // 行ボックス (平面レイアウト空間)。グリフのない行は前後の行から補間する。

            /// <summary>
            /// 行の直交方向の開始位置 (横書きでは上端、縦書きでは左端)
            /// </summary>
            public double CrossStart { get; set; }

            /// <summary>
            /// 行の直交方向の終了位置 (横書きでは下端、縦書きでは右端)
            /// </summary>
            public double CrossEnd { get; set; }

            /// <summary>
            /// 行頭の流れ方向の位置 (グリフのない行のキャレット位置に使う)
            /// </summary>
            public double FlowStart { get; set; }

            public Matrix3x2 CharTransform { get; set; } = Matrix3x2.Identity;

            public PreviewTextCoordTransformer? Transformer { get; set; }

            /// <summary>
            /// 行ボックスの直交方向が確定しているか
            /// </summary>
            public bool Resolved { get; set; }

            /// <summary>
            /// 行の太さ (横書きでは高さ、縦書きでは幅)
            /// </summary>
            public double CrossSize => CrossEnd - CrossStart;

            public double CrossCenter => (CrossStart + CrossEnd) / 2;
        }

        /// <summary>
        /// バウンディングボックスの余白
        /// </summary>
        const double BoundingBoxPadding = 2.0;

        /// <summary>
        /// 折り返し判定 (流れ方向の位置が行頭側へ戻ったか) の許容差
        /// </summary>
        const double WrapDetectionEpsilon = 0.5;

        /// <summary>
        /// 行の太さとして有効とみなす最小値。これ未満の行 (空白だけの行など) は前後の行から補間する。
        /// </summary>
        const double MinLineCrossSize = 1E-3;

        List<LineInfo> Lines { get; } = [];

        List<GlyphInfo> Glyphs { get; } = [];

        CharacterGeometry? EmptyCaret { get; }

        bool IsUniformTransform { get; }

        /// <summary>
        /// 縦書き (文字は下へ進み、列は左へ積まれる) か
        /// </summary>
        public bool IsVertical { get; }

        /// <summary>
        /// 行が積まれる直交方向の符号。横書きは +1 (下へ)、縦書きは -1 (左へ)。
        /// </summary>
        int LineProgression => IsVertical ? -1 : 1;

        public Size Extent { get; private set; }

        public bool DrawsText => false;

        public int LineCount => Lines.Count;

        /// <summary>
        /// キャレット位置を決定できるジオメトリが 1 つも無い場合 true
        /// </summary>
        public bool IsEmpty => Glyphs.Count == 0;

        /// <summary>
        /// レンダリング結果 (仮想スクリーン) の拡大縮小の中心 (PreviewTextBox.Origin)。
        /// ScreenCoordToLocalCoord の origin 引数へ配置情報として渡す。
        /// ヒットテストに渡される座標自体は、呼び出し側 (PreviewTextBox) で配置を取り除いた
        /// 仮想スクリーン座標へ変換済みのものを受け取る。
        /// </summary>
        public Vector2d ViewOrigin { get; set; }

        /// <summary>
        /// レンダリング結果のコントロール内での表示倍率 (PreviewTextBox.Scale)
        /// </summary>
        public Vector2d ViewScale { get; set; } = Vector2d.One;

        /// <param name="text">表示テキスト</param>
        /// <param name="geometries">レンダラが生成したグリフごとのジオメトリ (文字列順)</param>
        /// <param name="emptyCaretGeometry">
        /// テキストが空 (ジオメトリなし) のときに使うキャレット位置。
        /// Bounds はローカル空間でのキャレット矩形 (横書きは幅 0 で高さを、縦書きは高さ 0 で幅を使う)。
        /// </param>
        /// <param name="isVertical">縦書きか</param>
        public GeometryTextLayout(string text, IReadOnlyList<CharacterGeometry> geometries, CharacterGeometry? emptyCaretGeometry = null, bool isVertical = false)
        {
            EmptyCaret = emptyCaretGeometry;
            IsVertical = isVertical;
            AssignGlyphs(text, geometries);
            BuildVisualLines(text);
            ComputeCells();
            ResolveLineMetrics();
            ComputeFlowDirections();
            ComputeExtent();
            IsUniformTransform = ComputeUniformTransform();
        }

        public void Dispose()
        {
        }

        #region 座標軸

        /// <summary>
        /// 平面レイアウト空間の点の流れ方向の成分
        /// </summary>
        double Flow(Point point)
        {
            return IsVertical ? point.Y : point.X;
        }

        /// <summary>
        /// 平面レイアウト空間の点の直交方向の成分
        /// </summary>
        double Cross(Point point)
        {
            return IsVertical ? point.X : point.Y;
        }

        /// <summary>
        /// 流れ方向・直交方向の成分から平面レイアウト空間の点を作る
        /// </summary>
        Point MakePoint(double flow, double cross)
        {
            return IsVertical ? new Point(cross, flow) : new Point(flow, cross);
        }

        /// <summary>
        /// 流れ方向・直交方向の範囲から平面レイアウト空間の矩形を作る
        /// </summary>
        Rect MakeRect(double flowStart, double flowEnd, double crossStart, double crossEnd)
        {
            var flowSize = Math.Max(0, flowEnd - flowStart);
            var crossSize = Math.Max(0, crossEnd - crossStart);
            return IsVertical
                ? new Rect(crossStart, flowStart, crossSize, flowSize)
                : new Rect(flowStart, crossStart, flowSize, crossSize);
        }

        double FlowStart(Rect rect)
        {
            return IsVertical ? rect.Top : rect.Left;
        }

        double FlowEnd(Rect rect)
        {
            return IsVertical ? rect.Bottom : rect.Right;
        }

        double CrossStart(Rect rect)
        {
            return IsVertical ? rect.Left : rect.Top;
        }

        double CrossEnd(Rect rect)
        {
            return IsVertical ? rect.Right : rect.Bottom;
        }

        #endregion 座標軸

        #region 構築

        void AssignGlyphs(string text, IReadOnlyList<CharacterGeometry> geometries)
        {
            var geometryIndex = 0;
            var position = 0;
            while (position < text.Length && geometryIndex < geometries.Count)
            {
                if (text[position] == '\n')
                {
                    position++;
                    continue;
                }

                var geometry = geometries[geometryIndex];
                var cluster = NormalizeCluster(geometry.Grapheme);
                if (cluster.Length == 0)
                {
                    // 改行だけのエントリ (レンダラが含めた場合) は読み飛ばす
                    geometryIndex++;
                    continue;
                }

                var matchIndex = FindClusterInLine(text, position, cluster);
                if (matchIndex < 0)
                {
                    // 現在の行にこの書記素が無い (テキストと対応しないエントリ)。
                    // ここで止まると以降のジオメトリがすべて失われるため、ジオメトリ側を読み飛ばして再同期する
                    geometryIndex++;
                    continue;
                }

                // matchIndex までのテキスト要素はジオメトリなし (空白の省略など) として扱う
                position = matchIndex;
                // Rect.Empty (パスを持たないグリフ) は位置情報が無いため、ジオメトリなしとして扱う
                if (!geometry.Bounds.IsEmpty)
                {
                    Glyphs.Add(new GlyphInfo
                    {
                        Offset = position,
                        Length = cluster.Length,
                        LocalBounds = geometry.Bounds,
                        InkStart = FlowStart(geometry.Bounds),
                        InkEnd = FlowEnd(geometry.Bounds),
                        InkCrossStart = CrossStart(geometry.Bounds),
                        InkCrossEnd = CrossEnd(geometry.Bounds),
                        CharTransform = geometry.GraphemeTransform,
                        Transformer = geometry.Transformer,
                    });
                }
                geometryIndex++;
                position += cluster.Length;
            }
        }

        /// <summary>
        /// レンダラ側の書記素から改行文字を取り除く。
        /// テキストレイヤーの本文が "\r\n" 改行のとき、行末の書記素に '\r' が含まれることがあるが、
        /// 表示テキスト側は '\n' に正規化されているため、そのままでは照合できない。
        /// </summary>
        static string NormalizeCluster(string? grapheme)
        {
            if (string.IsNullOrEmpty(grapheme))
            {
                return "";
            }
            return grapheme.Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// position 以降の同じ行 (次の '\n' の手前まで) から、テキスト要素境界に一致する cluster の位置を探す。
        /// 見つからなければ -1。
        /// </summary>
        static int FindClusterInLine(string text, int position, string cluster)
        {
            var newlineIndex = text.IndexOf('\n', position);
            var lineEnd = newlineIndex < 0 ? text.Length : newlineIndex;
            var index = position;
            while (index < lineEnd)
            {
                if (index + cluster.Length <= lineEnd && string.CompareOrdinal(text, index, cluster, 0, cluster.Length) == 0)
                {
                    return index;
                }
                index += Math.Max(1, StringInfo.GetNextTextElementLength(text, index));
            }
            return -1;
        }

        /// <summary>
        /// 視覚行を構築する。改行 ('\n') に加えて、WrappingLength による折り返し
        /// (1 論理行が複数の視覚行になる) を検出して行を区切る。
        /// Bounds はインクのバウンディングボックスで文字ごとに端の位置が異なるため、直交方向の変化では判定できない。
        /// 代わりに「流れ方向の位置が直前のグリフより行頭側へ戻り、かつ次の行の側へ移動した」ことを折り返しとみなす。
        /// パスに沿ったレイアウトでもローカル Bounds はパス適用前の平面レイアウトのため、この判定が機能する。
        /// </summary>
        void BuildVisualLines(string text)
        {
            var lineStart = 0;
            // このインデックスまでの改行は処理済み
            var newlineScan = 0;
            LineInfo? current = null;

            void CloseLine(int end)
            {
                var line = current ?? new LineInfo();
                line.Start = lineStart;
                line.Length = end - lineStart;
                Lines.Add(line);
                current = null;
            }

            foreach (var glyph in Glyphs)
            {
                // このグリフより前にある改行で行を区切る
                for (; newlineScan < glyph.Offset; newlineScan++)
                {
                    if (text[newlineScan] == '\n')
                    {
                        CloseLine(newlineScan);
                        lineStart = newlineScan + 1;
                    }
                }

                // 折り返し: 同じ論理行内で行頭側へ戻ったら新しい視覚行
                if (current != null && IsWrappedLineStart(current.Glyphs[^1], glyph))
                {
                    CloseLine(glyph.Offset);
                    lineStart = glyph.Offset;
                }

                current ??= new LineInfo();
                // CloseLine 時にこの行が得る index
                glyph.LineIndex = Lines.Count;
                current.Glyphs.Add(glyph);
                newlineScan = Math.Max(newlineScan, glyph.Offset + glyph.Length);
            }

            // 末尾に残った改行と最終行
            for (; newlineScan < text.Length; newlineScan++)
            {
                if (text[newlineScan] == '\n')
                {
                    CloseLine(newlineScan);
                    lineStart = newlineScan + 1;
                }
            }
            CloseLine(text.Length);
        }

        /// <summary>
        /// 文字列順で連続する 2 つのグリフの間で折り返しが起きているかを判定する
        /// </summary>
        bool IsWrappedLineStart(GlyphInfo previous, GlyphInfo next)
        {
            var movedBack = next.InkStart < previous.InkStart - WrapDetectionEpsilon;
            var movedToNextLine = ((next.InkCrossCenter - previous.InkCrossCenter) * LineProgression) > 0;
            return movedBack && movedToNextLine;
        }

        /// <summary>
        /// 行ごとにセル (キャレット境界) を求める。隣接するグリフの間ではインクの隙間の中点を境界とし、
        /// 行頭・行末ではインクの端をそのまま使う。
        /// 空白のように流れ方向の幅が 0 のインクしか無いグリフも、前後との中点で幅のあるセルになる。
        /// </summary>
        void ComputeCells()
        {
            foreach (var line in Lines)
            {
                var glyphs = line.Glyphs;
                for (var i = 0; i < glyphs.Count; i++)
                {
                    var glyph = glyphs[i];
                    glyph.CellStart = i == 0
                        ? glyph.InkStart
                        : glyphs[i - 1].CellEnd;
                    if (i + 1 < glyphs.Count)
                    {
                        // カーニングなどでインクが重なる場合も中点を採用し、セルが逆転しないようにする
                        var boundary = (glyph.InkEnd + glyphs[i + 1].InkStart) / 2;
                        glyph.CellEnd = Math.Max(boundary, glyph.CellStart);
                    }
                    else
                    {
                        glyph.CellEnd = Math.Max(glyph.InkEnd, glyph.CellStart);
                    }
                }
            }
        }

        void ResolveLineMetrics()
        {
            // グリフのある行はグリフの union から行ボックスを求める
            foreach (var line in Lines)
            {
                if (line.Glyphs.Count == 0)
                {
                    continue;
                }
                var crossStart = double.MaxValue;
                var crossEnd = double.MinValue;
                foreach (var glyph in line.Glyphs)
                {
                    crossStart = Math.Min(crossStart, glyph.InkCrossStart);
                    crossEnd = Math.Max(crossEnd, glyph.InkCrossEnd);
                }
                line.CrossStart = crossStart;
                line.CrossEnd = crossEnd;
                line.FlowStart = line.Glyphs[0].CellStart;
                line.CharTransform = line.Glyphs[0].CharTransform;
                line.Transformer = line.Glyphs[0].Transformer;
                // 太さ 0 のインクしか無い行 (空白のみなど) は太さを前後の行から補間する
                line.Resolved = line.CrossSize >= MinLineCrossSize;
            }

            if (!Lines.Any(line => line.Resolved))
            {
                return;
            }

            // 前方パス: 未確定の行を直前の行の次に積む
            var lastSize = Lines.First(line => line.Resolved).CrossSize;
            for (var i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (line.Resolved)
                {
                    lastSize = line.CrossSize;
                    continue;
                }
                if (i > 0 && Lines[i - 1].Resolved)
                {
                    var previous = Lines[i - 1];
                    var crossStart = LineProgression > 0 ? previous.CrossEnd : previous.CrossStart - lastSize;
                    InheritLineMetrics(line, previous, crossStart, lastSize);
                }
            }

            // 後方パス: 先頭側に残った未確定の行を次の行の手前に積む
            for (var i = Lines.Count - 2; i >= 0; i--)
            {
                var line = Lines[i];
                if (line.Resolved)
                {
                    continue;
                }
                var next = Lines[i + 1];
                if (!next.Resolved)
                {
                    continue;
                }
                var crossStart = LineProgression > 0 ? next.CrossStart - next.CrossSize : next.CrossEnd;
                InheritLineMetrics(line, next, crossStart, next.CrossSize);
            }
        }

        /// <summary>
        /// 未確定の行に、隣接する行から直交方向の位置と太さを引き継ぐ。
        /// グリフを持たない行は行頭位置とトランスフォームも引き継ぐ。
        /// </summary>
        static void InheritLineMetrics(LineInfo line, LineInfo source, double crossStart, double crossSize)
        {
            line.CrossStart = crossStart;
            line.CrossEnd = crossStart + crossSize;
            if (line.Glyphs.Count == 0)
            {
                line.FlowStart = source.FlowStart;
                line.CharTransform = source.CharTransform;
                line.Transformer = source.Transformer;
            }
            line.Resolved = true;
        }

        /// <summary>
        /// グリフごとに、文字列順の並び方向とローカルの流れ方向の写像方向が一致しているかを判定する。
        /// パスの逆順レイアウト (isInvert) では、配置はパスを逆走する一方でグリフは正立のままのため、
        /// 文字列順で「次」のグリフが流れ方向の反対側に並ぶ。この場合、キャレット境界や
        /// ヒットテストの前後判定を反転させる必要がある。
        /// </summary>
        void ComputeFlowDirections()
        {
            foreach (var line in Lines)
            {
                var glyphs = line.Glyphs;
                for (var i = 0; i < glyphs.Count; i++)
                {
                    var glyph = glyphs[i];
                    GlyphInfo neighbor;
                    var sign = 1.0;
                    if (i + 1 < glyphs.Count)
                    {
                        neighbor = glyphs[i + 1];
                    }
                    else if (i > 0)
                    {
                        neighbor = glyphs[i - 1];
                        sign = -1.0;
                    }
                    else
                    {
                        // 行に 1 グリフのみ: 判定不能 (通常向きとみなす)
                        continue;
                    }

                    var glyphCenter = ProjectPoint(glyph.CharTransform, glyph.Transformer, MakePoint(glyph.InkCenter, line.CrossCenter));
                    var neighborCenter = ProjectPoint(neighbor.CharTransform, neighbor.Transformer, MakePoint(neighbor.InkCenter, line.CrossCenter));
                    var deltaX = (neighborCenter.X - glyphCenter.X) * sign;
                    var deltaY = (neighborCenter.Y - glyphCenter.Y) * sign;

                    // 流れ方向のスクリーン写像 (有限差分)
                    var unitFlow = ProjectPoint(glyph.CharTransform, glyph.Transformer, MakePoint(glyph.InkCenter + 1, line.CrossCenter));
                    var directionX = unitFlow.X - glyphCenter.X;
                    var directionY = unitFlow.Y - glyphCenter.Y;

                    glyph.FlowReversed = (directionX * deltaX) + (directionY * deltaY) < 0;
                }
            }
        }

        void ComputeExtent()
        {
            var maxX = 0.0;
            var maxY = 0.0;
            foreach (var glyph in Glyphs)
            {
                var bounds = ProjectRect(glyph.CharTransform, glyph.Transformer, glyph.LocalBounds).GetBounds();
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }
            Extent = new Size(Math.Max(0, maxX), Math.Max(0, maxY));
        }

        bool ComputeUniformTransform()
        {
            if (Glyphs.Count == 0)
            {
                return true;
            }
            var first = Glyphs[0];
            foreach (var glyph in Glyphs)
            {
                if (!SameTransform(glyph, first))
                {
                    return false;
                }
            }
            return true;
        }

        static bool SameTransform(GlyphInfo a, GlyphInfo b)
        {
            return a.CharTransform == b.CharTransform && Equals(a.Transformer, b.Transformer);
        }

        #endregion 構築

        #region 座標変換

        /// <summary>
        /// 平面レイアウト空間の点をスクリーン座標へ写像する
        /// </summary>
        static Point ProjectPoint(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, Point local)
        {
            var layerLocal = Vector2.Transform(new Vector2((float)local.X, (float)local.Y), charTransform);
            var screen = transformer.LocalCoordToScreenCoord(new Vector3d(layerLocal.X, layerLocal.Y, 0.0));
            return new Point(screen.X, screen.Y);
        }

        static TextQuad ProjectRect(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, Rect rect)
        {
            return new TextQuad(
                ProjectPoint(charTransform, transformer, rect.TopLeft),
                ProjectPoint(charTransform, transformer, rect.TopRight),
                ProjectPoint(charTransform, transformer, rect.BottomRight),
                ProjectPoint(charTransform, transformer, rect.BottomLeft));
        }

        /// <summary>
        /// グリフのセル (行の太さ × セルの長さ) を平面レイアウト空間の矩形として返す
        /// </summary>
        Rect GetCellRect(GlyphInfo glyph)
        {
            var line = Lines[glyph.LineIndex];
            return MakeRect(glyph.CellStart, glyph.CellEnd, line.CrossStart, line.CrossEnd);
        }

        /// <summary>
        /// グリフのインクの流れ方向の範囲 × 行の太さ を平面レイアウト空間の矩形として返す
        /// </summary>
        Rect GetInkLineRect(GlyphInfo glyph)
        {
            var line = Lines[glyph.LineIndex];
            return MakeRect(glyph.InkStart, glyph.InkEnd, line.CrossStart, line.CrossEnd);
        }

        /// <summary>
        /// スクリーン座標の点を平面レイアウト空間へ逆写像する
        /// </summary>
        bool TryUnproject(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, Point screen, out Point layoutLocal)
        {
            if (!Matrix3x2.Invert(charTransform, out var inverted))
            {
                layoutLocal = default;
                return false;
            }

            // scale / origin にはレンダリング結果のコントロール内での配置 (表示倍率と拡大縮小の中心) を渡す。
            // screen はこの配置を取り除いた仮想スクリーン座標に変換済みで受け取る。
            var layerLocal = transformer.ScreenCoordToLocalCoord(new Vector2d(screen.X, screen.Y), ViewScale, ViewOrigin);
            var point = Vector2.Transform(new Vector2((float)layerLocal.X, (float)layerLocal.Y), inverted);
            layoutLocal = new Point(point.X, point.Y);
            return true;
        }

        /// <summary>
        /// 指定点周りの有限差分による、平面レイアウト空間→スクリーンのアフィン近似
        /// </summary>
        static Matrix ApproximateScreenTransform(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, Point local)
        {
            var origin = ProjectPoint(charTransform, transformer, local);
            var unitX = ProjectPoint(charTransform, transformer, new Point(local.X + 1, local.Y));
            var unitY = ProjectPoint(charTransform, transformer, new Point(local.X, local.Y + 1));
            var jacobianXX = unitX.X - origin.X;
            var jacobianXY = unitX.Y - origin.Y;
            var jacobianYX = unitY.X - origin.X;
            var jacobianYY = unitY.Y - origin.Y;
            return new Matrix(
                jacobianXX, jacobianXY, jacobianYX, jacobianYY,
                origin.X - (jacobianXX * local.X) - (jacobianYX * local.Y),
                origin.Y - (jacobianXY * local.X) - (jacobianYY * local.Y));
        }

        #endregion 座標変換

        #region 行・オフセット

        public int GetLineIndexFromOffset(int offset)
        {
            var low = 0;
            var high = Lines.Count - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (Lines[middle].Start <= offset)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            return low;
        }

        public (int Start, int Length) GetLineRange(int lineIndex)
        {
            var line = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
            return (line.Start, line.Length);
        }

        /// <summary>
        /// キャレットのローカル位置 (流れ方向の位置・行・使用するトランスフォーム) を求める
        /// </summary>
        (double Flow, LineInfo Line, Matrix3x2 CharTransform, PreviewTextCoordTransformer? Transformer) GetCaretLocal(int offset)
        {
            var line = Lines[GetLineIndexFromOffset(offset)];

            if (line.Glyphs.Count == 0)
            {
                return (line.FlowStart, line, line.CharTransform, line.Transformer);
            }

            foreach (var glyph in line.Glyphs)
            {
                // クラスタ内部はクラスタ先頭にスナップ
                if (offset <= glyph.Offset || offset < glyph.Offset + glyph.Length)
                {
                    return (glyph.CaretBefore, line, glyph.CharTransform, glyph.Transformer);
                }
            }

            var last = line.Glyphs[^1];
            return (last.CaretAfter, line, last.CharTransform, last.Transformer);
        }

        /// <summary>
        /// 空テキスト用のキャレットが有効 (直交方向の太さを持つ) か
        /// </summary>
        bool HasEmptyCaret => EmptyCaret != null && CrossEnd(EmptyCaret.Bounds) - CrossStart(EmptyCaret.Bounds) > 0;

        public double GetCaretFlowPosition(int offset)
        {
            if (IsEmpty)
            {
                return EmptyCaret != null ? FlowStart(EmptyCaret.Bounds) : 0;
            }
            return GetCaretLocal(offset).Flow;
        }

        public (Point Top, Point Bottom) GetCaretLine(int offset)
        {
            if (IsEmpty)
            {
                if (HasEmptyCaret)
                {
                    var caret = EmptyCaret!;
                    var flow = FlowStart(caret.Bounds);
                    return (ProjectPoint(caret.GraphemeTransform, caret.Transformer, MakePoint(flow, CrossStart(caret.Bounds))),
                            ProjectPoint(caret.GraphemeTransform, caret.Transformer, MakePoint(flow, CrossEnd(caret.Bounds))));
                }
                return (default, default);
            }

            var (flowPosition, line, charTransform, transformer) = GetCaretLocal(offset);
            if (transformer == null)
            {
                return (default, default);
            }
            return (ProjectPoint(charTransform, transformer, MakePoint(flowPosition, line.CrossStart)),
                    ProjectPoint(charTransform, transformer, MakePoint(flowPosition, line.CrossEnd)));
        }

        public bool TryGetCaretLocalFrame(int offset, out Point localPosition, out double localHeight, out Matrix transform)
        {
            if (IsEmpty)
            {
                if (HasEmptyCaret)
                {
                    var caret = EmptyCaret!;
                    localPosition = MakePoint(FlowStart(caret.Bounds), CrossStart(caret.Bounds));
                    localHeight = CrossEnd(caret.Bounds) - CrossStart(caret.Bounds);
                    transform = ApproximateScreenTransform(caret.GraphemeTransform, caret.Transformer, localPosition);
                    return true;
                }
                localPosition = default;
                localHeight = 0;
                transform = Matrix.Identity;
                return false;
            }

            var (flowPosition, line, charTransform, transformer) = GetCaretLocal(offset);
            if (transformer == null)
            {
                localPosition = default;
                localHeight = 0;
                transform = Matrix.Identity;
                return false;
            }
            localPosition = MakePoint(flowPosition, line.CrossStart);
            localHeight = line.CrossSize;
            transform = ApproximateScreenTransform(charTransform, transformer, localPosition);
            return true;
        }

        public int GetOffsetAtFlowPosition(int lineIndex, double flowPosition)
        {
            var line = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
            foreach (var glyph in line.Glyphs)
            {
                if (flowPosition < glyph.InkCenter)
                {
                    return glyph.Offset;
                }
            }
            return line.Start + line.Length;
        }

        public int GetOffsetAt(Point point)
        {
            if (IsEmpty)
            {
                return 0;
            }

            // 1) グリフのセル Quad への直接ヒット (グリフごとにトランスフォームが違っても正確)。
            //    閉じたパスやループでは文字同士が重なることがあるため、
            //    ヒットした中で中心が最も近いグリフを採用する。
            GlyphInfo? hitGlyph = null;
            var hitDistance = double.MaxValue;
            foreach (var glyph in Glyphs)
            {
                var quad = ProjectRect(glyph.CharTransform, glyph.Transformer, GetCellRect(glyph));
                if (!quad.Contains(point))
                {
                    continue;
                }

                var distance = DistanceSquaredToCenter(quad.GetBounds(), point);
                if (distance < hitDistance)
                {
                    hitDistance = distance;
                    hitGlyph = glyph;
                }
            }

            if (hitGlyph != null)
            {
                return GetOffsetAroundGlyph(hitGlyph, point);
            }

            // 2) フォールバック
            if (IsUniformTransform)
            {
                // 全グリフが同一トランスフォーム: 逆変換して最も近い行内の位置を返す
                // (空行にもヒットさせられるよう、行ベースで判定する)
                var representative = Glyphs[0];
                if (!TryUnproject(representative.CharTransform, representative.Transformer, point, out var local))
                {
                    return 0;
                }

                var cross = Cross(local);
                LineInfo? bestLine = null;
                var bestDistance = double.MaxValue;
                foreach (var line in Lines)
                {
                    if (!line.Resolved)
                    {
                        continue;
                    }

                    var distance = cross < line.CrossStart
                        ? line.CrossStart - cross
                        : cross > line.CrossEnd ? cross - line.CrossEnd : 0.0;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestLine = line;
                    }
                }

                if (bestLine == null)
                {
                    return 0;
                }
                return GetOffsetAtFlowPosition(Lines.IndexOf(bestLine), Flow(local));
            }

            // パスに沿ったレイアウトなど、グリフごとにトランスフォームが異なる場合は
            // スクリーン空間で最も近いグリフを探し、そのローカル空間で前後を判定する
            GlyphInfo? bestGlyph = null;
            var bestGlyphDistance = double.MaxValue;
            foreach (var glyph in Glyphs)
            {
                var quad = ProjectRect(glyph.CharTransform, glyph.Transformer, GetCellRect(glyph));
                var distance = DistanceSquaredToCenter(quad.GetBounds(), point);
                if (distance < bestGlyphDistance)
                {
                    bestGlyphDistance = distance;
                    bestGlyph = glyph;
                }
            }

            if (bestGlyph == null)
            {
                return 0;
            }
            return GetOffsetAroundGlyph(bestGlyph, point);
        }

        static double DistanceSquaredToCenter(Rect bounds, Point point)
        {
            var deltaX = point.X - ((bounds.Left + bounds.Right) / 2);
            var deltaY = point.Y - ((bounds.Top + bounds.Bottom) / 2);
            return (deltaX * deltaX) + (deltaY * deltaY);
        }

        /// <summary>
        /// グリフのローカル空間で点がインクの中心の手前か奥かを判定し、対応するオフセットを返す
        /// </summary>
        int GetOffsetAroundGlyph(GlyphInfo glyph, Point point)
        {
            var line = Lines[glyph.LineIndex];
            if (TryUnproject(glyph.CharTransform, glyph.Transformer, point, out var local))
            {
                var before = Flow(local) < glyph.InkCenter;
                if (glyph.FlowReversed)
                {
                    before = !before;
                }
                return before
                    ? glyph.Offset
                    : Math.Min(glyph.Offset + glyph.Length, line.Start + line.Length);
            }
            return glyph.Offset;
        }

        #endregion 行・オフセット

        #region Quad

        public IReadOnlyList<TextQuad> GetRangeQuads(int start, int end)
        {
            var quads = new List<TextQuad>();
            if (end <= start || IsEmpty)
            {
                return quads;
            }

            foreach (var line in Lines)
            {
                var lineEnd = line.Start + line.Length;
                if (lineEnd < start)
                {
                    continue;
                }
                if (line.Start > end)
                {
                    break;
                }
                if (!line.Resolved)
                {
                    continue;
                }

                // 同一トランスフォームで連続するグリフのセルは 1 つの矩形にまとめる
                // (セルは隣接グリフと接しているため、選択範囲が隙間なく塗られる)
                var runStart = 0.0;
                var runEnd = 0.0;
                var runCharTransform = Matrix3x2.Identity;
                PreviewTextCoordTransformer? runTransformer = null;
                var runOpen = false;

                void Flush()
                {
                    if (!runOpen || runTransformer == null)
                    {
                        return;
                    }
                    quads.Add(ProjectRect(runCharTransform, runTransformer, MakeRect(runStart, runEnd, line.CrossStart, line.CrossEnd)));
                    runOpen = false;
                }

                foreach (var glyph in line.Glyphs)
                {
                    if (glyph.Offset + glyph.Length <= start)
                    {
                        continue;
                    }
                    if (glyph.Offset >= end)
                    {
                        break;
                    }

                    if (runOpen && glyph.CharTransform == runCharTransform && Equals(glyph.Transformer, runTransformer))
                    {
                        runStart = Math.Min(runStart, glyph.CellStart);
                        runEnd = Math.Max(runEnd, glyph.CellEnd);
                    }
                    else
                    {
                        Flush();
                        runStart = glyph.CellStart;
                        runEnd = glyph.CellEnd;
                        runCharTransform = glyph.CharTransform;
                        runTransformer = glyph.Transformer;
                        runOpen = true;
                    }
                }

                // 選択が改行を越えて続く場合のマーカー (行末のグリフと同じトランスフォームで描く)
                if (end > lineEnd && start <= lineEnd)
                {
                    var markerLength = line.CrossSize * 0.35;
                    var markerStart = line.FlowStart;
                    var markerCharTransform = line.CharTransform;
                    var markerTransformer = line.Transformer;
                    if (line.Glyphs.Count > 0)
                    {
                        var lastGlyph = line.Glyphs[^1];
                        markerCharTransform = lastGlyph.CharTransform;
                        markerTransformer = lastGlyph.Transformer;
                        // フロー反転時は行末の視覚的な「続き」はセルの開始側
                        markerStart = lastGlyph.FlowReversed
                            ? lastGlyph.CellStart - markerLength
                            : lastGlyph.CellEnd;
                    }

                    if (markerTransformer != null)
                    {
                        if (runOpen && runCharTransform == markerCharTransform && Equals(runTransformer, markerTransformer))
                        {
                            runStart = Math.Min(runStart, markerStart);
                            runEnd = Math.Max(runEnd, markerStart + markerLength);
                        }
                        else
                        {
                            Flush();
                            quads.Add(ProjectRect(markerCharTransform, markerTransformer,
                                MakeRect(markerStart, markerStart + markerLength, line.CrossStart, line.CrossEnd)));
                        }
                    }
                }

                Flush();
            }
            return quads;
        }

        public IReadOnlyList<TextQuad> GetBoundingQuads()
        {
            if (IsEmpty)
            {
                return Array.Empty<TextQuad>();
            }

            // 全グリフが同一トランスフォームなら、平面レイアウト空間の union を 1 つの Quad として返す
            if (IsUniformTransform)
            {
                var flowStart = double.MaxValue;
                var flowEnd = double.MinValue;
                var crossStart = double.MaxValue;
                var crossEnd = double.MinValue;
                foreach (var line in Lines)
                {
                    if (!line.Resolved || line.Glyphs.Count == 0)
                    {
                        continue;
                    }
                    crossStart = Math.Min(crossStart, line.CrossStart);
                    crossEnd = Math.Max(crossEnd, line.CrossEnd);
                    foreach (var glyph in line.Glyphs)
                    {
                        flowStart = Math.Min(flowStart, glyph.InkStart);
                        flowEnd = Math.Max(flowEnd, glyph.InkEnd);
                    }
                }
                if (flowStart == double.MaxValue)
                {
                    return Array.Empty<TextQuad>();
                }
                var union = MakeRect(flowStart - BoundingBoxPadding, flowEnd + BoundingBoxPadding, crossStart - BoundingBoxPadding, crossEnd + BoundingBoxPadding);
                var representative = Glyphs[0];
                return [ProjectRect(representative.CharTransform, representative.Transformer, union)];
            }

            // トランスフォームが混在する場合はスクリーン空間の AABB
            var aabb = Rect.Empty;
            foreach (var glyph in Glyphs)
            {
                aabb.Union(ProjectRect(glyph.CharTransform, glyph.Transformer, GetInkLineRect(glyph)).GetBounds());
            }
            aabb.Inflate(BoundingBoxPadding, BoundingBoxPadding);
            return [new TextQuad(aabb.TopLeft, aabb.TopRight, aabb.BottomRight, aabb.BottomLeft)];
        }

        #endregion Quad

        public void DrawText(DrawingContext drawingContext, Point origin)
        {
            // テキスト本体は外部でレンダリング済み (DrawsText = false)
        }
    }
}
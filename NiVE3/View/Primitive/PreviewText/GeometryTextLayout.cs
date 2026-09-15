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
    /// キャレット位置・選択範囲・ヒットテストにはセルを使う。行の縦幅は行内のインクの union から求める。
    ///
    /// テキスト本体はレンダリング済み画像として外部に表示されている前提で、
    /// このレイアウトはヒットテスト・キャレット・選択範囲・バウンディングボックスの
    /// 計算のみを担当する (DrawsText = false)。
    ///
    /// ジオメトリとテキストの対応付けは、各エントリの Grapheme 文字列を表示テキストと
    /// 順に照合して行う。改行や、レンダラが省略した空白などはジオメトリなしの「穴」として
    /// 扱い、前後のグリフの端から位置を補間する。
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
            /// セルの左端 (平面レイアウト空間)。行内で前のグリフとのインクの隙間の中点。行頭ではインクの左端。
            /// </summary>
            public double CellLeft { get; set; }

            /// <summary>
            /// セルの右端 (平面レイアウト空間)。行内で次のグリフとのインクの隙間の中点。行末ではインクの右端。
            /// </summary>
            public double CellRight { get; set; }

            public Matrix3x2 CharTransform { get; set; }

            public required PreviewTextCoordTransformer Transformer { get; set; }

            /// <summary>
            /// 文字列順の並び方向とローカル +X の写像方向が逆 (パスの逆順レイアウトなど)。
            /// true の場合、このグリフの「文字列順で手前」の境界はローカル Right 側になる。
            /// </summary>
            public bool FlowReversed { get; set; }

            /// <summary>
            /// インクの中心 X。ヒットテストでキャレットをグリフの前後どちらに置くかの判定に使う。
            /// </summary>
            public double InkCenterX => (LocalBounds.Left + LocalBounds.Right) / 2;

            /// <summary>
            /// 文字列順で「手前」のキャレット境界 (平面レイアウト空間)
            /// </summary>
            public double CaretBefore => FlowReversed ? CellRight : CellLeft;

            /// <summary>
            /// 文字列順で「奥」のキャレット境界 (平面レイアウト空間)
            /// </summary>
            public double CaretAfter => FlowReversed ? CellLeft : CellRight;
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

            public double LocalTop { get; set; }

            public double LocalBottom { get; set; }

            public double LocalLeft { get; set; }

            public Matrix3x2 CharTransform { get; set; } = Matrix3x2.Identity;

            public PreviewTextCoordTransformer? Transformer { get; set; }

            /// <summary>
            /// 行ボックスの縦方向が確定しているか
            /// </summary>
            public bool Resolved { get; set; }

            public double LocalHeight => LocalBottom - LocalTop;
        }

        /// <summary>
        /// バウンディングボックスの余白
        /// </summary>
        const double BoundingBoxPadding = 2.0;

        /// <summary>
        /// 折り返し判定 (ローカル X が行頭側へ戻ったか) の許容差
        /// </summary>
        const double WrapDetectionEpsilon = 0.5;

        /// <summary>
        /// 行の縦幅として有効とみなす最小値。これ未満の行 (空白だけの行など) は前後の行から補間する。
        /// </summary>
        const double MinLineHeight = 1E-3;

        List<LineInfo> Lines { get; } = [];

        List<GlyphInfo> Glyphs { get; } = [];

        CharacterGeometry? EmptyCaret { get; }

        bool IsUniformTransform { get; }

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
        /// <param name="geometries">レンダラが生成したグリフごとのジオメトリ</param>
        /// <param name="emptyCaretGeometry">
        /// テキストが空 (ジオメトリなし) のときに使うキャレット位置。
        /// Bounds はローカル空間でのキャレット矩形 (幅 0 でよい)。
        /// </param>
        public GeometryTextLayout(string text, IReadOnlyList<CharacterGeometry> geometries, CharacterGeometry? emptyCaretGeometry = null)
        {
            EmptyCaret = emptyCaretGeometry;
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
        /// Bounds はインクのバウンディングボックスで文字ごとに上下端が異なるため、Y の変化では判定できない。
        /// 代わりに「ローカル X が直前のグリフより行頭側へ戻り、かつ下に移動した」ことを折り返しとみなす
        /// (横書き・左から右への配置を前提とする)。
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
        static bool IsWrappedLineStart(GlyphInfo previous, GlyphInfo next)
        {
            var movedBack = next.LocalBounds.Left < previous.LocalBounds.Left - WrapDetectionEpsilon;
            var previousCenterY = (previous.LocalBounds.Top + previous.LocalBounds.Bottom) / 2;
            var nextCenterY = (next.LocalBounds.Top + next.LocalBounds.Bottom) / 2;
            return movedBack && nextCenterY > previousCenterY;
        }

        /// <summary>
        /// 行ごとにセル (キャレット境界) を求める。隣接するグリフの間ではインクの隙間の中点を境界とし、
        /// 行頭・行末ではインクの端をそのまま使う。
        /// 空白のように幅 0 のインクしか無いグリフも、前後との中点で幅のあるセルになる。
        /// </summary>
        void ComputeCells()
        {
            foreach (var line in Lines)
            {
                var glyphs = line.Glyphs;
                for (var i = 0; i < glyphs.Count; i++)
                {
                    var glyph = glyphs[i];
                    glyph.CellLeft = i == 0
                        ? glyph.LocalBounds.Left
                        : glyphs[i - 1].CellRight;
                    if (i + 1 < glyphs.Count)
                    {
                        // カーニングなどでインクが重なる場合も中点を採用し、セルが逆転しないようにする
                        var boundary = (glyph.LocalBounds.Right + glyphs[i + 1].LocalBounds.Left) / 2;
                        glyph.CellRight = Math.Max(boundary, glyph.CellLeft);
                    }
                    else
                    {
                        glyph.CellRight = Math.Max(glyph.LocalBounds.Right, glyph.CellLeft);
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
                var top = double.MaxValue;
                var bottom = double.MinValue;
                foreach (var glyph in line.Glyphs)
                {
                    top = Math.Min(top, glyph.LocalBounds.Top);
                    bottom = Math.Max(bottom, glyph.LocalBounds.Bottom);
                }
                line.LocalTop = top;
                line.LocalBottom = bottom;
                line.LocalLeft = line.Glyphs[0].CellLeft;
                line.CharTransform = line.Glyphs[0].CharTransform;
                line.Transformer = line.Glyphs[0].Transformer;
                // 幅 0 のインクしか無い行 (空白のみなど) は縦幅を前後の行から補間する
                line.Resolved = line.LocalHeight >= MinLineHeight;
            }

            if (!Lines.Any(line => line.Resolved))
            {
                return;
            }

            // 前方パス: 縦幅が未確定の行を直前の行の下に積む
            var lastHeight = Lines.First(line => line.Resolved).LocalHeight;
            for (var i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (line.Resolved)
                {
                    lastHeight = line.LocalHeight;
                    continue;
                }
                if (i > 0 && Lines[i - 1].Resolved)
                {
                    InheritLineMetrics(line, Lines[i - 1], Lines[i - 1].LocalBottom, lastHeight);
                }
            }

            // 後方パス: 先頭側に残った未確定の行を次の行の上に積む
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
                InheritLineMetrics(line, next, next.LocalTop - next.LocalHeight, next.LocalHeight);
            }
        }

        /// <summary>
        /// 縦幅が未確定の行に、隣接する行から縦方向の位置と高さを引き継ぐ。
        /// グリフを持たない行は左端とトランスフォームも引き継ぐ。
        /// </summary>
        static void InheritLineMetrics(LineInfo line, LineInfo source, double top, double height)
        {
            line.LocalTop = top;
            line.LocalBottom = top + height;
            if (line.Glyphs.Count == 0)
            {
                line.LocalLeft = source.LocalLeft;
                line.CharTransform = source.CharTransform;
                line.Transformer = source.Transformer;
            }
            line.Resolved = true;
        }

        /// <summary>
        /// グリフごとに、文字列順の並び方向とローカル +X の写像方向が一致しているかを判定する。
        /// パスの逆順レイアウト (isInvert) では、配置はパスを逆走する一方でグリフは正立のままのため、
        /// 文字列順で「次」のグリフがローカル +X の反対側に並ぶ。この場合、キャレット境界や
        /// ヒットテストの左右判定を反転させる必要がある。
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

                    var centerY = (line.LocalTop + line.LocalBottom) / 2;
                    var glyphCenter = ProjectPoint(glyph.CharTransform, glyph.Transformer, glyph.InkCenterX, centerY);
                    var neighborCenter = ProjectPoint(neighbor.CharTransform, neighbor.Transformer, neighbor.InkCenterX, centerY);
                    var deltaX = (neighborCenter.X - glyphCenter.X) * sign;
                    var deltaY = (neighborCenter.Y - glyphCenter.Y) * sign;

                    // ローカル +X 方向のスクリーン写像 (有限差分)
                    var unitX = ProjectPoint(glyph.CharTransform, glyph.Transformer, glyph.InkCenterX + 1, centerY);
                    var directionX = unitX.X - glyphCenter.X;
                    var directionY = unitX.Y - glyphCenter.Y;

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
        static Point ProjectPoint(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, double x, double y)
        {
            var layerLocal = Vector2.Transform(new Vector2((float)x, (float)y), charTransform);
            var screen = transformer.LocalCoordToScreenCoord(new Vector3d(layerLocal.X, layerLocal.Y, 0.0));
            return new Point(screen.X, screen.Y);
        }

        static TextQuad ProjectRect(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, Rect rect)
        {
            return new TextQuad(
                ProjectPoint(charTransform, transformer, rect.Left, rect.Top),
                ProjectPoint(charTransform, transformer, rect.Right, rect.Top),
                ProjectPoint(charTransform, transformer, rect.Right, rect.Bottom),
                ProjectPoint(charTransform, transformer, rect.Left, rect.Bottom));
        }

        /// <summary>
        /// グリフのセル (行の縦幅 × セル幅) を平面レイアウト空間の矩形として返す
        /// </summary>
        Rect GetCellRect(GlyphInfo glyph)
        {
            var line = Lines[glyph.LineIndex];
            return new Rect(glyph.CellLeft, line.LocalTop, Math.Max(0, glyph.CellRight - glyph.CellLeft), line.LocalHeight);
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
        static Matrix ApproximateScreenTransform(in Matrix3x2 charTransform, PreviewTextCoordTransformer transformer, double originX, double originY)
        {
            var origin = ProjectPoint(charTransform, transformer, originX, originY);
            var unitX = ProjectPoint(charTransform, transformer, originX + 1, originY);
            var unitY = ProjectPoint(charTransform, transformer, originX, originY + 1);
            var jacobianXX = unitX.X - origin.X;
            var jacobianXY = unitX.Y - origin.Y;
            var jacobianYX = unitY.X - origin.X;
            var jacobianYY = unitY.Y - origin.Y;
            return new Matrix(
                jacobianXX, jacobianXY, jacobianYX, jacobianYY,
                origin.X - (jacobianXX * originX) - (jacobianYX * originY),
                origin.Y - (jacobianXY * originX) - (jacobianYY * originY));
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
        /// キャレットのローカル座標 (X・行・使用するトランスフォーム) を求める
        /// </summary>
        (double X, LineInfo Line, Matrix3x2 CharTransform, PreviewTextCoordTransformer? Transformer) GetCaretLocal(int offset)
        {
            var line = Lines[GetLineIndexFromOffset(offset)];

            if (line.Glyphs.Count == 0)
            {
                return (line.LocalLeft, line, line.CharTransform, line.Transformer);
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

        public double GetCaretLocalX(int offset)
        {
            if (IsEmpty)
            {
                return EmptyCaret?.Bounds.X ?? 0;
            }
            return GetCaretLocal(offset).X;
        }

        public (Point Top, Point Bottom) GetCaretLine(int offset)
        {
            if (IsEmpty)
            {
                if (EmptyCaret != null && EmptyCaret.Bounds.Height > 0)
                {
                    return (ProjectPoint(EmptyCaret.GraphemeTransform, EmptyCaret.Transformer, EmptyCaret.Bounds.X, EmptyCaret.Bounds.Top),
                            ProjectPoint(EmptyCaret.GraphemeTransform, EmptyCaret.Transformer, EmptyCaret.Bounds.X, EmptyCaret.Bounds.Bottom));
                }
                return (default, default);
            }

            var (x, line, charTransform, transformer) = GetCaretLocal(offset);
            if (transformer == null)
            {
                return (default, default);
            }
            return (ProjectPoint(charTransform, transformer, x, line.LocalTop),
                    ProjectPoint(charTransform, transformer, x, line.LocalBottom));
        }

        public bool TryGetCaretLocalFrame(int offset, out Point localPosition, out double localHeight, out Matrix transform)
        {
            if (IsEmpty)
            {
                if (EmptyCaret != null && EmptyCaret.Bounds.Height > 0)
                {
                    localPosition = new Point(EmptyCaret.Bounds.X, EmptyCaret.Bounds.Y);
                    localHeight = EmptyCaret.Bounds.Height;
                    transform = ApproximateScreenTransform(EmptyCaret.GraphemeTransform, EmptyCaret.Transformer, EmptyCaret.Bounds.X, EmptyCaret.Bounds.Y);
                    return true;
                }
                localPosition = default;
                localHeight = 0;
                transform = Matrix.Identity;
                return false;
            }

            var (x, line, charTransform, transformer) = GetCaretLocal(offset);
            if (transformer == null)
            {
                localPosition = default;
                localHeight = 0;
                transform = Matrix.Identity;
                return false;
            }
            localPosition = new Point(x, line.LocalTop);
            localHeight = line.LocalHeight;
            transform = ApproximateScreenTransform(charTransform, transformer, x, line.LocalTop);
            return true;
        }

        public int GetOffsetAtLineDistance(int lineIndex, double localX)
        {
            var line = Lines[Math.Clamp(lineIndex, 0, Lines.Count - 1)];
            foreach (var glyph in line.Glyphs)
            {
                if (localX < glyph.InkCenterX)
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

                LineInfo? bestLine = null;
                var bestDistance = double.MaxValue;
                foreach (var line in Lines)
                {
                    if (!line.Resolved)
                    {
                        continue;
                    }

                    var distance = local.Y < line.LocalTop
                        ? line.LocalTop - local.Y
                        : local.Y > line.LocalBottom ? local.Y - line.LocalBottom : 0.0;

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
                return GetOffsetAtLineDistance(Lines.IndexOf(bestLine), local.X);
            }

            // パスに沿ったレイアウトなど、グリフごとにトランスフォームが異なる場合は
            // スクリーン空間で最も近いグリフを探し、そのローカル空間で左右を判定する
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
                var before = local.X < glyph.InkCenterX;
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
                    quads.Add(ProjectRect(runCharTransform, runTransformer,
                        new Rect(runStart, line.LocalTop, Math.Max(0, runEnd - runStart), line.LocalHeight)));
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
                        runStart = Math.Min(runStart, glyph.CellLeft);
                        runEnd = Math.Max(runEnd, glyph.CellRight);
                    }
                    else
                    {
                        Flush();
                        runStart = glyph.CellLeft;
                        runEnd = glyph.CellRight;
                        runCharTransform = glyph.CharTransform;
                        runTransformer = glyph.Transformer;
                        runOpen = true;
                    }
                }

                // 選択が改行を越えて続く場合のマーカー (行末のグリフと同じトランスフォームで描く)
                if (end > lineEnd && start <= lineEnd)
                {
                    var markerWidth = line.LocalHeight * 0.35;
                    var markerStart = line.LocalLeft;
                    var markerCharTransform = line.CharTransform;
                    var markerTransformer = line.Transformer;
                    if (line.Glyphs.Count > 0)
                    {
                        var lastGlyph = line.Glyphs[^1];
                        markerCharTransform = lastGlyph.CharTransform;
                        markerTransformer = lastGlyph.Transformer;
                        // フロー反転時は行末の視覚的な「続き」はローカル Left 側
                        markerStart = lastGlyph.FlowReversed
                            ? lastGlyph.CellLeft - markerWidth
                            : lastGlyph.CellRight;
                    }

                    if (markerTransformer != null)
                    {
                        if (runOpen && runCharTransform == markerCharTransform && Equals(runTransformer, markerTransformer))
                        {
                            runStart = Math.Min(runStart, markerStart);
                            runEnd = Math.Max(runEnd, markerStart + markerWidth);
                        }
                        else
                        {
                            Flush();
                            quads.Add(ProjectRect(markerCharTransform, markerTransformer,
                                new Rect(markerStart, line.LocalTop, markerWidth, line.LocalHeight)));
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
                var left = double.MaxValue;
                var top = double.MaxValue;
                var right = double.MinValue;
                var bottom = double.MinValue;
                foreach (var line in Lines)
                {
                    if (!line.Resolved || line.Glyphs.Count == 0)
                    {
                        continue;
                    }
                    top = Math.Min(top, line.LocalTop);
                    bottom = Math.Max(bottom, line.LocalBottom);
                    foreach (var glyph in line.Glyphs)
                    {
                        left = Math.Min(left, glyph.LocalBounds.Left);
                        right = Math.Max(right, glyph.LocalBounds.Right);
                    }
                }
                if (left == double.MaxValue)
                {
                    return Array.Empty<TextQuad>();
                }
                var union = new Rect(left - BoundingBoxPadding, top - BoundingBoxPadding, right - left + (BoundingBoxPadding * 2), bottom - top + (BoundingBoxPadding * 2));
                var representative = Glyphs[0];
                return [ProjectRect(representative.CharTransform, representative.Transformer, union)];
            }

            // トランスフォームが混在する場合はスクリーン空間の AABB
            var aabb = Rect.Empty;
            foreach (var glyph in Glyphs)
            {
                var line = Lines[glyph.LineIndex];
                var localRect = new Rect(glyph.LocalBounds.X, line.LocalTop, glyph.LocalBounds.Width, line.LocalHeight);
                aabb.Union(ProjectRect(glyph.CharTransform, glyph.Transformer, localRect).GetBounds());
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
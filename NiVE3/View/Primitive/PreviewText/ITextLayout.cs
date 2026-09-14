using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// テキストのレイアウト (文字オフセット⇔座標の変換と描画) を抽象化するインターフェース。
    ///
    /// エディタ本体 (PreviewTextBox) はこのインターフェースだけに依存する。
    /// 実装は 2 種類:
    ///  - FormatterTextLayout: WPF TextFormatter による整形+テキスト描画込み (スタンドアロン動作用)
    ///  - GeometryTextLayout: レンダラから渡された CharacterGeometry 配列に基づく
    ///    (テキスト本体は外部でレンダリング済み。選択・キャレットのみをオーバーレイ描画する)
    ///
    /// 座標系はレイアウト空間の DIP。コントロール座標との対応は PreviewTextBox 側が行い、
    /// スタンドアロンでは Padding の平行移動、オーバーレイでは Origin / Scale / Offset
    /// (仮想スクリーンのコントロール内配置) による変換になる。
    /// トランスフォームを持つ実装では、キャレット・選択範囲は軸平行とは限らないため
    /// Quad / 線分として表現する。「ローカル X」はトランスフォーム適用前の X 座標で、
    /// 上下キー移動時の preferred X の保持に使う。
    /// </summary>
    interface ITextLayout : IDisposable
    {
        /// <summary>
        /// レイアウト全体を包含するサイズ (Measure 用・レイアウト空間)
        /// </summary>
        Size Extent { get; }

        /// <summary>
        /// true ならテキスト本体もこのレイアウトが描画する (DrawText が有効)
        /// </summary>
        bool DrawsText { get; }

        int LineCount { get; }

        /// <summary>
        /// オフセットが属する行のインデックスを返す
        /// </summary>
        int GetLineIndexFromOffset(int offset);

        /// <summary>
        /// 行の文字範囲 (末尾の改行を含まない) を返す
        /// </summary>
        (int Start, int Length) GetLineRange(int lineIndex);

        /// <summary>
        /// キャレットのローカル X 座標 (上下移動の preferred X 用)
        /// </summary>
        double GetCaretLocalX(int offset);

        /// <summary>
        /// 指定行内でローカル X 座標に最も近いオフセットを返す (上下キー移動用)
        /// </summary>
        int GetOffsetAtLineDistance(int lineIndex, double localX);

        /// <summary>
        /// レイアウト空間の座標に最も近い文字オフセットを返す
        /// </summary>
        int GetOffsetAt(Point point);

        /// <summary>
        /// キャレットを表す線分 (上端→下端) をレイアウト空間で返す。
        /// 位置を決定できない場合 (ジオメトリ未提供など) は Top == Bottom の縮退線分を返す。
        /// </summary>
        (Point Top, Point Bottom) GetCaretLine(int offset);

        /// <summary>
        /// 文字範囲 [start, end) を覆う Quad 群 (選択ハイライト・IME 下線用) を返す
        /// </summary>
        IReadOnlyList<TextQuad> GetRangeQuads(int start, int end);

        /// <summary>
        /// キャレット位置のローカル座標系情報を返す (IME 未確定文字列をエディタ側で
        /// 描画するときに、キャレット位置から同じトランスフォームで文字を並べるために使う)。
        /// localPosition はローカル空間での行上端×キャレット X の点、localHeight は行の高さ。
        /// transform はレイアウトローカル→スクリーンの 2D アフィン変換
        /// (射影変換を含む場合はキャレット位置周りの近似)。位置を決定できない場合は false。
        /// </summary>
        bool TryGetCaretLocalFrame(int offset, out Point localPosition, out double localHeight, out Matrix transform);

        /// <summary>
        /// テキスト全体のバウンディングボックス表示用 Quad 群。空なら表示しない。
        /// </summary>
        IReadOnlyList<TextQuad> GetBoundingQuads();

        /// <summary>
        /// テキスト本体を描画する (DrawsText が true の実装のみ)
        /// </summary>
        void DrawText(DrawingContext drawingContext, Point origin);
    }
}
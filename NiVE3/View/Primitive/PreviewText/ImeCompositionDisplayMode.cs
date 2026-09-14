using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// IME 未確定文字列の表示方法 (オーバーレイ (ジオメトリ) モード時)
    /// </summary>
    enum ImeCompositionDisplayMode
    {
        /// <summary>
        /// DisplayText に未確定文字列を含め、レンダラ側で実レンダリングして表示する。
        /// 見た目は最終レンダリングと完全に一致するが、変換のたびに全レイヤーの再描画が走る。
        /// </summary>
        Renderer,

        /// <summary>
        /// DisplayText は確定テキストのままにし、未確定文字列はエディタが
        /// キャレット位置にプレーンテキストで直接描画する。変換中にレンダラの再描画が不要。
        /// </summary>
        Editor,
    }
}
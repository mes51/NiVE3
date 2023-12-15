using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// エフェクトなどの適用範囲を表します
    /// </summary>
    /// <param name="OriginalImagePosition">画像内の元画像の位置</param>
    /// <param name="OriginalImageSize">画像内の元画像のサイズ</param>
    /// <param name="Left">適用範囲の左</param>
    /// <param name="Top">適用範囲の上</param>
    /// <param name="Right">適用範囲の右</param>
    /// <param name="Bottom">適用範囲の下</param>
    public readonly record struct ROI(Int32Point OriginalImagePosition, Int32Size OriginalImageSize, int Left, int Top, int Right, int Bottom)
    {
    }
}

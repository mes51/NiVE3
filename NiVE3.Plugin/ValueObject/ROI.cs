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
        public static readonly ROI Empty = new ROI();

        public int Width { get; } = Right - Left;

        public int Height { get; } = Bottom - Top;

        /// <summary>
        /// エフェクトの適用範囲を拡張、または縮小します
        /// </summary>
        /// <param name="amount">拡張、または縮小する量</param>
        /// <returns>新しいエフェクトの適用範囲</returns>
        public ROI Expand(int amount)
        {
            return Expand(-amount, -amount, amount, amount);
        }

        /// <summary>
        /// エフェクトの適用範囲を拡張、または縮小します
        /// </summary>
        /// <param name="left">左端の拡張、または縮小する量</param>
        /// <param name="top">上端の拡張、または縮小する量</param>
        /// <param name="right">右端の拡張、または縮小する量</param>
        /// <param name="bottom">下端の拡張、または縮小する量</param>
        /// <returns>新しいエフェクトの適用範囲</returns>
        public ROI Expand(int left, int top, int right, int bottom)
        {
            return new ROI(OriginalImagePosition, OriginalImageSize, Left + left, Top + top, Right + right, Bottom + bottom);
        }

        /// <summary>
        /// エフェクトの適用範囲を指定した四角形と交差する範囲に変更します
        /// </summary>
        /// <param name="left">四角形の左</param>
        /// <param name="top">四角形の上</param>
        /// <param name="right">四角形の右</param>
        /// <param name="bottom">四角形の下</param>
        /// <returns>新しいエフェクトの適用範囲</returns>
        public ROI Intersect(int left, int top, int right, int bottom, bool offsetOrignalPosition = false)
        {
            var newLeft = Math.Max(Left, left);
            var newTop = Math.Max(Top, top);
            var newRight = Math.Min(Right, right);
            var newBottom = Math.Min(Bottom, bottom);

            return new ROI(OriginalImagePosition, OriginalImageSize, newLeft, newTop, newRight, newBottom);
        }
    }
}

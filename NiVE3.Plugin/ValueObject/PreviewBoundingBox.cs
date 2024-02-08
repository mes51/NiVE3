using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// プレビューで表示するバウンディングボックス
    /// </summary>
    /// <param name="Center">アンカーポイント等、バウンディングボックスが表すレイヤーの中心位置。</param>
    /// <param name="BoundingBoxies">バウンディングボックスのシェイプを表すBoundingBoxShapeの配列。複数の図形がある場合はそれぞれ別のBoundingBoxShapeに格納します</param>
    /// <param name="IsEmpty">バウンディングボックスが空であるかどうか。trueの場合はアンカーポイントのみ表示されます</param>
    /// <param name="IsInvalid">バウンディングボックスが存在しないかどうか。trueの場合はアンカーポイント含め表示されません</param>
    public record PreviewBoundingBox(Vector2d Center, BoundingBoxShape[] BoundingBoxies, bool IsEmpty, bool IsInvalid)
    {
        /// <summary>
        /// 空のバウンディングボックスを表します
        /// </summary>
        public static readonly PreviewBoundingBox Empty = new PreviewBoundingBox(Vector2d.Zero, Array.Empty<BoundingBoxShape>(), true, true);
    }

    /// <summary>
    /// バウンディングボックスのシェイプを表します
    /// </summary>
    /// <param name="Path">シェイプのパス</param>
    /// <param name="IsClosed">シェイプが閉じているかどうか</param>
    /// <param name="IsHoldSize">シェイプがプレビューの拡大率の影響を受けず、一定のサイズで表示するかどうか</param>
    /// <param name="Center">IsHoldSizeがtrueの時の拡大・縮小の中心位置</param>
    public record BoundingBoxShape(Vector2d[] Path, bool IsClosed, bool IsHoldSize, Vector2d? Center = null) { }
}

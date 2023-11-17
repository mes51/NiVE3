using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    public interface IPreviewBoundingBox
    {
        bool IsInvalid { get; }
    }

    /// <summary>
    /// プレビューで表示するレイヤーのバウンディングボックス
    /// </summary>
    /// <param name="LeftTop">左上の座標</param>
    /// <param name="RightTop">右上の座標</param>
    /// <param name="LeftBottom">左下の座標</param>
    /// <param name="RightBottom">右下の座標</param>
    /// <param name="AnchorPoint">アンカーポイントの座標</param>
    public record PreviewImageBoundingBox(Vector2d LeftTop, Vector2d RightTop, Vector2d LeftBottom, Vector2d RightBottom, Vector2d AnchorPoint) : IPreviewBoundingBox
    {
        public static readonly PreviewImageBoundingBox Empty = new PreviewImageBoundingBox(new Vector2d(), new Vector2d(), new Vector2d(), new Vector2d(), new Vector2d(double.NaN, double.NaN));

        /// <summary>
        /// バウンディングボックスのサイズが0であるかどうか
        /// </summary>
        public bool IsEmpty => (LeftTop - RightTop).IsZero && (LeftTop - LeftBottom).IsZero && (RightTop - RightBottom).IsZero && (LeftBottom - RightBottom).IsZero;

        public bool IsInvalid => double.IsNaN(AnchorPoint.X) || double.IsNaN(AnchorPoint.Y) || double.IsInfinity(AnchorPoint.X) || double.IsInfinity(AnchorPoint.Y);
    }

    /// <summary>
    /// プレビューで表示するライトのバウンディングボックス
    /// </summary>
    /// <param name="LightType">ライトの種類</param>
    /// <param name="Position">ライトの位置</param>
    /// <param name="PointOfInterest">ライトの目標点</param>
    /// <param name="ConeAngle">スポットライトの範囲</param>
    public record PreviewLightBoundingBox(LightType LightType, Vector2d Position, Vector2d PointOfInterest, double ConeAngle) : IPreviewBoundingBox
    {
        public static readonly PreviewLightBoundingBox Empty = new PreviewLightBoundingBox(LightType.Ambient, new Vector2d(), new Vector2d(), double.NaN);

        public bool IsInvalid => LightType == LightType.Spot && (double.IsNaN(ConeAngle) || double.IsInfinity(ConeAngle));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces.RendererParams
{
    /// <summary>
    /// ライトの設定を表します
    /// </summary>
    /// <param name="LightType">ライトの種類</param>
    /// <param name="PointOfInterest">ライトの目標点</param>
    /// <param name="Position">ライトの位置</param>
    /// <param name="Orientation">ライトの方向</param>
    /// <param name="AngleX">ライトのX回転</param>
    /// <param name="AngleY">ライトのY回転</param>
    /// <param name="AngleZ">ライトのZ回転</param>
    /// <param name="Color">ライトの色</param>
    /// <param name="Intensity">ライトの強度</param>
    /// <param name="ConeAngle">スポットライトの円錐頂角</param>
    /// <param name="ConeAttenuation">スポットライトの円錐ぼかし</param>
    /// <param name="FalloffType">ライトのフォールオフの種類</param>
    /// <param name="FalloffStart">フォールオフ開始距離</param>
    /// <param name="FalloffLength">フォールオフがLinearの時の強度が0になる長さ</param>
    /// <param name="IsEnableShadow">このライトが影を落とすかどうか</param>
    /// <param name="ShadowStrength">影の濃さ</param>
    /// <param name="ShadowScatterSize">影のぼかしのサイズ</param>
    /// <param name="ParentTransforms">親のトランスフォームの値</param>
    public record LightSetting(LightType LightType,
                               Vector3d PointOfInterest,
                               Vector3d Position,
                               Vector3d Orientation,
                               double AngleX,
                               double AngleY,
                               double AngleZ,
                               Vector3 Color,
                               double Intensity,
                               double ConeAngle,
                               double ConeAttenuation,
                               LightFalloffType FalloffType,
                               double FalloffStart,
                               double FalloffLength,
                               bool IsEnableShadow,
                               double ShadowStrength,
                               double ShadowScatterSize,
                               ParentTransform[] ParentTransforms)
    {
    }

    /// <summary>
    /// ライトの種類を表します
    /// </summary>
    public enum LightType
    {
        /// <summary>
        /// ポイント
        /// </summary>
        Point,
        /// <summary>
        /// スポット
        /// </summary>
        Spot,
        /// <summary>
        /// 平行
        /// </summary>
        Parallel,
        /// <summary>
        /// アンピエント
        /// </summary>
        Ambient
    }

    /// <summary>
    /// ライトのフォールオフの種類を表します
    /// </summary>
    public enum LightFalloffType
    {
        /// <summary>
        /// なし
        /// </summary>
        None,
        /// <summary>
        /// リニア
        /// </summary>
        Linear,
        /// <summary>
        /// 逆2乗クランプ
        /// </summary>
        Exponential
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Interfaces
{
    /// <summary>
    /// フッテージのメディアの形式を表します。
    /// </summary>
    [Flags]
    public enum SourceType
    {
        /// <summary>
        /// なし
        /// </summary>
        None = 0b000,
        /// <summary>
        /// 画像
        /// </summary>
        Image = 0b001,
        /// <summary>
        /// 音声
        /// </summary>
        Audio = 0b010,
        /// <summary>
        /// ビデオ(音声なし)
        /// </summary>
        Video = 0b100,
        /// <summary>
        /// ビデオ+音声
        /// </summary>
        VideoAndAudio = Video | Audio
    }

    /// <summary>
    /// 画像の補間画質を表します。レベルが高いほど高画質になります。
    /// </summary>
    public enum ImageInterpolationQuality
    {
        Level1,
        Level2
    }

    /// <summary>
    /// トラックマットのモードを表します。
    /// </summary>
    public enum TrackMatteMode
    {
        /// <summary>
        /// アルファ
        /// </summary>
        Alpha,
        /// <summary>
        /// 反転アルファ
        /// </summary>
        InvertAlpha,
        /// <summary>
        /// 輝度
        /// </summary>
        Luminance,
        /// <summary>
        /// 反転輝度
        /// </summary>
        InvertLuminance
    }

    /// <summary>
    /// 親レイヤーの種類を表します
    /// </summary>
    public enum ParentType
    {
        /// <summary>
        /// 通常のレイヤー
        /// </summary>
        Normal,
        /// <summary>
        /// カメラレイヤー
        /// </summary>
        Camera,
        /// <summary>
        /// ヌルオブジェクト
        /// </summary>
        NullObject,
        /// <summary>
        /// スポット/平行ライトレイヤー
        /// </summary>
        SpotOrParallelLight,
        /// <summary>
        /// ポイントライトレイヤー
        /// </summary>
        PointLight,
        /// <summary>
        /// アンビエントライトレイヤー
        /// </summary>
        AmbientLight,
        /// <summary>
        /// 展開元のコンポジション
        /// </summary>
        ExplodedComposition
    }

    /// <summary>
    /// 影を落とすかどうかの設定を表します
    /// </summary>
    public enum ShadowCastMode
    {
        /// <summary>
        /// 影を落としません
        /// </summary>
        None,
        /// <summary>
        /// 影を落とします
        /// </summary>
        Cast,
        /// <summary>
        /// 影のみを落とし、レイヤーは表示しません
        /// </summary>
        ShadowOnly
    }
}

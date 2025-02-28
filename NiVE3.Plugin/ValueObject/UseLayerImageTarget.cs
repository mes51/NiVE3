using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// 画像を取得して使用するレイヤーを表します
    /// </summary>
    /// <param name="LayerId">レイヤーのID</param>
    /// <param name="ImageProcessType">取得する画像に適用する処理</param>
    public record UseLayerImageTarget(Guid LayerId, LayerImageProcessType ImageProcessType)
    {
        public static readonly UseLayerImageTarget Empty = new UseLayerImageTarget(Guid.Empty, LayerImageProcessType.Raw);
    }

    /// <summary>
    /// レイヤーから画像を取得する際に適用する処理を表します
    /// </summary>
    public enum LayerImageProcessType
    {
        /// <summary>
        /// 何もしない(入力から取得したまま)
        /// </summary>
        Raw,
        /// <summary>
        /// マスク適用済み
        /// </summary>
        Masked,
        /// <summary>
        /// マスクとエフェクト適用済み
        /// </summary>
        Effected
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// 音声を取得して使用するレイヤーを表します
    /// </summary>
    /// <param name="LayerId">レイヤーのID</param>
    /// <param name="AudioProcessType">取得する画像に適用する処理</param>
    public record UseLayerAudioTarget(Guid LayerId, LayerAudioProcessType AudioProcessType)
    {
        public static readonly UseLayerAudioTarget Empty = new UseLayerAudioTarget(Guid.Empty, LayerAudioProcessType.Raw);
    }

    /// <summary>
    /// レイヤーから音声を取得する際に適用する処理を表します
    /// </summary>
    public enum LayerAudioProcessType
    {
        /// <summary>
        /// 何もしない(入力から取得したまま)
        /// </summary>
        Raw,
        /// <summary>
        /// マスク適用済み
        /// </summary>
        Effected
    }
}

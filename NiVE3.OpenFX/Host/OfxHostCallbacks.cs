using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// ホストアプリケーション (NiVE3 本体) が提供するコールバック
    /// </summary>
    public static class OfxHostCallbacks
    {
        /// <summary>
        /// Message Suite のメッセージ表示処理 (メッセージ種別, メッセージ本文) → ステータス
        /// 質問 (OfxMessageQuestion) の場合は ReplyYes / ReplyNo を返します
        /// null の場合はログ出力のみになります
        /// </summary>
        public static Func<string, string, OfxStatus>? MessageHandler { get; set; }
    }
}

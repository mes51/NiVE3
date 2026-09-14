using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// プレビューテキスト編集の IME まわりの診断ログ。
    /// Log は NIVE3_PREVIEW_TEXT_DIAGNOSTICS シンボルが定義されている構成 (DebugDiagnostics) でのみ有効で、
    /// それ以外の構成では [Conditional] により呼び出し側のコードごと (引数の評価も含めて) 除去されます。
    /// 有効な構成でも、環境変数 NIVE3_PREVIEW_TEXT_IME_LOG にファイルパスが設定されている場合のみ出力します。
    /// </summary>
    static class ImeDebugLog
    {
        [Conditional("NIVE3_PREVIEW_TEXT_DIAGNOSTICS")]
        public static void Log(string message)
        {
#if NIVE3_PREVIEW_TEXT_DIAGNOSTICS
            if (string.IsNullOrEmpty(LogPath))
            {
                return;
            }

            lock (LockObject)
            {
                try
                {
                    File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\r\n");
                }
                catch
                {
                    // ログ出力の失敗は無視する
                }
            }
#endif
        }

#if NIVE3_PREVIEW_TEXT_DIAGNOSTICS
        const string EnvironmentVariableName = "NIVE3_PREVIEW_TEXT_IME_LOG";

        static object LockObject { get; } = new();

        static string? LogPath { get; } = Environment.GetEnvironmentVariable(EnvironmentVariableName);
#endif
    }
}
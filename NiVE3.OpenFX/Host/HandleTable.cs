using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// OFX のハンドル (opaque pointer) とマネージドオブジェクトの対応表
    /// </summary>
    public static class HandleTable
    {
        static readonly ConcurrentDictionary<nint, object> Objects = new ConcurrentDictionary<nint, object>();

        static long NextId = 0x1000;

        /// <summary>
        /// オブジェクトにハンドルを割り当てます
        /// </summary>
        /// <param name="obj">割り当て対象のオブジェクト</param>
        /// <returns>割り当てられたハンドル</returns>
        public static nint Alloc(object obj)
        {
            var handle = (nint)Interlocked.Add(ref NextId, 0x10);
            Objects[handle] = obj;
            return handle;
        }

        /// <summary>
        /// ハンドルからオブジェクトを取得します
        /// </summary>
        /// <typeparam name="T">期待するオブジェクトの型</typeparam>
        /// <param name="handle">ハンドル</param>
        /// <returns>対応するオブジェクト。存在しない、または型が異なる場合は null</returns>
        public static T? Get<T>(nint handle) where T : class
        {
            var found = Objects.TryGetValue(handle, out var obj);
#if NIVE3_OFX_DIAGNOSTICS
            if (OfxLog.TraceEnabled)
            {
                var desc = obj switch
                {
                    ParamInstance p => $"Param:{p.Name}",
                    PropertySet ps => $"Props:{ps.Name}",
                    null => "(未登録)",
                    _ => obj.GetType().Name
                };
                OfxLog.Trace($"handle 0x{handle:X} -> {desc}");
            }
#endif
            return found ? obj as T : null;
        }

        /// <summary>
        /// ハンドルを解放します
        /// </summary>
        /// <param name="handle">解放するハンドル</param>
        public static void Free(nint handle)
        {
            Objects.TryRemove(handle, out _);
        }
    }

    /// <summary>
    /// OFX ホストの診断ログ。
    /// 各メソッドは NIVE3_OFX_DIAGNOSTICS シンボルが定義されている構成 (DebugDiagnostics) でのみ有効で、
    /// それ以外の構成では [Conditional] により呼び出し側のコードごと (引数の評価も含めて) 除去されます
    /// </summary>
    public static class OfxLog
    {
        /// <summary>
        /// ログの出力先
        /// </summary>
        public static Action<string>? Sink { get; set; }

        [Conditional("NIVE3_OFX_DIAGNOSTICS")]
        public static void Info(string message)
        {
#if NIVE3_OFX_DIAGNOSTICS
            Sink?.Invoke(message);
#endif
        }

        [Conditional("NIVE3_OFX_DIAGNOSTICS")]
        public static void Warn(string message)
        {
#if NIVE3_OFX_DIAGNOSTICS
            Sink?.Invoke("[WARN] " + message);
#endif
        }

#if NIVE3_OFX_DIAGNOSTICS
        /// <summary>
        /// NIVE3_OFX_TRACE=1 で有効になる詳細トレース (クラッシュ調査用)
        /// </summary>
        public static bool TraceEnabled { get; } = Environment.GetEnvironmentVariable("NIVE3_OFX_TRACE") == "1";

        public static void Trace(string message)
        {
            if (TraceEnabled)
            {
                Sink?.Invoke("[TRACE] " + message);
            }
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// OFX のプロパティセットの実装
    /// 値は double / int / string / nint (ポインタ) のいずれかを保持します
    /// </summary>
    public sealed class PropertySet : IDisposable
    {
        /// <summary>
        /// ログ出力などに使用する名前
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// このプロパティセットの OFX ハンドル
        /// </summary>
        public nint Handle { get; }

        Dictionary<string, List<object?>> Values { get; } = new Dictionary<string, List<object?>>();

        // propGetString が返すネイティブ文字列の生存期間を管理するためのキャッシュ
        Dictionary<(string Key, int Index), nint> NativeStrings { get; } = new Dictionary<(string, int), nint>();

        object Lock { get; } = new object();

        /// <summary>
        /// 存在しないプロパティが読まれた際に警告ログを出すかどうか
        /// </summary>
        public bool LogMissingReads { get; set; } = true;

        /// <summary>
        /// プラグインが propSet 系で単一の値を書き換えた際に呼ばれるコールバック (キー名が渡されます)
        /// ホスト側の一括設定 (SetAll) では呼ばれません
        /// </summary>
        public Action<string>? SingleValueChanged { get; set; }

        public PropertySet(string name)
        {
            Name = name;
            Handle = HandleTable.Alloc(this);
        }

        List<object?> EnsureList(string key, int minCount)
        {
            if (!Values.TryGetValue(key, out var list))
            {
                list = new List<object?>();
                Values[key] = list;
            }
            while (list.Count < minCount)
            {
                list.Add(null);
            }
            return list;
        }

        /// <summary>
        /// プロパティに値を設定します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="index">設定先のインデックス</param>
        /// <param name="value">設定する値</param>
        public void Set(string key, int index, object? value)
        {
#if NIVE3_OFX_DIAGNOSTICS
            if (OfxLog.TraceEnabled)
            {
                OfxLog.Trace($"{Name}.Set[\"{key}\"][{index}] = {value}");
            }
#endif
            lock (Lock)
            {
                var list = EnsureList(key, index + 1);
                list[index] = value;
                if (NativeStrings.Remove((key, index), out var old))
                {
                    Marshal.FreeCoTaskMem(old);
                }
            }
            SingleValueChanged?.Invoke(key);
        }

        /// <summary>
        /// プロパティの値の一覧をまとめて設定します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="values">設定する値の一覧</param>
        public void SetAll(string key, params object?[] values)
        {
            lock (Lock)
            {
                Reset(key);
                var list = EnsureList(key, values.Length);
                for (var i = 0; i < values.Length; i++)
                {
                    list[i] = values[i];
                }
            }
        }

        /// <summary>
        /// プロパティの値を取得します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="index">取得するインデックス</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得に成功したかどうか</returns>
        public bool TryGet(string key, int index, out object? value)
        {
            lock (Lock)
            {
                if (Values.TryGetValue(key, out var list) && index >= 0 && index < list.Count)
                {
                    value = list[index];
#if NIVE3_OFX_DIAGNOSTICS
                    if (OfxLog.TraceEnabled)
                    {
                        OfxLog.Trace($"{Name}.Get[\"{key}\"][{index}] -> {value}");
                    }
#endif
                    return true;
                }
            }

            value = null;
#if NIVE3_OFX_DIAGNOSTICS
            if (LogMissingReads)
            {
                OfxLog.Warn($"プロパティ未定義: {Name}[\"{key}\"][{index}]");
            }
#endif
            return false;
        }

        /// <summary>
        /// プロパティの値を取得します。存在しない場合も警告ログを出しません (ホスト内部での読み取り用)
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="index">取得するインデックス</param>
        /// <returns>取得した値。存在しない場合は null</returns>
        public object? GetOrDefault(string key, int index)
        {
            lock (Lock)
            {
                if (Values.TryGetValue(key, out var list) && index >= 0 && index < list.Count)
                {
                    return list[index];
                }
            }
            return null;
        }

        /// <summary>
        /// プロパティの次元数を取得します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>次元数。プロパティが存在しない場合は 0</returns>
        public int GetDimension(string key)
        {
            lock (Lock)
            {
                return Values.TryGetValue(key, out var list) ? list.Count : 0;
            }
        }

        /// <summary>
        /// プロパティが存在するかどうかを取得します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        public bool Contains(string key)
        {
            lock (Lock)
            {
                return Values.ContainsKey(key);
            }
        }

        /// <summary>
        /// プロパティをデフォルト (空) に戻します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        public void Reset(string key)
        {
            lock (Lock)
            {
                Values.Remove(key);
                foreach (var entry in NativeStrings.Where(kv => kv.Key.Key == key).ToArray())
                {
                    Marshal.FreeCoTaskMem(entry.Value);
                    NativeStrings.Remove(entry.Key);
                }
            }
        }

        /// <summary>
        /// propGetString 用に、キャッシュされたネイティブ UTF8 文字列を取得します
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="index">インデックス</param>
        /// <param name="value">対象の文字列</param>
        /// <returns>ネイティブ文字列へのポインタ</returns>
        public nint GetNativeString(string key, int index, string value)
        {
            lock (Lock)
            {
                if (!NativeStrings.TryGetValue((key, index), out var ptr))
                {
                    ptr = Marshal.StringToCoTaskMemUTF8(value);
                    NativeStrings[(key, index)] = ptr;
                }
#if NIVE3_OFX_DIAGNOSTICS
                if (OfxLog.TraceEnabled)
                {
                    OfxLog.Trace($"{Name}.NativeString[\"{key}\"][{index}] -> 0x{ptr:X} ({value.Length} 文字)");
                }
#endif
                return ptr;
            }
        }

        /// <summary>
        /// このプロパティセットの複製を作成します (デスクリプタからインスタンスを作る際に使用)
        /// </summary>
        /// <param name="name">複製に付ける名前</param>
        /// <returns>複製されたプロパティセット</returns>
        public PropertySet Clone(string name)
        {
            var clone = new PropertySet(name) { LogMissingReads = LogMissingReads };
            lock (Lock)
            {
                foreach (var (key, values) in Values)
                {
                    clone.Values[key] = new List<object?>(values);
                }
            }
            return clone;
        }

        /// <summary>
        /// 保持している全プロパティのスナップショットを取得します (インベントリのダンプ用)
        /// </summary>
        public IReadOnlyDictionary<string, object?[]> Snapshot()
        {
            lock (Lock)
            {
                return Values.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
            }
        }

        public void Dispose()
        {
            lock (Lock)
            {
                foreach (var ptr in NativeStrings.Values)
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
                NativeStrings.Clear();
            }
            HandleTable.Free(Handle);
        }
    }
}

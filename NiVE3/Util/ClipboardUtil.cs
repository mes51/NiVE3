using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Data.Clipboard;

namespace NiVE3.Util
{
    static class ClipboardUtil
    {
        public static void SetData<T>(CopyData<T> data)
        {
            var json = JsonSerializer.Serialize(data);
            try
            {
                Clipboard.SetText(json);
            }
            catch { } // NOTE: 連打するとたまにエラーになる
        }

        public static CopyData<T>? GetData<T>()
        {
            if (!Clipboard.ContainsText())
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<CopyData<T>>(Clipboard.GetText());
            }
            catch
            {
                return null;
            }
        }

        public static CopyData<T>? GetData<T>(CopyDataType targetType)
        {
            if (!Clipboard.ContainsText())
            {
                return null;
            }

            try
            {
                var result = JsonSerializer.Deserialize<CopyData<T>>(Clipboard.GetText());
                if (result?.Type == targetType)
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static CopyDataType GetDataType()
        {
            if (!Clipboard.ContainsText())
            {
                return CopyDataType.None;
            }

            try
            {
                return JsonSerializer.Deserialize<CopyData<object>>(Clipboard.GetText())?.Type ?? CopyDataType.None;
            }
            catch
            {
                return CopyDataType.None;
            }
        }
    }
}

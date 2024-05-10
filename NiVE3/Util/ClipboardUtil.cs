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
    }
}

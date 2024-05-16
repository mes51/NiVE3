using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Internal.Util
{
    static class SpanBinaryConverter
    {
        public static Span<byte> ConvertToSpan<T>(this T value) where T : unmanaged
        {
            var span = MemoryMarshal.CreateSpan(ref value, 1);
            return MemoryMarshal.Cast<T, byte>(span);
        }
    }
}

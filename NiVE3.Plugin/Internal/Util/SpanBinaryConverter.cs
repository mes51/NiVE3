using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Internal.Util
{
    static class SpanBinaryConverter
    {
        static byte[] Buffer = new byte[32];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> ConvertToSpan<T>(this T value) where T : unmanaged
        {
            var span = MemoryMarshal.CreateSpan(ref value, 1);
            var byteSpan = MemoryMarshal.Cast<T, byte>(span);
            if (byteSpan.Length > Buffer.Length)
            {
                Buffer = new byte[byteSpan.Length];
            }
            byteSpan.CopyTo(Buffer);
            return Buffer.AsSpan(0, byteSpan.Length);
        }
    }
}

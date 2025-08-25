using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Extension
{
    static class XxHash3Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 ToInt128(this XxHash3 hash)
        {
            var result = (Int128)0;
            var resultSpan = MemoryMarshal.CreateSpan(ref result, 1);
            hash.GetCurrentHash(MemoryMarshal.Cast<Int128, byte>(resultSpan));

            return result;
        }
    }
}

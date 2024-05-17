using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class HashExtensions
    {
        public static Int128 ToInt128(this XxHash3 hash)
        {
            var result = (Int128)0;
            var resultSpan = MemoryMarshal.CreateSpan(ref result, 1);
            hash.GetCurrentHash(MemoryMarshal.Cast<Int128, byte>(resultSpan));

            return result;
        }

        public static void Append<T>(this XxHash3 hash, T value) where T : unmanaged
        {
            var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
            hash.Append(MemoryMarshal.Cast<T, byte>(span));
        }
    }
}

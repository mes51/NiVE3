using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class GuidExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 ToInt128(this in Guid guid)
        {
            return Unsafe.BitCast<Guid, Int128>(guid);
        }
    }
}

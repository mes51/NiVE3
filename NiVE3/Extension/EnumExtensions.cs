using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this T value, params T[] candidate) where T : Enum
        {
            return candidate.Contains(value);
        }
    }
}

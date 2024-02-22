using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class FloatExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(this float a, float b, float value)
        {
            return a * (1.0F - value) + b * value;
        }
    }
}

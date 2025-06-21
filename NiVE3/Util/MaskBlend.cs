using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shape;

namespace NiVE3.Util
{
    static class MaskBlend
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Process(MaskBlendMode blendMode, float back, float front)
        {
            return blendMode switch
            {
                MaskBlendMode.Subtract => Subtract(back, front),
                MaskBlendMode.Multiply => Multiply(back, front),
                MaskBlendMode.Darken => Darken(back, front),
                MaskBlendMode.Lighten => Lighten(back, front),
                MaskBlendMode.Difference => Difference(back, front),
                _ => Add(back, front)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Add(float back, float front)
        {
            return Math.Clamp(back + front, 0.0F, 1.0F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Subtract(float back, float front)
        {
            return Math.Clamp(back - front, 0.0F, 1.0F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Multiply(float back, float front)
        {
            return back * front;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Darken(float back, float front)
        {
            return Math.Min(back, front);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Lighten(float back, float front)
        {
            return Math.Max(back, front);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Difference(float back, float front)
        {
            return Math.Abs(back - front);
        }
    }
}

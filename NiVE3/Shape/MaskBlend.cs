using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shape
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

    enum MaskBlendMode
    {
        Add,
        Subtract,
        Multiply,
        Darken,
        Lighten,
        Difference
    }

    static class MaskBlendModeExtensions
    {
        /// <summary>
        /// 初期状態に1.0Fである必要があるもの
        /// </summary>
        /// <param name="blendMode"></param>
        /// <returns></returns>
        public static bool IsInverted(this MaskBlendMode blendMode)
        {
            return blendMode == MaskBlendMode.Subtract || blendMode == MaskBlendMode.Multiply;
        }
    }
}

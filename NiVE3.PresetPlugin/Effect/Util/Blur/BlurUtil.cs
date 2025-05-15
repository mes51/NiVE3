using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Effect.Blur;
using NiVE3.PresetPlugin.Effect.Util;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class BlurUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetPixelForY(ReadOnlySpan<Vector4> data, int width, int height, int t, int w, EdgeRepeatMode edgeRepeatMode)
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[CoordWrap.Wrap(t, height) * width + w];
                case EdgeRepeatMode.Repeat:
                    return data[CoordWrap.Repeat(t, height) * width + w];
                case EdgeRepeatMode.Mirror:
                    return data[CoordWrap.Mirror(t, height) * width + w];
                default:
                    if (t > -1 && t < height)
                    {
                        return data[t * width + w];
                    }
                    else
                    {
                        return Vector4.Zero;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetPixelForY<T>(ReadOnlySpan<T> data, int width, int height, int t, int w, EdgeRepeatMode edgeRepeatMode) where T : INumber<T>
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[CoordWrap.Wrap(t, height) * width + w];
                case EdgeRepeatMode.Repeat:
                    return data[CoordWrap.Repeat(t, height) * width + w];
                case EdgeRepeatMode.Mirror:
                    return data[CoordWrap.Mirror(t, height) * width + w];
                default:
                    if (t > -1 && t < height)
                    {
                        return data[t * width + w];
                    }
                    else
                    {
                        return T.Zero;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetPixelForX(ReadOnlySpan<Vector4> data, int width, int l, int h, EdgeRepeatMode edgeRepeatMode)
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[h * width + CoordWrap.Wrap(l, width)];
                case EdgeRepeatMode.Repeat:
                    return data[h * width + CoordWrap.Repeat(l, width)];
                case EdgeRepeatMode.Mirror:
                    return data[h * width + CoordWrap.Mirror(l, width)];
                default:
                    if (l > -1 && l < width)
                    {
                        return data[h * width + l];
                    }
                    else
                    {
                        return Vector4.Zero;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetPixelForX<T>(ReadOnlySpan<T> data, int width, int l, int h, EdgeRepeatMode edgeRepeatMode) where T : INumber<T>
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[h * width + CoordWrap.Wrap(l, width)];
                case EdgeRepeatMode.Repeat:
                    return data[h * width + CoordWrap.Repeat(l, width)];
                case EdgeRepeatMode.Mirror:
                    return data[h * width + CoordWrap.Mirror(l, width)];
                default:
                    if (l > -1 && l < width)
                    {
                        return data[h * width + l];
                    }
                    else
                    {
                        return T.Zero;
                    }
            }
        }
    }
}

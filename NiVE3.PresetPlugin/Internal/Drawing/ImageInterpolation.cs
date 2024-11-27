using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Effect.Util;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    static class ImageInterpolation
    {
        static readonly Vector4 EmptyPixel = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 NearestNeighbor(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            return NearestNeighbor(texture, width, height, x, y, EmptyPixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 NearestNeighbor(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y, in Vector4 defaultColor)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix > -1 && iy > -1 && ix < width && iy < height)
            {
                return texture[iy * width + ix];
            }
            else
            {
                return defaultColor;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 NearestNeighborLoop(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            return texture[CoordWrap.Repeat(iy, height) * width + CoordWrap.Repeat(ix, width)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Bilinear(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            return Bilinear(texture, width, height, x, y, EmptyPixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Bilinear(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y, in Vector4 defaultColor)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return texture[iy * width + ix];
                }
                else
                {
                    return defaultColor;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return defaultColor;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = defaultColor;
            var c2 = defaultColor;
            var c3 = defaultColor;
            var c4 = defaultColor;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = texture[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = texture[pos];
                            c4 = texture[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = texture[pos];
                        c4 = texture[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        if (iy < mh)
                        {
                            c3 = texture[pos + width];
                        }
                    }
                    else
                    {
                        c3 = texture[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = texture[pos];
                    if (iy < mh)
                    {
                        c4 = texture[pos + width];
                    }
                }
                else
                {
                    c4 = texture[pos + width];
                }
            }

            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return defaultColor;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Bilinear(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y, in Vector4 defaultColor, BilinearEdgeMode edgeRepeatMode)
        {
            switch (edgeRepeatMode)
            {
                case BilinearEdgeMode.Wrap:
                    return BilinearEdgeRepeat(texture, width, height, x, y);
                case BilinearEdgeMode.Repeat:
                    return BilinearLoop(texture, width, height, x, y);
                case BilinearEdgeMode.Mirror:
                    return BilinearMirror(texture, width, height, x, y);
                default:
                    return Bilinear(texture, width, height, x, y, defaultColor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BilinearEdgeRepeat(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                return texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix, width)];
            }

            var c1 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix, width)];
            var c2 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix + 1, width)];
            var c3 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Wrap(ix, width)];
            var c4 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Wrap(ix + 1, width)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return EmptyPixel;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BilinearLoop(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                return texture[CoordWrap.Repeat(iy, height) * width + CoordWrap.Repeat(ix, width)];
            }

            var c1 = texture[CoordWrap.Repeat(iy, height) * width + CoordWrap.Repeat(ix, width)];
            var c2 = texture[CoordWrap.Repeat(iy, height) * width + CoordWrap.Repeat(ix + 1, width)];
            var c3 = texture[CoordWrap.Repeat(iy + 1, height) * width + CoordWrap.Repeat(ix, width)];
            var c4 = texture[CoordWrap.Repeat(iy + 1, height) * width + CoordWrap.Repeat(ix + 1, width)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return EmptyPixel;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BilinearMirror(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                return texture[CoordWrap.Mirror(iy, height) * width + CoordWrap.Mirror(ix, width)];
            }

            var c1 = texture[CoordWrap.Mirror(iy, height) * width + CoordWrap.Mirror(ix, width)];
            var c2 = texture[CoordWrap.Mirror(iy, height) * width + CoordWrap.Mirror(ix + 1, width)];
            var c3 = texture[CoordWrap.Mirror(iy + 1, height) * width + CoordWrap.Mirror(ix, width)];
            var c4 = texture[CoordWrap.Mirror(iy + 1, height) * width + CoordWrap.Mirror(ix + 1, width)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return EmptyPixel;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }

        /// <summary>
        /// 補間時のみ、端の色をリピートする
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BilinearInflateEdge(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y)
        {
            return BilinearInflateEdge(texture, width, height, x, y, EmptyPixel);
        }
        
        /// <summary>
        /// 補間時のみ、端の色をリピートする
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BilinearInflateEdge(ReadOnlySpan<Vector4> texture, int width, int height, float x, float y, in Vector4 defaultColor)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return texture[iy * width + ix];
                }
                else
                {
                    return defaultColor;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return defaultColor;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            Vector4 c1;
            Vector4 c2;
            Vector4 c3;
            Vector4 c4;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = texture[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = texture[pos];
                            c4 = texture[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = texture[pos];
                        c4 = texture[pos + 1];
                        c1 = c3;
                        c2 = c4;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = texture[pos + width];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = texture[pos + width];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = texture[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = texture[pos + width];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = texture[pos + width];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return defaultColor;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }
    }

    enum BilinearEdgeMode
    {
        None,
        Wrap,
        Repeat,
        Mirror
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Drawing
{
    /// <summary>
    /// ブレンド処理を提供します
    /// </summary>
    public static class Blend
    {
        static readonly Vector128<float> Half128 = Vector128.Create(0.5F);

        static readonly Vector128<float> One128 = Vector128.Create(1.0F);

        static readonly Vector4 ConvertToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        static readonly Vector4 Two = new Vector4(2.0F, 2.0F, 2.0F, 2.0F);

        /// <summary>
        /// 指定したブレンドモードで色を合成します
        /// </summary>
        /// <param name="blendMode">使用するブレンドモード</param>
        /// <param name="back">背景色</param>
        /// <param name="front">合成する色</param>
        /// <returns>合成後の色</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Process(BlendMode blendMode, in Vector4 back, in Vector4 front)
        {
            if (back.W <= 0.0F)
            {
                return front;
            }
            else if (front.W <= 0.0F)
            {
                return back;
            }

            return blendMode switch
            {
                BlendMode.Replace => front,
                BlendMode.Add => Add(back, front),
                BlendMode.Subtract => Subtract(back, front),
                BlendMode.Multiply => Multiply(back, front),
                BlendMode.Screen => Screen(back, front),
                BlendMode.Overlay => Overlay(back, front),
                BlendMode.HardLight => HardLight(back, front),
                BlendMode.SoftLight => SoftLight(back, front),
                BlendMode.VividLight => VividLight(back, front),
                BlendMode.LinearLight => LinearLight(back, front),
                BlendMode.PinLight => PinLight(back, front),
                BlendMode.ColorDodge => ColorDodge(back, front),
                BlendMode.LinearDodge => LinearDodge(back, front),
                BlendMode.ColorBurn => ColorBurn(back, front),
                BlendMode.LinearBurn => LinearBurn(back, front),
                BlendMode.Darken => Darken(back, front),
                BlendMode.Lighten => Lighten(back, front),
                BlendMode.Difference => Difference(back, front),
                BlendMode.Exclusion => Exclusion(back, front),
                BlendMode.Hue => Hue(back, front),
                BlendMode.Saturation => Saturation(back, front),
                BlendMode.Color => Color(back, front),
                BlendMode.Luminance => Luminance(back, front),
                _ => Normal(back, front),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Normal(in Vector4 back, in Vector4 front)
        {
            var ra = back.W + front.W - back.W * front.W;
            var invRa = 1.0F / ra;
            var result = (front * front.W + (1.0F - front.W) * back * back.W) * invRa;
            result.W = ra;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Composite(in Vector4 back, in Vector4 front, in Vector4 convertedFront)
        {
            var fba = front.W * back.W;
            var ra = back.W + front.W - fba;
            var invRa = 1.0F / ra;
            var result = (fba * convertedFront + (front.W - fba) * front + (back.W - fba) * back) * invRa;
            result.W = ra;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Add(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Min(front + back, Vector4.One);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Subtract(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Max(front - back, Vector4.Zero);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Multiply(in Vector4 back, in Vector4 front)
        {
            return Composite(back, front, front * back);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Screen(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.One - (Vector4.One - back) * (Vector4.One - front);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Overlay(in Vector4 back, in Vector4 front)
        {
            var mask = Sse.CompareLessThan(back.AsVector128(), Half128);
            var lt = 2.0F * back * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - back) * (Vector4.One - front);

            var c = Sse.Add(
                Sse.And(mask, lt.AsVector128()),
                Sse.AndNot(mask, gte.AsVector128())
            ).AsVector4();

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 HardLight(in Vector4 back, in Vector4 front)
        {
            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);
            var lt = 2.0F * back * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - back) * (Vector4.One - front);

            var c = Sse.Add(
                Sse.And(mask, lt.AsVector128()),
                Sse.AndNot(mask, gte.AsVector128())
            ).AsVector4();

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SoftLight(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var bv128 = back.AsVector128();

            var mask = Sse.CompareLessThan(fv128, Half128);
            var lt = bv128.Pow((2.0F * (Vector4.One - front)).AsVector128());
            var gte = bv128.Pow((Vector4.One / (2.0F * front)).AsVector128());

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 VividLight(in Vector4 back, in Vector4 front)
        {
            var bv128 = back.AsVector128();

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);

            var fv = front * 2.0F;
            var ltMask = Sse.CompareLessThanOrEqual(bv128, (Vector4.One - fv).AsVector128());
            var gteMask = Sse.CompareLessThan(bv128, (Two - fv).AsVector128());
            var lt = Sse.And(ltMask, (back - (Vector4.One - fv) / fv).AsVector128());
            var gte = Sse.Add(
                Sse.And(gteMask, (back / (Two - fv)).AsVector128()),
                Sse.AndNot(gteMask, Vector128.Create(1.0F))
            );

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            return Composite(back, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearLight(in Vector4 back, in Vector4 front)
        {
            var bv128 = back.AsVector128();

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);

            var fv = front * 2.0F;
            var ltMask = Sse.CompareLessThan(bv128, (Vector4.One - fv).AsVector128());
            var gteMask = Sse.CompareLessThan(bv128, (Two - fv).AsVector128());
            var tmp = (fv + back - Vector4.One).AsVector128();
            var lt = Sse.And(ltMask, tmp);
            var gte = Sse.Add(
                Sse.And(gteMask, tmp),
                Sse.AndNot(gteMask, Vector128.Create(1.0F))
            );

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            return Composite(back, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 PinLight(in Vector4 back, in Vector4 front)
        {
            var bv128 = back.AsVector128();

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);

            var fv = front * 2.0F;
            var fv128 = fv.AsVector128();

            var ltMask = Sse.CompareLessThan(fv128, bv128);
            var gteMask = Sse.CompareLessThan(Sse.Subtract(fv128, Vector128.Create(1.0F)), bv128);

            var lt = Sse.Add(
                Sse.And(ltMask, fv128),
                Sse.AndNot(ltMask, bv128)
            );
            var gte = Sse.Add(
                Sse.And(gteMask, bv128),
                Sse.AndNot(gteMask, Sse.Subtract(fv128, Vector128.Create(1.0F)))
            );

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            return Composite(back, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ColorDodge(in Vector4 back, in Vector4 front)
        {
            var c256 = new Vector4(1.00392156862745F);
            var c = Vector4.Min((c256 * back) / (c256 - front), Vector4.One);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearDodge(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Min(front + back, Vector4.One);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ColorBurn(in Vector4 back, in Vector4 front)
        {
            var mask = Sse.CompareLessThan((front + back).AsVector128(), One128);

            var lteInnerMask = Sse.CompareLessThan(front.AsVector128(), Vector128<float>.Zero);

            var lteInner = (Vector4.One - (Vector4.One - back) / front).AsVector128();

            var c = Sse.AndNot(
                mask,
                Sse.Add(
                    Sse.And(lteInnerMask, lteInner),
                    Sse.AndNot(lteInnerMask, One128)
                )
            ).AsVector4();

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearBurn(in Vector4 back, in Vector4 front)
        {
            var mask = Sse.CompareLessThan((front + back).AsVector128(), One128);

            var c = Sse.AndNot(
                mask,
                (front + back - Vector4.One).AsVector128()
            ).AsVector4();

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Darken(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Min(front, back);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Lighten(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Max(front, back);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Difference(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Abs(front - back);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Exclusion(in Vector4 back, in Vector4 front)
        {
            var c = ((Vector4.One - front) * back + (Vector4.One - back) * front);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Hue(in Vector4 back, in Vector4 front)
        {
            var luminance = GetLuminance(back);
            var c = SetLuminance(SetSaturation(front, luminance), luminance);

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Saturation(in Vector4 back, in Vector4 front)
        {
            var c = SetLuminance(SetSaturation(back, GetSaturation(front)), GetLuminance(back));
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Color(in Vector4 back, in Vector4 front)
        {
            var c = SetLuminance(front, GetLuminance(back));
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Luminance(in Vector4 back, in Vector4 front)
        {
            var c = SetLuminance(back, GetLuminance(front));
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ClipColor(in Vector4 c)
        {
            var l = GetLuminance(c);
            var lv = new Vector4(l, l, l, 0.0F);
            var n = c.HorizontalMinBy3Element();
            if (n < 0.0F)
            {
                return lv + (((c - lv) * l) / (l - n));
            }
            else
            {
                var x = c.HorizontalMaxBy3Element();
                return lv + (((c - lv) * (1.0F - l)) / (x - l));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SetLuminance(in Vector4 c, float luminance)
        {
            var d = luminance - GetLuminance(c);
            return ClipColor(c + new Vector4(d, d, d, 0.0F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SetSaturation(Vector4 c, float saturation)
        {
            var min = c.HorizontalMinBy3Element();
            var max = c.HorizontalMaxBy3Element();

            if (max > min)
            {
                if (max == c.Z)
                {
                    if (min == c.Y)
                    {
                        c.X = ((c.X - c.Y) * saturation) / (c.Z - c.Y);
                        c.Y = 0.0F;
                    }
                    else
                    {
                        c.Y = ((c.Y - c.X) * saturation) / (c.Z - c.X);
                        c.X = 0.0F;
                    }
                    c.Z = saturation;
                }
                else if (max == c.Y)
                {
                    if (min == c.X)
                    {
                        c.Z = ((c.Z - c.X) * saturation) / (c.Y - c.X);
                        c.X = 0.0F;
                    }
                    else
                    {
                        c.X = ((c.X - c.Z) * saturation) / (c.Y - c.Z);
                        c.Z = 0.0F;
                    }
                    c.Y = saturation;
                }
                else
                {
                    if (min == c.Z)
                    {
                        c.Y = ((c.Y - c.Z) * saturation) / (c.X - c.Z);
                        c.Z = 0.0F;
                    }
                    else
                    {
                        c.Z = ((c.Z - c.Y) * saturation) / (c.X - c.Y);
                        c.Y = 0.0F;
                    }
                    c.X = saturation;
                }
                return c;
            }
            else
            {
                return Vector4.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetLuminance(in Vector4 c)
        {
            return (c * ConvertToGrayScale).HorizontalAdd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetSaturation(in Vector4 c)
        {
            var c128 = c.AsVector128();
            return Sse.Subtract(c128.HorizontalMaxBy3Element(), c128.HorizontalMinBy3Element()).GetElement(0);
        }
    }

    /// <summary>
    /// ブレンドモードを表します。
    /// </summary>
    public enum BlendMode
    {
        Normal,
        Replace,
        Add,
        Subtract,
        Multiply,
        Screen,
        Overlay,
        HardLight,
        SoftLight,
        VividLight,
        LinearLight,
        PinLight,
        ColorDodge,
        LinearDodge,
        ColorBurn,
        LinearBurn,
        Darken,
        Lighten,
        Difference,
        Exclusion,
        Hue,
        Saturation,
        Color,
        Luminance
    }
}

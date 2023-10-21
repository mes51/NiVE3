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
using NiVE3.Plugin.Interfaces;

namespace NiVE3.PresetPlugin.Internal.Drawing
{
    static class Blend
    {
        static readonly Vector128<float> Half128 = Vector128.Create(0.5F);

        static readonly Vector128<float> One128 = Vector128.Create(1.0F);

        static readonly Vector4 ConvertToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        static readonly Vector4 Two = new Vector4(2.0F, 2.0F, 2.0F, 2.0F);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Process(BlendMode blendMode, Span<Vector4> back, in Vector4 front, int pos)
        {
            switch (blendMode)
            {
                case BlendMode.Replace:
                    back[pos] = front;
                    break;
                case BlendMode.Add:
                    Blend.Add(back, front, pos);
                    break;
                case BlendMode.Subtract:
                    Blend.Subtract(back, front, pos);
                    break;
                case BlendMode.Multiply:
                    Blend.Multiply(back, front, pos);
                    break;
                case BlendMode.Screen:
                    Blend.Screen(back, front, pos);
                    break;
                case BlendMode.Overlay:
                    Blend.Overlay(back, front, pos);
                    break;
                case BlendMode.HardLight:
                    Blend.HardLight(back, front, pos);
                    break;
                case BlendMode.SoftLight:
                    Blend.SoftLight(back, front, pos);
                    break;
                case BlendMode.VividLight:
                    Blend.VividLight(back, front, pos);
                    break;
                case BlendMode.LinearLight:
                    Blend.LinearLight(back, front, pos);
                    break;
                case BlendMode.PinLight:
                    Blend.PinLight(back, front, pos);
                    break;
                case BlendMode.ColorDodge:
                    Blend.ColorDodge(back, front, pos);
                    break;
                case BlendMode.LinearDodge:
                    Blend.LinearDodge(back, front, pos);
                    break;
                case BlendMode.ColorBurn:
                    Blend.ColorBurn(back, front, pos);
                    break;
                case BlendMode.LinearBurn:
                    Blend.LinearBurn(back, front, pos);
                    break;
                case BlendMode.Darken:
                    Blend.Darken(back, front, pos);
                    break;
                case BlendMode.Lighten:
                    Blend.Lighten(back, front, pos);
                    break;
                case BlendMode.Difference:
                    Blend.Difference(back, front, pos);
                    break;
                case BlendMode.Exclusion:
                    Blend.Exclusion(back, front, pos);
                    break;
                case BlendMode.Hue:
                    Blend.Hue(back, front, pos);
                    break;
                case BlendMode.Saturation:
                    Blend.Saturation(back, front, pos);
                    break;
                case BlendMode.Color:
                    Blend.Color(back, front, pos);
                    break;
                case BlendMode.Luminance:
                    Blend.Luminance(back, front, pos);
                    break;
                default:
                    Blend.Normal(back, front, pos);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Normal(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var ra = bv.W + front.W - bv.W * front.W;
            var invRa = 1.0F / ra;
            var result = (front * front.W + (1.0F - front.W) * bv * bv.W) * invRa;
            result.W = ra;

            back[pos] = result;
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
        static void Add(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.Min(front + bv, Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Subtract(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.Max(front + bv, Vector4.Zero);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Multiply(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            back[pos] = Composite(bv, front, front * bv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Screen(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.One - (Vector4.One - bv) * (Vector4.One - front);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Overlay(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var mask = Sse.CompareLessThan(bv.AsVector128(), Half128);
            var lt = 2.0F * bv * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - bv) * (Vector4.One - front);

            var c = Sse.Add(
                Sse.And(mask, lt.AsVector128()),
                Sse.AndNot(mask, gte.AsVector128())
            ).AsVector4();

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HardLight(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);
            var lt = 2.0F * bv * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - bv) * (Vector4.One - front);

            var c = Sse.Add(
                Sse.And(mask, lt.AsVector128()),
                Sse.AndNot(mask, gte.AsVector128())
            ).AsVector4();

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SoftLight(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var fv128 = front.AsVector128();
            var bv128 = bv.AsVector128();

            var mask = Sse.CompareLessThan(fv128, Half128);
            var lt = bv128.Pow((2.0F * (Vector4.One - front)).AsVector128());
            var gte = bv128.Pow((Vector4.One / (2.0F * front)).AsVector128());

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void VividLight(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var bv128 = bv.AsVector128();

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);

            var fv = front * 2.0F;
            var ltMask = Sse.CompareLessThanOrEqual(bv128, (Vector4.One - fv).AsVector128());
            var gteMask = Sse.CompareLessThan(bv128, (Two - fv).AsVector128());
            var lt = Sse.And(ltMask, (bv - (Vector4.One - fv) / fv).AsVector128());
            var gte = Sse.Add(
                Sse.And(gteMask, (bv / (Two - fv)).AsVector128()),
                Sse.AndNot(gteMask, Vector128.Create(1.0F))
            );

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            back[pos] = Composite(bv, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LinearLight(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var bv128 = bv.AsVector128();

            var mask = Sse.CompareLessThan(front.AsVector128(), Half128);

            var fv = front * 2.0F;
            var ltMask = Sse.CompareLessThan(bv128, (Vector4.One - fv).AsVector128());
            var gteMask = Sse.CompareLessThan(bv128, (Two - fv).AsVector128());
            var tmp = (fv + bv - Vector4.One).AsVector128();
            var lt = Sse.And(ltMask, tmp);
            var gte = Sse.Add(
                Sse.And(gteMask, tmp),
                Sse.AndNot(gteMask, Vector128.Create(1.0F))
            );

            var c = Sse.Add(
                Sse.And(mask, lt),
                Sse.AndNot(mask, gte)
            ).AsVector4();

            back[pos] = Composite(bv, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void PinLight(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var bv128 = bv.AsVector128();

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

            back[pos] = Composite(bv, fv, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ColorDodge(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c256 = new Vector4(1.00392156862745F);

            var c = Vector4.Min((c256 * bv) / (c256 - front), Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LinearDodge(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Min(front + bv, Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ColorBurn(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var mask = Sse.CompareLessThan((front + bv).AsVector128(), One128);

            var lteInnerMask = Sse.CompareLessThan(front.AsVector128(), Vector128<float>.Zero);

            var lteInner = (Vector4.One - (Vector4.One - bv) / front).AsVector128();

            var c = Sse.AndNot(
                mask,
                Sse.Add(
                    Sse.And(lteInnerMask, lteInner),
                    Sse.AndNot(lteInnerMask, One128)
                )
            ).AsVector4();

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LinearBurn(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var mask = Sse.CompareLessThan((front + bv).AsVector128(), One128);

            var c = Sse.AndNot(
                mask,
                (front + bv - Vector4.One).AsVector128()
            ).AsVector4();

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Darken(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Min(front, bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Lighten(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Max(front, bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Difference(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Abs(front - bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Exclusion(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = ((Vector4.One - front) * bv + (Vector4.One - bv) * front);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Hue(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var luminance = GetLuminance(bv);
            var c = SetLuminance(SetSaturation(front, luminance), luminance);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Saturation(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = SetLuminance(SetSaturation(bv, GetSaturation(front)), GetLuminance(bv));

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Color(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = SetLuminance(front, GetLuminance(bv));

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Luminance(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = SetLuminance(bv, GetLuminance(front));

            back[pos] = Composite(bv, front, c);
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
}

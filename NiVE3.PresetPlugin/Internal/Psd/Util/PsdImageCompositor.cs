using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Internal.Psd.Structs;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Psd.Util
{
    static class PsdImageCompositor
    {
        const uint DissolveSeed = 4389545U;

        const uint DissolveZ = 985472U;

        static readonly Vector128<float> Half128 = Vector128.Create(0.5F);

        public static void Blend(NManagedImage back, NManagedImage front, RectTLBR bounds, float opacity, BlendModeType blendMode)
        {
            if (opacity <= 0.0F)
            {
                return;
            }

            var imageWidth = back.Width;
            var backData = back.Data;
            var frontData = front.Data;
            switch (blendMode)
            {
                case BlendModeType.Dissolve:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            var rate = fc.W * opacity;
                            if (NoiseFunction.Pcg3D1FloatCpu((uint)x, (uint)y, DissolveZ, DissolveSeed) > rate)
                            {
                                backDataSpan[x] = fc;
                            }
                        }
                    });
                    break;
                case BlendModeType.Darken:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Darken(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Multiply:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Multiply(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.ColorBurn:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = ColorBurn(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.LinearBurn:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = LinearBurn(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.DarkerColor:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = DarkerColor(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Lighten:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Lighten(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Screen:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Screen(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.ColorDodge:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = ColorDodge(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.LinearDodge:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = LinearDodge(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.LighterColor:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = LighterColor(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Overlay:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Overlay(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.SoftLight:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = SoftLight(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.HardLight:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = HardLight(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.VividLight:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = VividLight(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.LinearLight:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = LinearLight(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.PinLight:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = PinLight(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.HardMix:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = HardMix(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Difference:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Difference(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Exclusion:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Exclusion(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Subtract:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Subtract(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Divide:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Divide(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Hue:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Hue(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Saturation:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Saturation(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Color:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Color(bc, fc);
                            }
                        }
                    });
                    break;
                case BlendModeType.Luminosity:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Luminosity(bc, fc);
                            }
                        }
                    });
                    break;
                default:
                    Parallel.For(bounds.Top, bounds.Bottom, y =>
                    {
                        var backDataSpan = backData.AsSpan(y * imageWidth, imageWidth);
                        var frontDataSpan = frontData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = bounds.Left; x < bounds.Right; x++)
                        {
                            var fc = frontDataSpan[x];
                            fc.W *= opacity;
                            if (fc.W <= 0.0F)
                            {
                                continue;
                            }
                            var bc = backDataSpan[x];
                            if (bc.W <= 0.0F)
                            {
                                backDataSpan[x] = fc;
                            }
                            else
                            {
                                backDataSpan[x] = Normal(bc, fc);
                            }
                        }
                    });
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Composite(in Vector4 back, in Vector4 front, in Vector4 convertedFront)
        {
            var fba = front.W * back.W;
            var ra = back.W + front.W - fba;
            var result = (fba * convertedFront + (front.W - fba) * front + (back.W - fba) * back) / ra;
            result.W = ra;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Normal(in Vector4 back, in Vector4 front)
        {
            var ra = back.W + front.W - back.W * front.W;
            var result = (front * front.W + (1.0F - front.W) * back * back.W) / ra;
            result.W = ra;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Darken(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Min(front, back);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Multiply(in Vector4 back, in Vector4 front)
        {
            return Composite(back, front, front * back);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ColorBurn(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var bv128 = back.AsVector128();

            var g1 = Vector128.GreaterThanOrEqual(bv128, Vector128<float>.One);
            var l0 = Vector128.LessThanOrEqual(fv128, Vector128<float>.Zero);
            var mask = (g1.Not() & l0.Not());

            var c = Vector128<float>.One - (Vector128<float>.One - bv128) / fv128;
            return Composite(back, front, ((g1 & Vector128<float>.One) + (mask & c)).AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearBurn(in Vector4 back, in Vector4 front)
        {
            var mask = Vector128.GreaterThanOrEqual((front + back).AsVector128(), Vector128<float>.One);
            var c = (mask & (front + back - Vector4.One).AsVector128()).AsVector4();
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 DarkerColor(in Vector4 back, in Vector4 front)
        {
            var bl = Vector4.Dot(back, Const.ConvertToGrayScale);
            var fl = Vector4.Dot(front, Const.ConvertToGrayScale);
            var c = bl > fl ? front : back;

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Lighten(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.Max(front, back);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Screen(in Vector4 back, in Vector4 front)
        {
            var c = Vector4.One - (Vector4.One - back) * (Vector4.One - front);
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ColorDodge(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var bv128 = back.AsVector128();

            var g1 = Vector128.GreaterThanOrEqual(fv128, Vector128<float>.One);
            var l0 = Vector128.LessThanOrEqual(bv128, Vector128<float>.Zero);
            var mask = (g1.Not() & l0.Not());

            var c = bv128 / (Vector128<float>.One - fv128);
            return Composite(back, front, ((Vector128<float>.One & g1) + (c & mask)).AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearDodge(in Vector4 back, in Vector4 front)
        {
            // add
            return Composite(back, front, front + back);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LighterColor(in Vector4 back, in Vector4 front)
        {
            var bl = Vector4.Dot(back, Const.ConvertToGrayScale);
            var fl = Vector4.Dot(front, Const.ConvertToGrayScale);
            var c = bl < fl ? front : back;

            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Overlay(in Vector4 back, in Vector4 front)
        {
            var mask = Vector128.LessThan(back.AsVector128(), Half128);
            var lt = 2.0F * back * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - back) * (Vector4.One - front);

            var c = (mask & lt.AsVector128()) + (mask.Not() & gte.AsVector128());
            return Composite(back, front, c.AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SoftLight(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var bv128 = back.AsVector128();

            var mask = Vector128.LessThan(fv128, Half128);
            var lt = (fv128 * 2.0F - Vector128<float>.One) * (bv128 - bv128 * bv128) + bv128;
            var gte = (fv128 * 2.0F - Vector128<float>.One) * (bv128.Pow(Vector128.Create(0.5F)) - bv128) + bv128;

            var c = (mask & lt) + (mask.Not() & gte);
            return Composite(back, front, c.AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 HardLight(in Vector4 back, in Vector4 front)
        {
            var mask = Vector128.LessThan(front.AsVector128(), Half128);
            var lt = 2.0F * back * front;
            var gte = Vector4.One - 2.0F * (Vector4.One - back) * (Vector4.One - front);

            var c = (mask & lt.AsVector128()) + (mask.Not() & gte.AsVector128());
            return Composite(back, front, c.AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 VividLight(in Vector4 back, in Vector4 front)
        {
            var mask = Vector128.LessThan(front.AsVector128(), Half128);

            var lt = (Vector4.One - (Vector4.One - back) / (front * 2.0F)).AsVector128();
            var gte = (back / ((Vector4.One - front) * 2.0F)).AsVector128();

            var c = (mask & lt) + (mask.Not() & gte);
            return Composite(back, front, c.AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 LinearLight(in Vector4 back, in Vector4 front)
        {
            var c = back + 2.0F * front - Vector4.One;
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 PinLight(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var bv128 = back.AsVector128();

            var pa = fv128 * 2.0F - Vector128<float>.One;
            var pc = fv128 * 2.0F;

            var paMask = Vector128.LessThan(bv128, pa);
            var pcMask = Vector128.GreaterThan(bv128, pc);
            var pbMask = (pa | pc).Not();

            var c = ((paMask & pa) + (pbMask & bv128) + (pcMask & pc)).AsVector4();
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 HardMix(in Vector4 back, in Vector4 front)
        {
            var mask = Vector128.GreaterThan(front.AsVector128(), Vector128<float>.One - back.AsVector128());
            var c = (mask & Vector128<float>.One).AsVector4();
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
            var c = back + front - back * front * 2.0F;
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Subtract(in Vector4 back, in Vector4 front)
        {
            return Composite(back, front, front - back);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Divide(in Vector4 back, in Vector4 front)
        {
            var fv128 = front.AsVector128();
            var mask = Vector128.Equals(fv128, Vector128<float>.Zero).Not();
            return Composite(back, front, Vector128.Min(Vector128.Max(((back / front).AsVector128() & mask), Vector128<float>.Zero), Vector128<float>.One).AsVector4());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 Hue(in Vector4 back, in Vector4 front)
        {
            var c = SetLuminance(SetSaturation(front, GetSaturation(back)), GetLuminance(back));
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
        static Vector4 Luminosity(in Vector4 back, in Vector4 front)
        {
            var c = SetLuminance(back, GetLuminance(front));
            return Composite(back, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 ClipColor(in Vector4 c)
        {
            var l = GetLuminance(c);
            var lv = new Vector4(l, l, l, 0.0F);
            var min = c.HorizontalMinBy3Element();
            var max = c.HorizontalMaxBy3Element();
            var result = c;
            if (min < 0.0F)
            {
                result = lv + ((result - lv) * l / (l - min));
            }
            if (max > 1.0F)
            {
                result = lv + ((result - lv) * (1.0F - l) / (max - l));
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 SetLuminance(in Vector4 c, float luminance)
        {
            var d = luminance - GetLuminance(c);
            return ClipColor(c + new Vector4(d, d, d, 0.0F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetLuminance(in Vector4 c)
        {
            return Vector4.Dot(c, Const.ConvertToGrayScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetSaturation(in Vector4 c)
        {
            var c128 = c.AsVector128();
            return (c128.HorizontalMaxBy3Element() - c128.HorizontalMinBy3Element()).GetElement(0);
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
                        c.X = ((c.X - c.Y) / (c.Z - c.Y)) * saturation;
                        c.Y = 0.0F;
                    }
                    else
                    {
                        c.Y = ((c.Y - c.X) / (c.Z - c.X)) * saturation;
                        c.X = 0.0F;
                    }
                    c.Z = saturation;
                }
                else if (max == c.Y)
                {
                    if (min == c.X)
                    {
                        c.Z = ((c.Z - c.X) / (c.Y - c.X)) * saturation;
                        c.X = 0.0F;
                    }
                    else
                    {
                        c.X = ((c.X - c.Z) / (c.Y - c.Z)) * saturation;
                        c.Z = 0.0F;
                    }
                    c.Y = saturation;
                }
                else
                {
                    if (min == c.Z)
                    {
                        c.Y = ((c.Y - c.Z) / (c.X - c.Z)) * saturation;
                        c.Z = 0.0F;
                    }
                    else
                    {
                        c.Z = ((c.Z - c.Y) / (c.X - c.Y)) * saturation;
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
    }
}

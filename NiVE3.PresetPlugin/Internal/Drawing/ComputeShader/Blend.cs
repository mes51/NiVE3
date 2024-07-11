using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader
{
    static class BlendMethods
    {
        static readonly Float3 ConvertToGrayScale = new Float3(0.114478F, 0.586611F, 0.298912F);

        public static Float4 Process(int blendMode, Float4 back, Float4 front)
        {
            if (back.W <= 0.0F)
            {
                return front;
            }
            else if (front.W <= 0.0F)
            {
                return back;
            }

            switch (blendMode)
            {
                case 1:
                    return front;
                case 2:
                    return Add(back, front);
                case 3:
                    return Subtract(back, front);
                case 4:
                    return Multiply(back, front);
                case 5:
                    return Screen(back, front);
                case 6:
                    return Overlay(back, front);
                case 7:
                    return HardLight(back, front);
                case 8:
                    return SoftLight(back, front);
                case 9:
                    return VividLight(back, front);
                case 10:
                    return LinearLight(back, front);
                case 11:
                    return PinLight(back, front);
                case 12:
                    return ColorDodge(back, front);
                case 13:
                    return LinearDodge(back, front);
                case 14:
                    return ColorBurn(back, front);
                case 15:
                    return LinearBurn(back, front);
                case 16:
                    return Darken(back, front);
                case 17:
                    return Lighten(back, front);
                case 18:
                    return Difference(back, front);
                case 19:
                    return Exclusion(back, front);
                case 20:
                    return Hue(back, front);
                case 21:
                    return Saturation(back, front);
                case 22:
                    return Color(back, front);
                case 23:
                    return Luminance(back, front);
                default:
                    return Normal(back, front);
            }
        }

        static Float4 Normal(Float4 back, Float4 front)
        {
            var ra = back.W + front.W - back.W * front.W;
            var invRa = 1.0F / ra;
            var result = (front * front.W + (1.0F - front.W) * back * back.W) * invRa;
            result.W = ra;

            return result;
        }

        static Float4 Add(Float4 back, Float4 front)
        {
            return Composite(back, front, front + back);
        }

        static Float4 Subtract(Float4 back, Float4 front)
        {
            return Composite(back, front, front - back);
        }

        static Float4 Multiply(Float4 back, Float4 front)
        {
            return Composite(back, front, front * back);
        }

        static Float4 Screen(Float4 back, Float4 front)
        {
            var c = 1.0F - (1.0F - back) * (1.0F - front);
            return Composite(back, front, c);
        }

        static Float4 Overlay(Float4 back, Float4 front)
        {
            var mask = back < 0.5F;
            var lt = 2.0F * back * front;
            var gte = 1.0F - 2.0F * (1.0F - back) * (1.0F - front);

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 HardLight(Float4 back, Float4 front)
        {
            var mask = front < 0.5F;
            var lt = 2.0F * back * front;
            var gte = 1.0F - 2.0F * (1.0F - back) * (1.0F - front);

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 SoftLight(Float4 back, Float4 front)
        {
            var mask = front < 0.5F;
            var lt = (front * 2.0F - 1.0F) * (back - Hlsl.Pow(back, 2.0F)) + back;
            var gte = (front * 2.0F - 1.0F) * (Hlsl.Pow(back, 0.5F) - back) + back;

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 VividLight(Float4 back, Float4 front)
        {
            var mask = front < 0.5F;
            var lt = 1.0F - (1.0F - back) / (front * 2.0F);
            var gte = back / ((1.0F - front) * 2.0F);

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 LinearLight(Float4 back, Float4 front)
        {
            var mask = front < 0.5F;

            var fv = front * 2.0F;
            var ltMask = back >= (1.0F - fv);
            var gteMask = back < (2.0F - fv);
            var tmp = fv + back - 1.0F;
            var lt = ShaderUtil.Mask(tmp, ltMask);
            var gte = ShaderUtil.Mask(tmp, gteMask) + ShaderUtil.NotMask(1.0F, gteMask);

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 PinLight(Float4 back, Float4 front)
        {
            var mask = front < 0.5F;

            var fv = front * 2.0F;
            var ltMask = fv < back;
            var gteMask = (fv - 1.0F) < back;

            var lt = ShaderUtil.Mask(fv, ltMask) + ShaderUtil.NotMask(back, ltMask);
            var gte = ShaderUtil.Mask(back, gteMask) + ShaderUtil.NotMask(fv - 1.0F, gteMask);

            var c = ShaderUtil.Mask(lt, mask) + ShaderUtil.NotMask(gte, mask);
            return Composite(back, front, c);
        }

        static Float4 ColorDodge(Float4 back, Float4 front)
        {
            var c = Hlsl.Min(back / (1.0F - front), 1.0F);
            return Composite(back, front, c);
        }

        static Float4 LinearDodge(Float4 back, Float4 front)
        {
            var c = Hlsl.Min(front + back, 1.0F);
            return Composite(back, front, c);
        }

        static Float4 ColorBurn(Float4 back, Float4 front)
        {
            var mask = front > 0.0F;
            var c = 1.0F - (1.0F - back) / front;
            return Composite(back, front, ShaderUtil.Mask(c, mask));
        }

        static Float4 LinearBurn(Float4 back, Float4 front)
        {
            var mask = (front + back) < 1.0F;
            var c = ShaderUtil.NotMask(front + back - 1.0F, mask);
            return Composite(back, front, c);
        }

        static Float4 Darken(Float4 back, Float4 front)
        {
            var c = Hlsl.Min(front, back);
            return Composite(back, front, c);
        }

        static Float4 Lighten(Float4 back, Float4 front)
        {
            var c = Hlsl.Max(front, back);
            return Composite(back, front, c);
        }

        static Float4 Difference(Float4 back, Float4 front)
        {
            var c = Hlsl.Abs(front - back);
            return Composite(back, front, c);
        }

        static Float4 Exclusion(Float4 back, Float4 front)
        {
            var c = back + front - back * front * 2.0F;
            return Composite(back, front, c);
        }

        static Float4 Hue(Float4 back, Float4 front)
        {
            var c = SetLuminance(SetSaturation(front, GetSaturation(back)), GetLuminance(back));
            return Composite(back, front, c);
        }

        static Float4 Saturation(Float4 back, Float4 front)
        {
            var c = SetLuminance(SetSaturation(back, GetSaturation(front)), GetLuminance(back));
            return Composite(back, front, c);
        }

        static Float4 Color(Float4 back, Float4 front)
        {
            var c = SetLuminance(front, GetLuminance(back));
            return Composite(back, front, c);
        }

        static Float4 Luminance(Float4 back, Float4 front)
        {
            var c = SetLuminance(back, GetLuminance(front));
            return Composite(back, front, c);
        }

        static Float4 Composite(Float4 back, Float4 front, Float4 convertedFront)
        {
            var fba = front.W * back.W;
            var ra = back.W + front.W - fba;
            var invRa = 1.0F / ra;
            var result = (fba * convertedFront + (front.W - fba) * front + (back.W - fba) * back) * invRa;
            result.W = ra;

            return result;
        }

        static Float4 ClipColor(Float4 c)
        {
            var l = GetLuminance(c);
            var lv = new Float4(l, l, l, 0.0F);
            var min = HorizontalMin(c.XYZ);
            var max = HorizontalMax(c.XYZ);
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

        static Float4 SetLuminance(Float4 c, float luminance)
        {
            var d = luminance - GetLuminance(c);
            return ClipColor(c + new Float4(d, d, d, 0.0F));
        }

        static Float4 SetSaturation(Float4 c, float saturation)
        {
            var min = HorizontalMin(c.XYZ);
            var max = HorizontalMax(c.XYZ);

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
                return 0.0F;
            }
        }

        static float GetSaturation(Float4 c)
        {
            return HorizontalMax(c.XYZ) - HorizontalMin(c.XYZ);
        }

        static float GetLuminance(Float4 c)
        {
            return Hlsl.Dot(c.XYZ, ConvertToGrayScale);
        }

        static float HorizontalMax(Float3 v)
        {
            return Hlsl.Max(Hlsl.Max(v.X, v.Y), v.Z);
        }

        static float HorizontalMin(Float3 v)
        {
            return Hlsl.Min(Hlsl.Min(v.X, v.Y), v.Z);
        }
    }
}

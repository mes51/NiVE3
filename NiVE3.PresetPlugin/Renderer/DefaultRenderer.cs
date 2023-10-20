using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Struct;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Renderer
{
    [Export(typeof(IRenderer))]
    [RendererMetadata(typeof(DefaultRenderer), LanguageResourceDictionary.Renderer_DefaultRenderer_Name, LanguageResourceDictionary.Renderer_DefaultRenderer_Description, "mes51", "D67AC3F-A137-45B1-99F7-3E68A0B910E6", LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class DefaultRenderer : IRenderer
    {
        int Width { get; set; }

        int Height { get; set; }

        NImage? CurrentFrame { get; set; }

        bool UseGpu { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void BeginRendering(double downSamplingRate, bool useGpu)
        {
            if (CurrentFrame != null)
            {
                throw new InvalidOperationException("rendering is already started"); // bug
            }

            CurrentFrame = new NManagedImage(Width, Height, true);
            UseGpu = useGpu;
        }

        public void Render(RenderableImage[] images)
        {
            if (CurrentFrame == null)
            {
                return;
            }

            foreach (var group in images.GroupByPrev(i => i.IsEnable3D))
            {
                if (group.First().IsEnable3D)
                {

                }
                else
                {
                    var renderer = new SoftwareRenderer2D(CurrentFrame);

                    foreach (var i in group)
                    {
                        var opacity = (double)(i.Transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) * 0.01;
                        var matrix = GetTransform2D(i.Transform);

                        foreach (var (type, parentTransform) in i.ParentTransforms)
                        {
                            matrix = GetTransform2D(parentTransform) * matrix;
                        }

                        renderer.Draw(i.Image, (float)opacity, matrix, i.InterpolationQuality, i.BlendMode);
                    }
                }
            }
        }

        public NImage GetCurrentRenderedImage()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }
            
            return CurrentFrame.Copy();
        }

        public NImage FinishRendering()
        {
            if (CurrentFrame == null)
            {
                throw new InvalidOperationException("rendering not started"); // bug
            }

            var result = CurrentFrame;
            CurrentFrame = null;
            return result;
        }

        Matrix3x3 GetTransform2D(PropertyValueGroup transformProperties)
        {
            var anchorPoint = (Vector3d)(transformProperties[ILayerObject.TransformAnchorPointId] ?? new Vector3d());
            var scale = (Vector3d)(transformProperties[ILayerObject.TransformScaleId] ?? new Vector3d()) * 0.01;
            var angle = (double)(transformProperties[ILayerObject.TransformZAngleId] ?? 0.0);
            var translate = (Vector3d)(transformProperties[ILayerObject.TransformTranslateId] ?? new Vector3d());
            return Matrix3x3.AffineTransform((Vector2)anchorPoint.AsVector2d(), (Vector2)scale.AsVector2d(), (float)angle, (Vector2)translate.AsVector2d());
        }

        public void Dispose()
        {
            CurrentFrame?.Dispose();
        }
    }

    class SoftwareRenderer2D
    {
        static readonly Vector4 EmptyPixel = new Vector4(255.0F, 255.0F, 255.0F, 0.0F);

        NImage Target { get; }

        public SoftwareRenderer2D(NImage target)
        {
            Target = target;
        }

        public void Draw(NImage image, float opacity, Matrix3x3 transform, ImageInterpolationQuality interpolationQuality, BlendMode blendMode)
        {
            if (!Matrix3x3.Invert(transform, out var inverted))
            {
                return;
            }

            var p1 = transform.Transform(new Vector2());
            var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
            var p3 = transform.Transform(new Vector2(image.Width, image.Height));
            var p4 = transform.Transform(new Vector2(0.0F, image.Height));
            var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
            var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
            var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), Target.Width);
            var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), Target.Height);

            var width = Target.Width;
            Parallel.For(minY, maxY, y =>
            {
                var targetData = MemoryMarshal.Cast<float, Vector4>(Target.GetData().AsSpan(0, Target.DataLength));
                var imageData = MemoryMarshal.Cast<float, Vector4>(image.GetData().AsSpan(0, image.DataLength));
                for (int x = minX, pos = y * Target.Width + minX; x < maxX; x++, pos++)
                {
                    var (imageX, imageY) = inverted.Transform(x, y);
                    var p = interpolationQuality switch
                    {
                        ImageInterpolationQuality.Level2 => Bilinear(imageData, image.Width, image.Height, imageX, imageY),
                        _ => NearestNeighbor(imageData, image.Width, image.Height, imageX, imageY)
                    };

                    p.W *= opacity;

                    switch (blendMode)
                    {
                        case BlendMode.Replace:
                            targetData[pos] = p;
                            break;
                        case BlendMode.Add:
                            Blend.Add(targetData, p, pos);
                            break;
                        case BlendMode.Subtract:
                            Blend.Subtract(targetData, p, pos);
                            break;
                        case BlendMode.Multiply:
                            Blend.Multiply(targetData, p, pos);
                            break;
                        case BlendMode.Screen:
                            Blend.Screen(targetData, p, pos);
                            break;
                        case BlendMode.Overlay:
                            Blend.Overlay(targetData, p, pos);
                            break;
                        case BlendMode.HardLight:
                            Blend.HardLight(targetData, p, pos);
                            break;
                        case BlendMode.SoftLight:
                            Blend.SoftLight(targetData, p, pos);
                            break;
                        case BlendMode.VividLight:
                            Blend.VividLight(targetData, p, pos);
                            break;
                        case BlendMode.LinearLight:
                            Blend.LinearLight(targetData, p, pos);
                            break;
                        case BlendMode.PinLight:
                            Blend.PinLight(targetData, p, pos);
                            break;
                        case BlendMode.ColorDodge:
                            Blend.ColorDodge(targetData, p, pos);
                            break;
                        case BlendMode.LinearDodge:
                            Blend.LinearDodge(targetData, p, pos);
                            break;
                        case BlendMode.ColorBurn:
                            Blend.ColorBurn(targetData, p, pos);
                            break;
                        case BlendMode.LinearBurn:
                            Blend.LinearBurn(targetData, p, pos);
                            break;
                        case BlendMode.Darken:
                            Blend.Darken(targetData, p, pos);
                            break;
                        case BlendMode.Lighten:
                            Blend.Lighten(targetData, p, pos);
                            break;
                        case BlendMode.Difference:
                            Blend.Difference(targetData, p, pos);
                            break;
                        case BlendMode.Exclusion:
                            Blend.Exclusion(targetData, p, pos);
                            break;
                        case BlendMode.Hue:
                            Blend.Hue(targetData, p, pos);
                            break;
                        case BlendMode.Saturation:
                            Blend.Saturation(targetData, p, pos);
                            break;
                        case BlendMode.Color:
                            Blend.Color(targetData, p, pos);
                            break;
                        case BlendMode.Luminance:
                            Blend.Luminance(targetData, p, pos);
                            break;
                        default:
                            Blend.Normal(targetData, p, pos);
                            break;
                    }
                }
            });
        }

        static Vector4 NearestNeighbor(Span<Vector4> texture, int width, int height, float x, float y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix > -1 && iy > -1 && ix < width && iy < height)
            {
                return texture[iy * width + ix];
            }
            else
            {
                return EmptyPixel;
            }
        }

        static Vector4 Bilinear(Span<Vector4> texture, int width, int height, float x, float y)
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
                    return EmptyPixel;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return EmptyPixel;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = EmptyPixel;
            var c2 = EmptyPixel;
            var c3 = EmptyPixel;
            var c4 = EmptyPixel;
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

            var ta = ((ip * ((iq * c1) + (qq * c3))) + (pp * ((iq * c2) + (qq * c4)))).W;
            var t = ((ip * ((iq * c1 * c1.W) + (qq * c3 * c3.W))) + (pp * ((iq * c2 * c2.W) + (qq * c4 * c4.W)))) / ta;
            t.W = ta;

            return t;
        }
    }

    static class Blend
    {
        static readonly Vector128<float> Half128 = Vector128.Create(0.5F);

        static readonly Vector128<float> One128 = Vector128.Create(1.0F);

        static readonly Vector4 ConvertToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        static readonly Vector4 Two = new Vector4(2.0F, 2.0F, 2.0F, 2.0F);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normal(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void Add(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.Min(front + bv, Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.Max(front + bv, Vector4.Zero);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            back[pos] = Composite(bv, front, front * bv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Screen(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c = Vector4.One - (Vector4.One - bv) * (Vector4.One - front);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Overlay(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void HardLight(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void SoftLight(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void VividLight(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void LinearLight(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void PinLight(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void ColorDodge(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];
            var c256 = new Vector4(1.00392156862745F);

            var c = Vector4.Min((c256 * bv) / (c256 - front), Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LinearDodge(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Min(front + bv, Vector4.One);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ColorBurn(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void LinearBurn(Span<Vector4> back, in Vector4 front, int pos)
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
        public static void Darken(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Min(front, bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Lighten(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Max(front, bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Difference(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = Vector4.Abs(front - bv);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exclusion(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = ((Vector4.One - front) * bv + (Vector4.One - bv) * front);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Hue(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var luminance = GetLuminance(bv);
            var c = SetLuminance(SetSaturation(front, luminance), luminance);

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Saturation(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = SetLuminance(SetSaturation(bv, GetSaturation(front)), GetLuminance(bv));

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Color(Span<Vector4> back, in Vector4 front, int pos)
        {
            var bv = back[pos];

            var c = SetLuminance(front, GetLuminance(bv));

            back[pos] = Composite(bv, front, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Luminance(Span<Vector4> back, in Vector4 front, int pos)
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

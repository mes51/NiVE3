using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_FreeTransform_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_FreeTransform_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class FreeTransform : IEffect
    {
        const string ID = "329A21B0-8026-4DD1-A7D9-A3F98BB5DE9B";

        const string PropertyLeftTopId = nameof(PropertyLeftTopId);

        const string PropertyRightTopId = nameof(PropertyRightTopId);

        const string PropertyLeftBottomId = nameof(PropertyLeftBottomId);

        const string PropertyRightBottomId = nameof(PropertyRightBottomId);

        static readonly Vector2 Epsilon = new Vector2(1E-7F);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new Vector3dProperty(PropertyLeftTopId, LanguageResourceDictionary.ResourceKeys.Distortion_FreeTransform_LeftTop, Vector3d.Zero, digit: 2, useInteraction: true),
                new Vector3dProperty(PropertyRightTopId, LanguageResourceDictionary.ResourceKeys.Distortion_FreeTransform_RightTop, new Vector3d(sourceSize.Width, 0.0, 0.0), digit: 2, useInteraction: true),
                new Vector3dProperty(PropertyLeftBottomId, LanguageResourceDictionary.ResourceKeys.Distortion_FreeTransform_LeftBottom, new Vector3d(0.0, sourceSize.Height, 0.0), digit: 2, useInteraction: true),
                new Vector3dProperty(PropertyRightBottomId, LanguageResourceDictionary.ResourceKeys.Distortion_FreeTransform_RightBottom, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0), digit: 2, useInteraction: true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var offset = new Vector2(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y);
            var scale = new Vector3d(downSamplingRateX, downSamplingRateY, 1.0);
            var As = (Vector2)(properties.GetValue(PropertyLeftBottomId, layerTime, Vector3d.Zero) / scale) + offset;
            var Bs = (Vector2)(properties.GetValue(PropertyLeftTopId, layerTime, Vector3d.Zero) / scale) + offset;
            var Cs = (Vector2)(properties.GetValue(PropertyRightTopId, layerTime, Vector3d.Zero) / scale) + offset;
            var Ds = (Vector2)(properties.GetValue(PropertyRightBottomId, layerTime, Vector3d.Zero) / scale) + offset;

            var Ad = new Vector2(0.0F, roi.OriginalImageSize.Height) + offset;
            var Bd = offset;
            var Cd = new Vector2(roi.OriginalImageSize.Width, 0.0F) + offset;
            var Dd = new Vector2(roi.OriginalImageSize.Width, roi.OriginalImageSize.Height) + offset;

            if (Vector2.LessThanAll(Vector2.Abs(Ad - As), Epsilon) && Vector2.LessThanAll(Vector2.Abs(Bd - Bs), Epsilon) && Vector2.LessThanAll(Vector2.Abs(Cd - Cs), Epsilon) && Vector2.LessThanAll(Vector2.Abs(Dd - Ds), Epsilon))
            {
                return image;
            }
            var a = Cs - Ds - Bs + As;
            var b = Ds - As;
            var c = Bs - As;
            var da = Vector2.Cross(a, c);
            var tdb = Vector2.Cross(c, b);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, As, Ad, Bd, Cd, Dd, a, b, c, da, tdb);
            }
            else
            {
                return ProcessCpu(image, roi, As, Ad, Bd, Cd, Dd, a, b, c, da, tdb);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 As, Vector2 Ad, Vector2 Bd, Vector2 Cd, Vector2 Dd, Vector2 a, Vector2 b, Vector2 c, float da, float tdb)
        {
            var managedImage = image.ToManaged();

            using var sourceImage = (NManagedImage)managedImage.Copy();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceData = sourceImage.Data;

            if (da == 0.0F)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var d = new Vector2(x, y) - As;
                        var db = Vector2.Cross(d, a) - tdb;
                        var dc = Vector2.Cross(d, b);

                        var t = db != 0.0F ? -1.0F * dc / db : 0.0F;
                        var ft = (a.X * t) + b.X;
                        var s = ft != 0.0F ? (d.X - (c.X * t)) / ft : 0.0F;
                        var GX = Ad.X + (t * (Bd.X - Ad.X));
                        var GY = Ad.Y + (t * (Bd.Y - Ad.Y));

                        imageDataSpan[x] = ImageInterpolation.Bilinear(sourceData, imageWidth, imageHeight, GX + (s * (Dd.X + (t * (Cd.X - Dd.X)) - GX)), GY + (s * (Dd.Y + (t * (Cd.Y - Dd.Y)) - GY)));
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var d = new Vector2(x, y) - As;
                        var db = Vector2.Cross(d, a) - tdb;
                        var dc = Vector2.Cross(d, b);

                        var D = db * db - 4.0F * da * dc;
                        if (D >= 0.0F)
                        {
                            var dd = MathF.Sqrt(D);
                            var t = (-db - dd) / (2.0F * da);
                            var ft = (a.X * t) + b.X;
                            var s = ft != 0.0F ? (d.X - (c.X * t)) / ft : 0.0F;
                            var GX = Ad.X + (t * (Bd.X - Ad.X));
                            var GY = Ad.Y + (t * (Bd.Y - Ad.Y));

                            imageDataSpan[x] = ImageInterpolation.Bilinear(sourceData, imageWidth, imageHeight, GX + (s * (Dd.X + (t * (Cd.X - Dd.X)) - GX)), GY + (s * (Dd.Y + (t * (Cd.Y - Dd.Y)) - GY)));
                        }
                        else
                        {
                            imageDataSpan[x] = Const.EmptyPixel;
                        }
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 As, Vector2 Ad, Vector2 Bd, Vector2 Cd, Vector2 Dd, Vector2 a, Vector2 b, Vector2 c, float da, float tdb)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            device.For(roi.Width, roi.Height, new FreeTransformProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, As, Ad, Bd, Cd, Dd, a, b, c, da, tdb, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FreeTransformProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> originalImage, Float2 As, Float2 Ad, Float2 Bd, Float2 Cd, Float2 Dd, Float2 a, Float2 b, Float2 c, float da, float tdb, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var d = new Float2(x, y) - As;
            var db = (d.X * a.Y) - tdb - (a.X * d.Y);
            var dc = (d.X * b.Y) - (b.X * d.Y);

            var t = 0.0F;
            var s = 0.0F;
            var GX = 0.0F;
            var GY = 0.0F;
            if (da == 0.0F)
            {
                if (db != 0.0F)
                {
                    t = -1.0F * dc / db;
                }
                var ft = (a.X * t) + b.X;
                if (ft != 0.0F)
                {
                    s = (d.X - (c.X * t)) / ft;
                }
                GX = Ad.X + (t * (Bd.X - Ad.X));
                GY = Ad.Y + (t * (Bd.Y - Ad.Y));
            }
            else
            {
                var D = db * db - 4.0F * da * dc;
                if (D >= 0.0F)
                {
                    var dd = Hlsl.Sqrt(D);
                    t = (-db - dd) / (2.0F * da);
                    var ft = (a.X * t) + b.X;
                    if (ft != 0.0F)
                    {
                        s = (d.X - (c.X * t)) / ft;
                    }
                    GX = Ad.X + (t * (Bd.X - Ad.X));
                    GY = Ad.Y + (t * (Bd.Y - Ad.Y));
                }
                else
                {
                    image[pos] = Const.EmptyPixelFloat4;
                    return;
                }
            }

            image[pos] = OriginalImageBilinear(GX + (s * (Dd.X + (t * (Cd.X - Dd.X)) - GX)), GY + (s * (Dd.Y + (t * (Cd.Y - Dd.Y)) - GY)));
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return Const.EmptyPixelFloat4;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return Const.EmptyPixelFloat4;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixelFloat4;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }
}

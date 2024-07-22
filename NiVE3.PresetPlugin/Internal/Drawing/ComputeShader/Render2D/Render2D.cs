using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Plugin.Interfaces;
using static Vanara.PInvoke.Kernel32;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render2D
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct Render2D(
        ReadWriteBuffer<Float4> target,
        int targetWidth,
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        int imageHeight,
        int interpolationQuality,
        ReadWriteBuffer<float> trackMatte,
        float opacity,
        int blendMode,
        Float3x3 transform
    ) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * targetWidth + ThreadIds.X;
            var imagePos = transform * new Float3(ThreadIds.X, ThreadIds.Y, 1.0F);

            var c = Float4.Zero;
            switch (interpolationQuality)
            {
                case 0:
                    c = NearestNeighbor(imagePos.XY);
                    break;
                default:
                    c = Bilinear(imagePos.XY);
                    break;
            }

            c.W *= trackMatte[pos % trackMatte.Length] * opacity;
            target[pos] = BlendMethods.Process(blendMode, target[pos], c);
        }

        Float4 NearestNeighbor(Float2 imagePos)
        {
            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
            {
                return image[iy * imageWidth + ix];
            }
            else
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }

        Float4 Bilinear(Float2 imagePos)
        {

            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix == imagePos.X && iy == imagePos.Y)
            {
                if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
                {
                    return image[iy * imageWidth + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= imageWidth || iy >= imageHeight)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = imagePos.X - ix;
            var qq = imagePos.Y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = imageWidth - 1;
            var mh = imageHeight - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var pos = iy * imageWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        c2 = image[pos + 1];
                        if (iy < mh)
                        {
                            pos += imageWidth;
                            c3 = image[pos];
                            c4 = image[pos + 1];
                        }
                    }
                    else
                    {
                        pos += imageWidth;
                        c3 = image[pos];
                        c4 = image[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        if (iy < mh)
                        {
                            c3 = image[pos + imageWidth];
                        }
                    }
                    else
                    {
                        c3 = image[pos + imageWidth];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = image[pos];
                    if (iy < mh)
                    {
                        c4 = image[pos + imageWidth];
                    }
                }
                else
                {
                    c4 = image[pos + imageWidth];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RenderMatte2D(
        ReadWriteBuffer<float> target,
        int targetWidth,
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        int imageHeight,
        int interpolationQuality,
        ReadWriteBuffer<float> trackMatte,
        float opacity,
        int trackMatteMode,
        Float3x3 transform
    ) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * targetWidth + ThreadIds.X;
            var imagePos = transform * new Float3(ThreadIds.X, ThreadIds.Y, 1.0F);

            var c = Float4.Zero;
            switch (interpolationQuality)
            {
                case 0:
                    c = NearestNeighbor(imagePos.XY);
                    break;
                default:
                    c = Bilinear(imagePos.XY);
                    break;
            }

            var p = 0.0F;
            switch (trackMatteMode)
            {
                case 0:
                    p = c.W;
                    break;
                case 1:
                    p = 1.0F - c.W;
                    break;
                case 2:
                    p = Hlsl.Dot(c.XYZ, Const.ConvertToGrayScaleFloat3) * c.W;
                    break;
                case 3:
                    p = 1.0F - (Hlsl.Dot(c.XYZ, Const.ConvertToGrayScaleFloat3) * c.W);
                    break;
            }

            target[pos] = p * opacity * trackMatte[pos % trackMatte.Length];
        }

        Float4 NearestNeighbor(Float2 imagePos)
        {
            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
            {
                return image[iy * imageWidth + ix];
            }
            else
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }

        Float4 Bilinear(Float2 imagePos)
        {

            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix == imagePos.X && iy == imagePos.Y)
            {
                if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
                {
                    return image[iy * imageWidth + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= imageWidth || iy >= imageHeight)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = imagePos.X - ix;
            var qq = imagePos.Y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = imageWidth - 1;
            var mh = imageHeight - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var pos = iy * imageWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        c2 = image[pos + 1];
                        if (iy < mh)
                        {
                            pos += imageWidth;
                            c3 = image[pos];
                            c4 = image[pos + 1];
                        }
                    }
                    else
                    {
                        pos += imageWidth;
                        c3 = image[pos];
                        c4 = image[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        if (iy < mh)
                        {
                            c3 = image[pos + imageWidth];
                        }
                    }
                    else
                    {
                        c3 = image[pos + imageWidth];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = image[pos];
                    if (iy < mh)
                    {
                        c4 = image[pos + imageWidth];
                    }
                }
                else
                {
                    c4 = image[pos + imageWidth];
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader.Render2D
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct NearestNeighbor(ReadWriteBuffer<Float4> target, int targetWidth, ReadWriteBuffer<Float4> image, int imageWidth, int imageHeight, Float3x3 transform) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * targetWidth + ThreadIds.X;
            var imagePos = transform * new Float3(ThreadIds.X, ThreadIds.Y, 1.0F);

            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
            {
                target[pos] = image[iy * imageWidth + ix];
            }
            else
            {
                target[pos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct Bilinear(ReadWriteBuffer<Float4> target, int targetWidth, ReadWriteBuffer<Float4> image, int imageWidth, int imageHeight, Float3x3 transform) : IComputeShader
    {
        public void Execute()
        {
            var targetPos = ThreadIds.Y * targetWidth + ThreadIds.X;
            var imagePos = transform * new Float3(ThreadIds.X, ThreadIds.Y, 1.0F);

            var ix = (int)imagePos.X;
            var iy = (int)imagePos.Y;
            if (ix == imagePos.X && iy == imagePos.Y)
            {
                if (ix > -1 && iy > -1 && ix < imageWidth && iy < imageHeight)
                {
                    target[targetPos] = image[iy * imageWidth + ix];
                }
                else
                {
                    target[targetPos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
                return;
            }
            else if (ix < -1 || iy < -1 || ix >= imageWidth || iy >= imageHeight)
            {
                target[targetPos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                return;
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
                target[targetPos] = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                target[targetPos] = t;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BlendWithTrackMatte(ReadWriteBuffer<Float4> target, ReadWriteBuffer<Float4> image, ReadWriteBuffer<float> trackMatte, int blendMode, float opacity) : IComputeShader
    {
        public void Execute()
        {
            var c = image[ThreadIds.X];
            c.W *= trackMatte[ThreadIds.X] * opacity;
            target[ThreadIds.X] = BlendMethods.Process(blendMode, target[ThreadIds.X], c);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct Blend(ReadWriteBuffer<Float4> target, ReadWriteBuffer<Float4> image, int blendMode, float opacity) : IComputeShader
    {
        public void Execute()
        {
            var c = image[ThreadIds.X];
            c.W *= opacity;
            target[ThreadIds.X] = BlendMethods.Process(blendMode, target[ThreadIds.X], c);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CreateMatteWithTrackMatte(ReadWriteBuffer<float> target, ReadWriteBuffer<Float4> image, ReadWriteBuffer<float> trackMatte, int trackMatteMode, float opacity) : IComputeShader
    {
        static readonly Float3 ConvertToGrayScale = new Float3(0.114478F, 0.586611F, 0.298912F);

        public void Execute()
        {
            var c = image[ThreadIds.X];
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
                    p = Hlsl.Dot(c.XYZ, ConvertToGrayScale) * c.W;
                    break;
                case 3:
                    p = 1.0F - (Hlsl.Dot(c.XYZ, ConvertToGrayScale) * c.W);
                    break;
            }

            target[ThreadIds.X] = p * opacity * trackMatte[ThreadIds.X];
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CreateMatte(ReadWriteBuffer<float> target, ReadWriteBuffer<Float4> image, int trackMatteMode, float opacity) : IComputeShader
    {
        static readonly Float3 ConvertToGrayScale = new Float3(0.114478F, 0.586611F, 0.298912F);

        public void Execute()
        {
            var c = image[ThreadIds.X];
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
                    p = Hlsl.Dot(c.XYZ, ConvertToGrayScale) * c.W;
                    break;
                case 3:
                    p = 1.0F - (Hlsl.Dot(c.XYZ, ConvertToGrayScale) * c.W);
                    break;
            }

            target[ThreadIds.X] = p * opacity;
        }
    }
}

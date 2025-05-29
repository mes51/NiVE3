using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Shape.Internal;

namespace NiVE3.Shape.Internal.Shader
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct EvenOddAntiAliasedRadialGradient(
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        ReadOnlyBuffer<GPULineHit> lineHits,
        ReadOnlyBuffer<Int2> lineHitIndices,
        int superSamplingCount,
        float offsetX,
        float offsetY,
        ReadOnlyBuffer<Float3> colorGradientValues,
        ReadOnlyBuffer<float> colorGradientPositions,
        ReadOnlyBuffer<float> opacityGradientValues,
        ReadOnlyBuffer<float> opacityGradientPositions,
        GPURadialGradientBrush brushState,
        int blendMode,
        int startX,
        int startY
    ) : IComputeShader
    {
        readonly float SamplingRate = 1.0F / superSamplingCount;

        public void Execute()
        {
            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            var area = CalcArea(ThreadIds.X, ThreadIds.Y);
            if (area > 0.0F)
            {
                var pos = py * imageWidth + px;
                var gradientPos = CalcGradientPosition(new Float2(px + offsetX, py + offsetY));
                image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(CalcColor(gradientPos), CalcOpacity(gradientPos) * brushState.Opacity * area));
            }
        }

        float CalcArea(int tx, int ty)
        {
            var x = tx + startX + offsetX;
            var ly = ty * superSamplingCount;
            var totalArea = 0.0F;
            for (var s = 0; s < superSamplingCount; s++)
            {
                var lineHitIndexBegin = lineHitIndices[ly + s].X;
                var lineHitIndexEnd = lineHitIndices[ly + s].Y;
                if (lineHitIndexEnd - lineHitIndexBegin < 1)
                {
                    continue;
                }

                var prev = false;
                var area = 0.0F;
                for (var li = lineHitIndexBegin; li < lineHitIndexEnd; li++)
                {
                    var lineHit = lineHits[li];
                    var value = lineHit.Value;
                    if (value > x)
                    {
                        break;
                    }

                    var d = x - value;
                    if (d < 1.0F && d > 0.0F)
                    {
                        var diff = (1.0F - (value - (int)Hlsl.Floor(value))) * (prev ? -1.0F : 1.0F);
                        area += diff;
                    }
                    else
                    {
                        if (prev && value < x)
                        {
                            area = 0.0F;
                        }
                        else
                        {
                            area = 1.0F;
                        }
                    }
                    prev = !prev;
                }

                totalArea += area;
            }

            return totalArea * SamplingRate;
        }

        float CalcGradientPosition(Float2 pos)
        {
            return Hlsl.Distance(pos, brushState.Begin) / brushState.Length;
        }

        Float3 CalcColor(float gradientPos)
        {
            if (colorGradientValues.Length < 2)
            {
                if (brushState.UseOKLabInterpolation)
                {
                    return ShaderUtil.OkLabToRgb(colorGradientValues[0]);
                }
                else
                {
                    return colorGradientValues[0];
                }
            }

            var prevColor = colorGradientValues[0];
            var prevPosition = colorGradientPositions[0];
            for (var i = colorGradientPositions.Length - 1; i > -1; i--)
            {
                if (colorGradientPositions[i] <= gradientPos)
                {
                    prevColor = colorGradientValues[i];
                    prevPosition = colorGradientPositions[i];
                    break;
                }
            }

            var nextColor = colorGradientValues[colorGradientValues.Length - 1];
            var nextPosition = colorGradientPositions[colorGradientPositions.Length - 1];
            for (var i = 0; i < colorGradientPositions.Length; i++)
            {
                if (colorGradientPositions[i] > gradientPos)
                {
                    nextColor = colorGradientValues[i];
                    nextPosition = colorGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                if (brushState.UseOKLabInterpolation)
                {
                    return ShaderUtil.OkLabToRgb(prevColor);
                }
                else
                {
                    return prevColor;
                }
            }

            var result = Hlsl.Lerp(prevColor, nextColor, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            if (brushState.UseOKLabInterpolation)
            {
                return ShaderUtil.OkLabToRgb(result);
            }
            else
            {
                return result;
            }
        }

        float CalcOpacity(float gradientPos)
        {
            if (opacityGradientValues.Length < 2)
            {
                return opacityGradientValues[0];
            }

            var prevOpacity = opacityGradientValues[0];
            var prevPosition = opacityGradientPositions[0];
            for (var i = opacityGradientPositions.Length - 1; i > -1; i--)
            {
                if (opacityGradientPositions[i] <= gradientPos)
                {
                    prevOpacity = opacityGradientValues[i];
                    prevPosition = opacityGradientPositions[i];
                    break;
                }
            }

            var nextOpacity = opacityGradientValues[opacityGradientValues.Length - 1];
            var nextPosition = opacityGradientPositions[opacityGradientPositions.Length - 1];
            for (var i = 0; i < opacityGradientPositions.Length; i++)
            {
                if (opacityGradientPositions[i] > gradientPos)
                {
                    nextOpacity = opacityGradientValues[i];
                    nextPosition = opacityGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                return prevOpacity;
            }
            else
            {
                return Hlsl.Lerp(prevOpacity, nextOpacity, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader.Shape
{
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct NonZeroAliasedLinearGradient(
        ReadWriteBuffer<Float4> image,
        int imageWidth,
        ReadOnlyBuffer<GPULineHit> lineHits,
        ReadOnlyBuffer<Int2> lineHitIndices,
        float offsetX,
        float offsetY,
        ReadOnlyBuffer<Float3> colorGradientValues,
        ReadOnlyBuffer<float> colorGradientPositions,
        ReadOnlyBuffer<float> opacityGradientValues,
        ReadOnlyBuffer<float> opacityGradientPositions,
        GPULinearGradientBrush brushState,
        int blendMode,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            if (!IsHit(ThreadIds.X, ThreadIds.Y))
            {
                return;
            }

            var px = ThreadIds.X + startX;
            var py = ThreadIds.Y + startY;

            var pos = py * imageWidth + px;
            var gradientPos = CalcGradientPosition(new Float2(px + offsetX, py + offsetY));
            image[pos] = BlendMethods.Process(blendMode, image[pos], new Float4(CalcColor(gradientPos), CalcOpacity(gradientPos)));
        }

        bool IsHit(int tx, int ty)
        {
            var x = tx + startX + offsetX;

            var lineHitIndexBegin = lineHitIndices[ty].X;
            var lineHitIndexEnd = lineHitIndices[ty].Y;
            if (lineHitIndexEnd - lineHitIndexBegin < 1)
            {
                return false;
            }

            var depth = 0;
            var dir = false;
            for (var li = lineHitIndexBegin; li < lineHitIndexEnd; li++)
            {
                var lineHit = lineHits[li];
                var value = lineHit.Value;
                if (value > x)
                {
                    break;
                }

                if (depth > 0)
                {
                    if (dir != lineHit.IsDown)
                    {
                        depth--;
                    }
                    else
                    {
                        depth++;
                    }
                }
                else
                {
                    dir = lineHit.IsDown;
                    depth++;
                }
            }

            return depth > 0;
        }

        float CalcGradientPosition(Float2 pos)
        {
            var p = Hlsl.Dot(brushState.SinCos, (pos - brushState.Begin));
            return (brushState.Reversed ? brushState.Length - p : p) / brushState.Length;
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

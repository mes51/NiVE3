using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Fxaa_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Fxaa_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Fxaa : IEffect
    {
        const string ID = "49EDC830-90A5-4F47-BF52-53373872CB39";

        const float QualityEdgeThreshold = 0.125F;

        const float EdgeThresholdMin = 0.0312F;

        const float QualitySubPixel = 0.75F;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return [];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            if (AcceleratorObject != null && useGpu)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi);
            }
            else
            {
                return ProcessCpu(image, roi);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi)
        {
            var managedImage = image.ToManaged();

            var luminanceMap = ArrayPool<float>.Shared.Rent(managedImage.DataLength);
            luminanceMap.AsSpan(0, managedImage.DataLength).Clear();
            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(0, managedImage.Height, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var luminanceMapSpan = luminanceMap.AsSpan(y * imageWidth, imageWidth);

                for (var x = 0; x < imageWidth; x++)
                {
                    luminanceMapSpan[x] = Vector4.Dot(imageDataSpan[x], Const.ConvertToGrayScaleRec709);
                }
            });

            using var sourceImage = (NManagedImage)managedImage.Copy();

            var sourceImageData = sourceImage.Data;
            var imageHeight = managedImage.Height;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                Span<float> SearchSteps = [1.0F, 1.0F, 1.0F, 1.0F, 1.0F, 1.5F, 2.0F, 2.0F, 2.0F, 2.0F, 4.0F, 8.0F];

                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var topLuminanceMapLine = luminanceMap.AsSpan(Math.Max(y - 1, 0) * imageWidth, imageWidth);
                var centerLuminanceMapLine = luminanceMap.AsSpan(y * imageWidth, imageWidth);
                var bottomLuminanceMapLine = luminanceMap.AsSpan(Math.Min(y + 1, imageHeight - 1) * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var left = Math.Max(x - 1, 0);
                    var right = Math.Min(x + 1, imageWidth - 1);

                    var lumaN = topLuminanceMapLine[x];
                    var lumaS = bottomLuminanceMapLine[x];
                    var lumaW = centerLuminanceMapLine[left];
                    var lumaM = centerLuminanceMapLine[x];
                    var lumaE = centerLuminanceMapLine[right];

                    var rangeMin = Math.Min(Math.Min(Math.Min(Math.Min(lumaM, lumaN), lumaS), lumaW), lumaE);
                    var rangeMax = Math.Max(Math.Max(Math.Max(Math.Max(lumaM, lumaN), lumaS), lumaW), lumaE);
                    var range = rangeMax - rangeMin;
                    if (range < Math.Max(EdgeThresholdMin, rangeMax * QualityEdgeThreshold))
                    {
                        continue;
                    }

                    var lumaNW = topLuminanceMapLine[left];
                    var lumaNE = topLuminanceMapLine[right];
                    var lumaSW = bottomLuminanceMapLine[left];
                    var lumaSE = bottomLuminanceMapLine[right];

                    var lumaNS = lumaN + lumaS;
                    var lumaWE = lumaW + lumaE;
                    var subPixelNSWE = lumaNS + lumaWE;
                    var edgeHorizontal1 = -2.0F * lumaM + lumaNS;
                    var edgeVertical1 = -2.0F * lumaM + lumaWE;

                    var lumaNESE = lumaNE + lumaSE;
                    var lumaNWNE = lumaNW + lumaNE;
                    var edgeHorizontal2 = -2.0F * lumaE + lumaNESE;
                    var edgeVertical2 = -2.0F * lumaN + lumaNWNE;

                    var lumaNWSW = lumaNW + lumaSW;
                    var lumaSWSE = lumaSW + lumaSE;
                    var edgeHorizontal4 = Math.Abs(edgeHorizontal1) * 2.0F + Math.Abs(edgeHorizontal2);
                    var edgeVertical4 = Math.Abs(edgeVertical1) * 2.0F + Math.Abs(edgeVertical2);
                    var edgeHorizontal3 = -2.0F * lumaW + lumaNWSW;
                    var edgeVertical3 = -2.0F * lumaS + lumaSWSE;
                    var edgeHorizontal = Math.Abs(edgeHorizontal3) + edgeHorizontal4;
                    var edgeVertical = Math.Abs(edgeVertical3) + edgeVertical4;

                    var subPixelNWSWNESE = lumaNWSW + lumaNESE;
                    var lengthSign = 1.0F;
                    var isHorizontal = edgeHorizontal >= edgeVertical;
                    var subPixelA = subPixelNSWE * 2.0F + subPixelNWSWNESE;

                    if (!isHorizontal)
                    {
                        lumaN = lumaW;
                        lumaS = lumaE;
                    }
                    var subPixelB = (subPixelA * (1.0F / 12.0F)) - lumaM;

                    var gradientN = lumaN - lumaM;
                    var gradientS = lumaS - lumaM;
                    var lumaNN = lumaN + lumaM;
                    var lumaSS = lumaS + lumaM;
                    var pairN = Math.Abs(gradientN) >= Math.Abs(gradientS);
                    var gradient = Math.Max(Math.Abs(gradientN), Math.Abs(gradientS));
                    if (pairN)
                    {
                        lengthSign = -1.0F;
                    }
                    var subPixelC = Math.Clamp(Math.Abs(subPixelB) / range, 0.0F, 1.0F);

                    var offsetNP = new Vector2(!isHorizontal ? 0.0F : 1.0F, isHorizontal ? 0.0F : 1.0F);
                    var posB = new Vector2(x, y) + (isHorizontal ? new Vector2(0.0F, lengthSign) : new Vector2(lengthSign, 0.0F)) * 0.5F;

                    var subPixelD = -2.0F * subPixelC + 3.0F;
                    var subPixelE = subPixelC * subPixelC;
                    var subPixelF = subPixelD * subPixelE;
                    var subPixelG = subPixelF * subPixelF;
                    var subPixelH = subPixelG * QualitySubPixel;

                    if (!pairN)
                    {
                        lumaNN = lumaSS;
                    }
                    var gradientScaled = gradient * 0.25F;
                    var lumaMM = lumaM - lumaNN * 0.5F;
                    var lumaMLTZero = lumaMM < 0.0F;

                    var doneNP = true;
                    var posN = posB - offsetNP * SearchSteps[0];
                    var posP = posB + offsetNP * SearchSteps[0];
                    var lumaEndN = 0.0F;
                    var lumaEndP = 0.0F;
                    var doneN = false;
                    var doneP = false;
                    for (var i = 1; i < SearchSteps.Length && doneNP; i++)
                    {
                        if (!doneN)
                        {
                            lumaEndN = Vector4.Dot(ImageInterpolation.BilinearEdgeRepeat(sourceImageData, imageWidth, imageHeight, posN.X, posN.Y), Const.ConvertToGrayScaleRec709) - lumaNN * 0.5F;
                            doneN = Math.Abs(lumaEndN) >= gradientScaled;
                        }
                        if (!doneP)
                        {
                            lumaEndP = Vector4.Dot(ImageInterpolation.BilinearEdgeRepeat(sourceImageData, imageWidth, imageHeight, posP.X, posP.Y), Const.ConvertToGrayScaleRec709) - lumaNN * 0.5F;
                            doneP = Math.Abs(lumaEndP) >= gradientScaled;
                        }
                        doneNP = !doneN || !doneP;
                        if (!doneN)
                        {
                            posN -= offsetNP * SearchSteps[i];
                        }
                        if (!doneP)
                        {
                            posP += offsetNP * SearchSteps[i];
                        }
                    }
                    var distN = isHorizontal ? x - posN.X : y - posN.Y;
                    var distP = isHorizontal ? posP.X - x : posP.Y - y;

                    var goodSpanN = (lumaEndN < 0.0F) != lumaMLTZero;
                    var goodSpanP = (lumaEndP < 0.0F) != lumaMLTZero;
                    var spanLength = distP + distN;
                    var directionN = distN < distP;
                    var distance = Math.Min(distN, distP);
                    var goodSpan = directionN ? goodSpanN : goodSpanP;
                    var pixelOffset = (distance / -spanLength) + 0.5F;

                    var pixelOffsetGood = goodSpan ? pixelOffset : 0.0F;
                    var pixelOffsetSubPixel = Math.Max(pixelOffsetGood, subPixelH);

                    var posMX = x + (isHorizontal ? 0.0F : lengthSign * pixelOffsetSubPixel);
                    var posMY = y + (isHorizontal ? lengthSign * pixelOffsetSubPixel : 0.0F);

                    imageDataSpan[x] = ImageInterpolation.BilinearEdgeRepeat(sourceImageData, imageWidth, imageHeight, posMX, posMY);
                }
            });

            ArrayPool<float>.Shared.Return(luminanceMap);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            using var luminanceMap = device.AllocateReadWriteBuffer<float>(gpuImage.DataLength);

            using (var context = device.CreateComputeContext())
            {
                context.For(gpuImage.Width, gpuImage.Height, new FxaaCalcLuminanceProcess(gpuImage.Data, gpuImage.Width, luminanceMap));
                context.Barrier(luminanceMap);
                context.For(roi.Width, roi.Height, new FxaaProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, luminanceMap, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FxaaCalcLuminanceProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> luminanceMap) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;

            luminanceMap[pos] = Hlsl.Dot(Const.ConvertToGrayScaleRec709Float3, image[pos].XYZ);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FxaaProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, ReadWriteBuffer<float> luminanceMap, int startX, int startY) : IComputeShader
    {
        const float QualityEdgeThreshold = 0.125F;

        const float EdgeThresholdMin = 0.0312F;

        const float QualitySubPixel = 0.75F;

        static readonly Float4x3 SearchSteps = new Float4x3(1.0F, 1.0F, 1.0F, 1.0F, 1.0F, 1.5F, 2.0F, 2.0F, 2.0F, 2.0F, 4.0F, 8.0F);

        const int SearchStepsCount = 12;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var top = Hlsl.Max(y - 1, 0) * width;
            var center = y * width;
            var bottom = Hlsl.Min(y + 1, height - 1) * width;
            var left = Hlsl.Max(x - 1, 0);
            var right = Hlsl.Min(x + 1, width - 1);

            var lumaN = luminanceMap[top + x];
            var lumaS = luminanceMap[bottom + x];
            var lumaW = luminanceMap[center + left];
            var lumaM = luminanceMap[center + x];
            var lumaE = luminanceMap[center + right];

            var rangeMin = Hlsl.Min(Hlsl.Min(Hlsl.Min(Hlsl.Min(lumaM, lumaN), lumaS), lumaW), lumaE);
            var rangeMax = Hlsl.Max(Hlsl.Max(Hlsl.Max(Hlsl.Max(lumaM, lumaN), lumaS), lumaW), lumaE);
            var range = rangeMax - rangeMin;
            if (range < Hlsl.Max(EdgeThresholdMin, rangeMax * QualityEdgeThreshold))
            {
                return;
            }

            var lumaNW = luminanceMap[top + left];
            var lumaNE = luminanceMap[top + right];
            var lumaSW = luminanceMap[bottom + left];
            var lumaSE = luminanceMap[bottom + right];

            var lumaNS = lumaN + lumaS;
            var lumaWE = lumaW + lumaE;
            var subPixelNSWE = lumaNS + lumaWE;
            var edgeHorizontal1 = -2.0F * lumaM + lumaNS;
            var edgeVertical1 = -2.0F * lumaM + lumaWE;

            var lumaNESE = lumaNE + lumaSE;
            var lumaNWNE = lumaNW + lumaNE;
            var edgeHorizontal2 = -2.0F * lumaE + lumaNESE;
            var edgeVertical2 = -2.0F * lumaN + lumaNWNE;

            var lumaNWSW = lumaNW + lumaSW;
            var lumaSWSE = lumaSW + lumaSE;
            var edgeHorizontal4 = Hlsl.Abs(edgeHorizontal1) * 2.0F + Hlsl.Abs(edgeHorizontal2);
            var edgeVertical4 = Hlsl.Abs(edgeVertical1) * 2.0F + Hlsl.Abs(edgeVertical2);
            var edgeHorizontal3 = -2.0F * lumaW + lumaNWSW;
            var edgeVertical3 = -2.0F * lumaS + lumaSWSE;
            var edgeHorizontal = Hlsl.Abs(edgeHorizontal3) + edgeHorizontal4;
            var edgeVertical = Hlsl.Abs(edgeVertical3) + edgeVertical4;

            var subPixelNWSWNESE = lumaNWSW + lumaNESE;
            var lengthSign = 1.0F;
            var isHorizontal = edgeHorizontal >= edgeVertical;
            var subPixelA = subPixelNSWE * 2.0F + subPixelNWSWNESE;

            if (!isHorizontal)
            {
                lumaN = lumaW;
                lumaS = lumaE;
            }
            var subPixelB = (subPixelA * (1.0F / 12.0F)) - lumaM;

            var gradientN = lumaN - lumaM;
            var gradientS = lumaS - lumaM;
            var lumaNN = lumaN + lumaM;
            var lumaSS = lumaS + lumaM;
            var pairN = Hlsl.Abs(gradientN) >= Hlsl.Abs(gradientS);
            var gradient = Hlsl.Max(Hlsl.Abs(gradientN), Hlsl.Abs(gradientS));
            if (pairN)
            {
                lengthSign = -1.0F;
            }
            var subPixelC = Hlsl.Clamp(Hlsl.Abs(subPixelB) / range, 0.0F, 1.0F);

            var offsetNP = new Float2(!isHorizontal ? 0.0F : 1.0F, isHorizontal ? 0.0F : 1.0F);
            var posB = new Float2(x, y) + (isHorizontal ? new Float2(0.0F, lengthSign) : new Float2(lengthSign, 0.0F)) * 0.5F;

            var subPixelD = -2.0F * subPixelC + 3.0F;
            var subPixelE = subPixelC * subPixelC;
            var subPixelF = subPixelD * subPixelE;
            var subPixelG = subPixelF * subPixelF;
            var subPixelH = subPixelG * QualitySubPixel;

            if (!pairN)
            {
                lumaNN = lumaSS;
            }
            var gradientScaled = gradient * 0.25F;
            var lumaMM = lumaM - lumaNN * 0.5F;
            var lumaMLTZero = lumaMM < 0.0F;

            var doneNP = true;
            var posN = posB - offsetNP * SearchSteps[0][0];
            var posP = posB + offsetNP * SearchSteps[0][0];
            var lumaEndN = 0.0F;
            var lumaEndP = 0.0F;
            var doneN = false;
            var doneP = false;
            for (var i = 1; i < SearchStepsCount && doneNP; i++)
            {
                if (!doneN)
                {
                    lumaEndN = Hlsl.Dot(SourceImageBilinear(posN.X, posN.Y).XYZ, Const.ConvertToGrayScaleRec709Float3) - lumaNN * 0.5F;
                    doneN = Hlsl.Abs(lumaEndN) >= gradientScaled;
                }
                if (!doneP)
                {
                    lumaEndP = Hlsl.Dot(SourceImageBilinear(posP.X, posP.Y).XYZ, Const.ConvertToGrayScaleRec709Float3) - lumaNN * 0.5F;
                    doneP = Hlsl.Abs(lumaEndP) >= gradientScaled;
                }
                doneNP = !doneN || !doneP;
                var row = i / 3;
                var col = 1 % 3;
                if (!doneN)
                {
                    posN -= offsetNP * SearchSteps[row][col];
                }
                if (!doneP)
                {
                    posP += offsetNP * SearchSteps[row][col];
                }
            }
            var distN = isHorizontal ? x - posN.X : y - posN.Y;
            var distP = isHorizontal ? posP.X - x : posP.Y - y;

            var goodSpanN = (lumaEndN < 0.0F) != lumaMLTZero;
            var goodSpanP = (lumaEndP < 0.0F) != lumaMLTZero;
            var spanLength = distP + distN;
            var directionN = distN < distP;
            var distance = Hlsl.Min(distN, distP);
            var goodSpan = directionN ? goodSpanN : goodSpanP;
            var pixelOffset = (distance / -spanLength) + 0.5F;

            var pixelOffsetGood = goodSpan ? pixelOffset : 0.0F;
            var pixelOffsetSubPixel = Hlsl.Max(pixelOffsetGood, subPixelH);

            var posMX = x + (isHorizontal ? 0.0F : lengthSign * pixelOffsetSubPixel);
            var posMY = y + (isHorizontal ? lengthSign * pixelOffsetSubPixel : 0.0F);

            var pos = y * width + x;
            image[pos] = SourceImageBilinear(posMX, posMY);
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c2 = sourceImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];
            var c3 = sourceImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c4 = sourceImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
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

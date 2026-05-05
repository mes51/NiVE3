using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_BumpMappinglMap_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_BumpMapping_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class BumpMapping : IEffect
    {
        const float ShininessStrength = 120.0F;

        const string ID = "D63EEBB3-6244-4C7E-BBBC-D53427729AB3";

        const string PropertyNormalMapLayerId = nameof(PropertyNormalMapLayerId);

        const string PropertyNormalMapLayerPositionId = nameof(PropertyNormalMapLayerPositionId);

        const string PropertyNormalMapLayerUseReferenceTimeId = nameof(PropertyNormalMapLayerUseReferenceTimeId);

        const string PropertyNormalMapLayerSpecificReferenceTimeId = nameof(PropertyNormalMapLayerSpecificReferenceTimeId);

        const string PropertyParallelLightGroupId = nameof(PropertyParallelLightGroupId);

        const string PropertyParallelLightColorId = nameof(PropertyParallelLightColorId);

        const string PropertyParallelLightIntensityId = nameof(PropertyParallelLightIntensityId);

        const string PropertyParallelLightPositionId = nameof(PropertyParallelLightPositionId);

        const string PropertyAmbientLightGroupId = nameof(PropertyAmbientLightGroupId);

        const string PropertyAmbientLightColorId = nameof(PropertyAmbientLightColorId);

        const string PropertyAmbientLightIntensityId = nameof(PropertyAmbientLightIntensityId);

        const string PropertyMaterialGroupId = nameof(PropertyMaterialGroupId);

        const string PropertyMaterialDiffuseId = nameof(PropertyMaterialDiffuseId);

        const string PropertyMaterialSpecularIntensityId = nameof(PropertyMaterialSpecularIntensityId);

        const string PropertyMaterialSpecularShininessId = nameof(PropertyMaterialSpecularShininessId);

        const string PropertyMaterialMetalId = nameof(PropertyMaterialMetalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var colorDialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var okButton = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var cancelButton = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            return
            [
                new UseLayerImageProperty(PropertyNormalMapLayerId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_NormalMapLayer, 90.0),
                new EnumProperty(PropertyNormalMapLayerPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_NormalMapLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyNormalMapLayerUseReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_NormalMapLayerUseSpecificReferenceTime, false),
                new DoubleProperty(PropertyNormalMapLayerSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_NormalMapLayerSpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                new PropertyGroup(PropertyParallelLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_ParallelLight,
                [
                    new ColorProperty(PropertyParallelLightColorId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_ParallelLight_Color, colorDialogTitle, okButton, cancelButton, Vector4.One),
                    new DoubleProperty(PropertyParallelLightIntensityId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_ParallelLight_Intensity, 100.0, 0.0, double.MaxValue, digit: 2),
                    new Vector3dProperty(PropertyParallelLightPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_ParallelLight_Position, new Vector3d(sourceSize.Width * 0.25, sourceSize.Height * 0.25, -600.0), digit: 2, is3D: true, useInteraction: true)
                ]),
                new PropertyGroup(PropertyAmbientLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_AmbientLight,
                [
                    new ColorProperty(PropertyAmbientLightColorId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_AmbientLight_Color, colorDialogTitle, okButton, cancelButton, Vector4.One),
                    new DoubleProperty(PropertyAmbientLightIntensityId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_AmbientLight_Intensity, 50.0, 0.0, 100.0, digit: 2)
                ]),
                new PropertyGroup(PropertyMaterialGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_Material,
                [
                    new DoubleProperty(PropertyMaterialDiffuseId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_Material_Diffuse, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyMaterialSpecularIntensityId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_Material_SpecularIntensity, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyMaterialSpecularShininessId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_Material_SpecularShininess, 5.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyMaterialMetalId, LanguageResourceDictionary.ResourceKeys.Stylize_BumpMapping_Material_Metal, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                ])
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var normalMapLayerId = properties.GetValue(PropertyNormalMapLayerId, layerTime, UseLayerImageTarget.Empty);
            if (normalMapLayerId == UseLayerImageTarget.Empty)
            {
                return image;
            }

            var useReferenceTime = properties.GetValue(PropertyNormalMapLayerUseReferenceTimeId, layerTime, false);
            var specificTime = (Time)properties.GetValue(PropertyNormalMapLayerSpecificReferenceTimeId, layerTime, 0.0);
            using var normalMap = useReferenceTime ? normalMapLayerId.GetImageReferenceTime(composition, specificTime, downSamplingRateX, useGpu) : normalMapLayerId.GetImage(composition, layerTime + layer.SourceStartPoint, downSamplingRateX, useGpu);
            if (normalMap == null)
            {
                return image;
            }

            var normalMapPosition = properties.GetValue(PropertyNormalMapLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var parallelLightGroup = properties.First(p => p.Id == PropertyParallelLightGroupId).GetChildren() ?? [];
            var parallelLightColor = parallelLightGroup.GetValue(PropertyParallelLightColorId, layerTime, Vector4.Zero);
            var parallelLightIntensity = (float)(parallelLightGroup.GetValue(PropertyParallelLightIntensityId, layerTime, 0.0) * 0.01);
            var parallelLightPosition = (Vector3)parallelLightGroup.GetValue(PropertyParallelLightPositionId, layerTime, Vector3d.Zero);
            var ambientLightGroup = properties.First(p => p.Id == PropertyAmbientLightGroupId).GetChildren() ?? [];
            var ambientLightColor = ambientLightGroup.GetValue(PropertyAmbientLightColorId, layerTime, Vector4.Zero);
            var ambientLightIntensity = (float)(ambientLightGroup.GetValue(PropertyAmbientLightIntensityId, layerTime, 0.0) * 0.01);
            var materialGroup = properties.First(p => p.Id == PropertyMaterialGroupId).GetChildren() ?? [];
            var diffuse = (float)(materialGroup.GetValue(PropertyMaterialDiffuseId, layerTime, 0.0) * 0.01);
            var specularIntensity = (float)(materialGroup.GetValue(PropertyMaterialSpecularIntensityId, layerTime, 0.0) * 0.01);
            var specularShininess = (float)(materialGroup.GetValue(PropertyMaterialSpecularShininessId, layerTime, 0.0) * 0.01);
            var metal = (float)(materialGroup.GetValue(PropertyMaterialMetalId, layerTime, 0.0) * 0.01);

            var parallelLightDir = Vector3.Normalize(new Vector3(roi.OriginalImageSize.Width, roi.OriginalImageSize.Height, 0.0F) * 0.5F - parallelLightPosition);
            if (Vector3.AnyWhereAllBitsSet(Vector3.IsNaN(parallelLightDir)))
            {
                parallelLightDir = Vector3.UnitZ;
            }

            parallelLightColor *= parallelLightIntensity;
            ambientLightColor *= ambientLightIntensity;
            parallelLightColor.W = 1.0F;
            ambientLightColor.W = 1.0F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, normalMap, normalMapPosition, parallelLightColor, parallelLightDir, ambientLightColor, diffuse, specularIntensity, specularShininess, metal);
            }
            else
            {
                return ProcessCpu(image, roi, normalMap, normalMapPosition, parallelLightColor, parallelLightDir, ambientLightColor, diffuse, specularIntensity, specularShininess, metal);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, NImage normalMap, SourceLayerPositionType normalMapPosition, Vector4 parallelLightColor, Vector3 parallelLightDir, Vector4 ambientLightColor, float diffuse, float specularIntensity, float specularShininess, float metal)
        {
            const float Scale = (float)(0.5 / Const.DefaultCameraFov);

            var managedImage = image.ToManaged();

            var managedNormalMap = normalMap.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var normalMapWidth = managedNormalMap.Width;
            var normalMapHeight = managedNormalMap.Height;
            var normalMapData = managedNormalMap.Data;

            var size = Math.Max(managedImage.Width, managedImage.Height);
            var uvRate = new Vector3(1.0F / size, 1.0F / size, 1.0F);
            var viewOffset = new Vector3(size - managedImage.Width, size - managedImage.Height, 0.0F) / size * 0.5F - new Vector3(0.5F, 0.5F, 0.0F) + new Vector3(0.0F, 0.0F, Scale);

            var (sourceStartX, sourceStartY) = normalMapPosition switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedNormalMap.Width - managedImage.Width) * 0.5F, (managedNormalMap.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = normalMapPosition switch
            {
                SourceLayerPositionType.Stretch => ((managedNormalMap.Width - 1) / (float)(managedImage.Width - 1), (managedNormalMap.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceX = sourceStartX + sourceDiffX * roi.Left;
                var sourceY = sourceStartY + sourceDiffY * y;
                for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                {
                    var color = imageDataSpan[x];

                    var normal = (normalMapPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(normalMapData, normalMapWidth, normalMapHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(normalMapData, normalMapWidth, normalMapHeight, sourceX, sourceY)).AsVector3();
                    normal = Vector3.Normalize(normal - new Vector3(0.0F, 0.5F, 0.5F));
                    if (Vector3.AnyWhereAllBitsSet(Vector3.IsNaN(normal)))
                    {
                        normal = Vector3.UnitZ;
                    }
                    else
                    {
                        normal = Avx.Permute(normal.AsVector128(), 0b11000110).AsVector3();
                    }
                    var diffuseFactor = Math.Max(Vector3.Dot(parallelLightDir, normal), 0.0F);
                    var newColor = parallelLightColor * diffuseFactor * color * diffuse;

                    var view = -Vector3.Normalize(new Vector3(x, y, 0.0F) * uvRate + viewOffset);
                    var halfLE = Vector3.Normalize(view - parallelLightDir);
                    var specularFactor = Math.Max(Vector3.Dot(-normal, halfLE), 0.0F);
                    newColor += Vector4.Lerp(parallelLightColor, color * parallelLightColor, metal) * MathF.Pow(specularFactor, ShininessStrength * specularShininess) * specularIntensity;

                    newColor += ambientLightColor * color;
                    newColor.W = color.W;
                    imageDataSpan[x] = newColor;
                }
            });

            if (managedNormalMap != normalMap)
            {
                managedNormalMap.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage normalMap, SourceLayerPositionType normalMapPosition, Vector4 parallelLightColor, Vector3 parallelLightDir, Vector4 ambientLightColor, float diffuse, float specularIntensity, float specularShininess, float metal)
        {
            var gpuImage = image.ToGpu(device);

            var gpuNormalMap = normalMap.ToGpu(device);

            device.For(
                roi.Width,
                roi.Height,
                new BumpMappingProcess(
                    gpuImage.Data,
                    gpuImage.Width,
                    gpuImage.Height,
                    Math.Max(gpuImage.Width, gpuImage.Height),
                    gpuNormalMap.Data,
                    gpuNormalMap.Width,
                    gpuNormalMap.Height,
                    (int)normalMapPosition,
                    parallelLightColor,
                    parallelLightDir,
                    ambientLightColor,
                    diffuse,
                    specularIntensity,
                    specularShininess,
                    metal,
                    roi.Left,
                    roi.Top
                )
            );

            if (normalMap != gpuNormalMap)
            {
                gpuNormalMap.Dispose();
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BumpMappingProcess(
        ReadWriteBuffer<Float4> image,
        int width,
        int height,
        int renderSize,
        ReadWriteBuffer<Float4> normalMap,
        int normalMapWidth,
        int normalMapHeight,
        int normalMapPosition,
        Float4 parallelLightColor,
        Float3 parallelLightDir,
        Float4 ambientLightColor,
        float diffuse,
        float specularIntensity,
        float specularShininess,
        float metal,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float Scale = (float)(0.5 / Const.DefaultCameraFov);

        const float ShininessStrength = 120.0F;

        readonly float SourceStartX = normalMapPosition == 1 ? 0.0F : (normalMapWidth - width) * 0.5F;

        readonly float SourceStartY = normalMapPosition == 1 ? 0.0F : (normalMapHeight - height) * 0.5F;

        readonly float SourceDiffX = normalMapPosition == 1 ? (normalMapWidth - 1.0F) / (width - 1.0F) : 1.0F;

        readonly float SourceDiffY = normalMapPosition == 1 ? (normalMapHeight - 1.0F) / (height - 1.0F) : 1.0F;

        readonly Float3 UVRate = new Float3(1.0F / renderSize, 1.0F / renderSize, 1.0F);

        readonly Float3 ViewOffset = new Float3((renderSize - width) / (float)renderSize * 0.5F - 0.5F, (renderSize - height) / (float)renderSize * 0.5F - 0.5F, Scale);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var color = image[pos];

            var normalX = x * SourceDiffX + SourceStartX;
            var normalY = y * SourceDiffY + SourceStartY;
            var normal = (normalMapPosition == 2 ? NormalMapBilinearLoop(normalX, normalY) : NormalMapBilinear(normalX, normalY)).ZYX;
            normal = Hlsl.Normalize(normal - new Float3(0.5F, 0.5F, 0.0F));
            if (Hlsl.Any(Hlsl.IsNaN(normal)))
            {
                normal = Float3.UnitZ;
            }
            var diffuseFactor = Hlsl.Max(Hlsl.Dot(parallelLightDir, normal), 0.0F);
            var newColor = parallelLightColor * diffuseFactor * color * diffuse;

            var view = -Hlsl.Normalize(new Float3(x, y, 0.0F) * UVRate + ViewOffset);
            var halfLE = Hlsl.Normalize(view - parallelLightDir);
            var specularFactor = Hlsl.Max(Hlsl.Dot(-normal, halfLE), 0.0F);
            newColor += Hlsl.Lerp(parallelLightColor, color * parallelLightColor, metal) * Hlsl.Pow(specularFactor, ShininessStrength * specularShininess) * specularIntensity;

            newColor += ambientLightColor * color;
            newColor.W = color.W;
            image[pos] = newColor;
        }

        Float4 NormalMapBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < normalMapWidth && iy < normalMapHeight)
                {
                    return normalMap[iy * normalMapWidth + ix];
                }
                else
                {
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= normalMapWidth || iy >= normalMapHeight)
            {
                return 0.5F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = normalMapWidth - 1;
            var mh = normalMapHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * normalMapWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = normalMap[pos];
                        c2 = normalMap[pos + 1];
                        if (iy < mh)
                        {
                            pos += normalMapWidth;
                            c3 = normalMap[pos];
                            c4 = normalMap[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += normalMapWidth;
                        c1 = normalMap[pos];
                        c2 = normalMap[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = normalMap[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = normalMap[pos + normalMapWidth];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = normalMap[pos + normalMapWidth];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = normalMap[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = normalMap[pos + normalMapWidth];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = normalMap[pos + normalMapWidth];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 NormalMapBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return normalMap[CoordWrapGpu.Repeat(iy, normalMapHeight) * normalMapWidth + CoordWrapGpu.Repeat(ix, normalMapWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = normalMap[CoordWrapGpu.Repeat(iy, normalMapHeight) * normalMapWidth + CoordWrapGpu.Repeat(ix, normalMapWidth)];
            var c2 = normalMap[CoordWrapGpu.Repeat(iy, normalMapHeight) * normalMapWidth + CoordWrapGpu.Repeat(ix + 1, normalMapWidth)];
            var c3 = normalMap[CoordWrapGpu.Repeat(iy + 1, normalMapHeight) * normalMapWidth + CoordWrapGpu.Repeat(ix, normalMapWidth)];
            var c4 = normalMap[CoordWrapGpu.Repeat(iy + 1, normalMapHeight) * normalMapWidth + CoordWrapGpu.Repeat(ix + 1, normalMapWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
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

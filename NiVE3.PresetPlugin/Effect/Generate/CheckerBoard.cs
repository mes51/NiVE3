using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Stylize;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [EffectMetadata(LanguageResourceDictionary.Generate_CheckerBoard_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_CheckerBoard_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public class CheckerBoard : IEffect
    {
        const string ID = "A6E2DFBA-919A-4963-AF7F-24B69A0CECF9";

        const string PropertyAnchorId = nameof(PropertyAnchorId);

        const string PropertyColor1Id = nameof(PropertyColor1Id);

        const string PropertyOpacity1Id = nameof(PropertyOpacity1Id);

        const string PropertyColor2Id = nameof(PropertyColor2Id);

        const string PropertyOpacity2Id = nameof(PropertyOpacity2Id);

        const string PropertyGridSizeGroupId = nameof(PropertyGridSizeGroupId);

        const string PropertyGridSizeTypeId = nameof(PropertyGridSizeTypeId);

        const string PropertyGridSizeCornerId = nameof(PropertyGridSizeCornerId);

        const string PropertyGridSizeWidthId = nameof(PropertyGridSizeWidthId);

        const string PropertyGridSizeHeightId = nameof(PropertyGridSizeHeightId);

        const string PropertyBlurWidthId = nameof(PropertyBlurWidthId);

        const string PropertyBlurHeightId = nameof(PropertyBlurHeightId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        const int RoundDigitRate = 1000;

        const float RoundDigitDenominator = 0.001F;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var anchor = new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5;
            var dialogOK = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            var colorDialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            return
            [
                new Vector3dProperty(PropertyAnchorId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_Anchor, anchor, digit: 2),
                new ColorProperty(PropertyColor1Id, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_Color1, colorDialogTitle, dialogOK, dialogCancel, Vector4.One),
                new DoubleProperty(PropertyOpacity1Id, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_Opacity1, 100.0, 0.0, 100.0, digit: 2),
                new ColorProperty(PropertyColor2Id, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_Color2, colorDialogTitle, dialogOK, dialogCancel, Vector4.UnitW),
                new DoubleProperty(PropertyOpacity2Id, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_Opacity2, 100.0, 0.0, 100.0, digit: 2),
                new PropertyGroup(PropertyGridSizeGroupId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_GridSize,
                [
                    new EnumProperty(PropertyGridSizeTypeId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_GridSize_Type, typeof(CheckerBoardGridSizeType), typeof(LanguageResourceDictionary), CheckerBoardGridSizeType.Width, selectBoxWidth: 90.0),
                    new DoubleProperty(PropertyGridSizeWidthId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_GridSize_Width, 20.0, 0.5, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyGridSizeHeightId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_GridSize_Height, 20.0, 0.5, double.MaxValue, digit: 2),
                    new Vector3dProperty(PropertyGridSizeCornerId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_GridSize_Corner, anchor + new Vector3d(20.0, 20.0, 0.0), digit: 2)
                ]),
                new DoubleProperty(PropertyBlurWidthId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_BlurWidth, 0.0, 0.0, 1000.0, digit: 2),
                new DoubleProperty(PropertyBlurHeightId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_BlurHeight, 0.0, 0.0, 1000.0, digit: 2),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Generate_CheckerBoard_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var anchor = properties.GetValue(PropertyAnchorId, layerTime, Vector3d.Zero);
            var gridSize = properties.First(p => p.Id == PropertyGridSizeGroupId)?.GetChildren() ?? [];
            var gridSizeType = gridSize.GetValue(PropertyGridSizeTypeId, layerTime, CheckerBoardGridSizeType.Width);
            var gridWidthValue = (float)gridSize.GetValue(PropertyGridSizeWidthId, layerTime, 0.0);
            var gridHeightValue = (float)gridSize.GetValue(PropertyGridSizeHeightId, layerTime, 0.0);
            var gridCorner = (Vector2)gridSize.GetValue(PropertyGridSizeCornerId, layerTime, Vector3d.Zero);

            var gridWidth = 0.0F;
            var gridHeight = 0.0F;
            switch (gridSizeType)
            {
                case CheckerBoardGridSizeType.Width:
                    gridWidth = gridWidthValue;
                    gridHeight = gridWidthValue;
                    break;
                case CheckerBoardGridSizeType.WidthAndHeight:
                    gridWidth = gridWidthValue;
                    gridHeight = gridHeightValue;
                    break;
                case CheckerBoardGridSizeType.Corner:
                    {
                        var size = Vector2.Abs(gridCorner - (Vector2)anchor);
                        gridWidth = size.X;
                        gridHeight = size.Y;
                    }
                    break;
            }

            gridWidth = Math.Max((float)(gridWidth / downSamplingRateX), 0.5F);
            gridHeight = Math.Max((float)(gridHeight / downSamplingRateY), 0.5F);
            anchor /= new Vector3d(downSamplingRateX, downSamplingRateY, 1.0);

            var color1 = properties.GetValue(PropertyColor1Id, layerTime, Vector4.Zero);
            var opacity1 = (float)(properties.GetValue(PropertyOpacity1Id, layerTime, 0.0) * 0.01);
            var color2 = properties.GetValue(PropertyColor2Id, layerTime, Vector4.Zero);
            var opacity2 = (float)(properties.GetValue(PropertyOpacity2Id, layerTime, 0.0) * 0.01);
            var blurWidth = (float)(properties.GetValue(PropertyBlurWidthId, layerTime, 0.0) / downSamplingRateX);
            var blurHeight = (float)(properties.GetValue(PropertyBlurHeightId, layerTime, 0.0) / downSamplingRateY);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            color1.W = opacity1;
            color2.W = opacity2;

            var gridStartPos = (Vector2)(anchor + new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0));
            gridStartPos.X = gridStartPos.X % (gridWidth * 2.0F);
            gridStartPos.Y = gridStartPos.Y % (gridHeight * 2.0F);
            if (gridStartPos.X > 0.0F)
            {
                gridStartPos.X -= gridWidth * 2.0F;
            }
            if (gridStartPos.Y > 0.0F)
            {
                gridStartPos.Y -= gridHeight * 2.0F;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, gridStartPos, color1, color2, gridWidth, gridHeight, blurWidth, blurHeight, blendMode);
            }
            else
            {
                return ProcessCpu(image, roi, gridStartPos, color1, color2, gridWidth, gridHeight, blurWidth, blurHeight, blendMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 gridStartPos, Vector4 color1, Vector4 color2, float gridWidth, float gridHeight, float blurWidth, float blurHeight, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            using var checkerBoardImage = new NManagedImage(managedImage.Width, managedImage.Height);

            if (gridWidth <= 0.5F ||  gridHeight <= 0.5F)
            {
                var flattenedColor = (color1 + color2) * 0.5F;
                checkerBoardImage.Data.AsSpan().Fill(flattenedColor);
            }
            else
            {
                var imageWidth = image.Width;
                var imageData = managedImage.Data;
                var intGridWidth = (int)(gridWidth * RoundDigitRate);
                var intGridHeight = (int)(gridHeight * RoundDigitRate);
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var posY = (int)((y - gridStartPos.Y) * RoundDigitRate);
                    var startMode = (posY / intGridHeight) % 2;
                    var yArea = 1.0F - Math.Clamp((posY % intGridHeight) - intGridHeight + RoundDigitRate, 0, RoundDigitRate) * RoundDigitDenominator;

                    if (yArea < 1.0F)
                    {
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var posX = (int)((x - gridStartPos.X) * RoundDigitRate);
                            var mode = (posX / intGridWidth + startMode) % 2;
                            var xArea = 1.0F - Math.Clamp((posX % intGridWidth) - intGridWidth + RoundDigitRate, 0, RoundDigitRate) * RoundDigitDenominator;
                            var area = yArea * xArea + (1.0F - yArea) * (1.0F - xArea);
                            var color = mode == 0 ? Vector4.Lerp(color2, color1, area) : Vector4.Lerp(color1, color2, area);

                            imageDataSpan[x] = color;
                        }
                    }
                    else
                    {
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var posX = (int)((x - gridStartPos.X) * RoundDigitRate);
                            var mode = (posX / intGridWidth + startMode) % 2;
                            var area = 1.0F - Math.Clamp((posX % intGridWidth) - intGridWidth + RoundDigitRate, 0, RoundDigitRate) * RoundDigitDenominator;
                            var color = mode == 0 ? Vector4.Lerp(color2, color1, area) : Vector4.Lerp(color1, color2, area);

                            imageDataSpan[x] = color;
                        }
                    }
                });

                if (blurWidth > 0.0F &&  blurHeight > 0.0F)
                {
                    BoxBlurProcessor.ProcessCpu(checkerBoardImage, roi, blurWidth, blurHeight, 1, EdgeRepeatMode.Mirror);
                }
            }

            ImageBlendProcessor.SameSizeCpu(managedImage, checkerBoardImage, roi, blendMode);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 gridStartPos, Vector4 color1, Vector4 color2, float gridWidth, float gridHeight, float blurWidth, float blurHeight, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            using var checkerBoardImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device, (color1 + color2) * 0.5F);
            if (gridWidth > 0.5F && gridHeight > 0.5F)
            {
                device.For(roi.Width, roi.Height, new CheckerBoardProcess(checkerBoardImage.Data, checkerBoardImage.Width, color1, color2, gridStartPos, gridWidth, gridHeight, roi.Left, roi.Top));

                if (blurWidth > 0.0F || blurHeight > 0.0F)
                {
                    BoxBlurProcessor.ProcessGpu(device, checkerBoardImage, roi, blurWidth, blurHeight, 1, EdgeRepeatMode.Mirror);
                }
            }

            ImageBlendProcessor.SameSizeGpu(device, gpuImage, checkerBoardImage, roi, blendMode);

            return gpuImage;
        }
    }

    enum CheckerBoardGridSizeType
    {
        Width,
        WidthAndHeight,
        Corner,
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CheckerBoardProcess(ReadWriteBuffer<Float4> image, int width, Float4 color1, Float4 color2, Float2 gridStartPos, float gridWidth, float gridHeight, int startX, int startY) : IComputeShader
    {
        const int RoundDigitRate = 1000;

        const float RoundDigitDenominator = 0.001F;

        readonly int IntGridWidth = (int)(gridWidth * RoundDigitRate);

        readonly int IntGridHeight = (int)(gridHeight * RoundDigitRate);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var posY = (int)((y - gridStartPos.Y) * RoundDigitRate);
            var posX = (int)((x - gridStartPos.X) * RoundDigitRate);
            var mode = (posX / IntGridWidth + ((posY / IntGridHeight) % 2)) % 2;
            var yArea = 1.0F - Hlsl.Clamp((posY % IntGridHeight) - IntGridHeight + RoundDigitRate, 0, RoundDigitRate) * RoundDigitDenominator;
            var xArea = 1.0F - Hlsl.Clamp((posX % IntGridWidth) - IntGridWidth + RoundDigitRate, 0, RoundDigitRate) * RoundDigitDenominator;

            var area = xArea;
            if (yArea < 1.0F)
            {
                area = yArea * xArea + (1.0F - yArea) * (1.0F - xArea);
            }

            var color = mode == 0 ? Hlsl.Lerp(color2, color1, area) : Hlsl.Lerp(color1, color2, area);
            image[y * width + x] = color;
        }
    }
}

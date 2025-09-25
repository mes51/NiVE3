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
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shape;
using SixLabors.ImageSharp.Drawing;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Generate_GridLine_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_GridLine_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class GridLine : IEffect
    {
        const string ID = "CC1B9205-0365-4AED-BE29-7B4C8F84825C";

        const string PropertyAnchorPointId = nameof(PropertyAnchorPointId);

        const string PropertyGridSizeGroupId = nameof(PropertyGridSizeGroupId);

        const string PropertyGridSizeGridSizeTypeId = nameof(PropertyGridSizeGridSizeTypeId);

        const string PropertyGridSizeCornerPointId = nameof(PropertyGridSizeCornerPointId);

        const string PropertyGridSizeWidthId = nameof(PropertyGridSizeWidthId);

        const string PropertyGridSizeHeightId = nameof(PropertyGridSizeHeightId);

        const string PropertyThicknessId = nameof(PropertyThicknessId);

        const string PropertyInvertId = nameof(PropertyInvertId);

        const string PropertyColorId = nameof(PropertyColorId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyBlurId = nameof(PropertyBlurId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var center = new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5;
            return
            [
                new Vector3dProperty(PropertyAnchorPointId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_AnchorPoint, center, digit: 2, useInteraction: true),
                new PropertyGroup(PropertyGridSizeGroupId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_GridSize,
                [
                    new EnumProperty(PropertyGridSizeGridSizeTypeId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_GridSize_Type, typeof(GridLineGridSizeType), typeof(LanguageResourceDictionary), GridLineGridSizeType.CornerPoint, selectBoxWidth: 90.0),
                    new Vector3dProperty(PropertyGridSizeCornerPointId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_GridSize_CornerPoint, center + new Vector3d(100.0, 100.0, 0.0), digit: 2, useInteraction: true),
                    new DoubleProperty(PropertyGridSizeWidthId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_GridSize_Width, 100.0, 0.0, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyGridSizeHeightId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_GridSize_Height, 100.0, 0.0, double.MaxValue, digit: 2)
                ]),
                new DoubleProperty(PropertyThicknessId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_Thickness, 1.0, 0.0, double.MaxValue, digit: 2),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_Invert, false),
                new ColorProperty(PropertyColorId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_Color, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new Vector3dProperty(PropertyBlurId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_Blur, Vector3d.Zero, Vector3d.Zero, new Vector3d(10000.0), digit: 2, useLinkRatio: true),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Generate_GridLine_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.ReplaceForce, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var offset = new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0);
            var downSamplingRate = new Vector3d(downSamplingRateX, downSamplingRateY, 1.0);
            var anchorPoint = (Vector2)(properties.GetValue(PropertyAnchorPointId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
            var gridSizeGroup = properties.First(p => p.Id == PropertyGridSizeGroupId).GetChildren() ?? [];
            var gridSizeType = gridSizeGroup.GetValue(PropertyGridSizeGridSizeTypeId, layerTime, GridLineGridSizeType.CornerPoint);
            var cornerPoint = (Vector2)(gridSizeGroup.GetValue(PropertyGridSizeCornerPointId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
            var gridWidth = (float)(gridSizeGroup.GetValue(PropertyGridSizeWidthId, layerTime, 0.0) / downSamplingRateX);
            var gridHeight = (float)(gridSizeGroup.GetValue(PropertyGridSizeHeightId, layerTime, 0.0) / downSamplingRateY);
            var thickness = (float)properties.GetValue(PropertyThicknessId, layerTime, 0.0);
            var invert = properties.GetValue(PropertyInvertId, layerTime, false);
            var color = properties.GetValue(PropertyColorId, layerTime, Vector4.One);
            var opacity = (float)(properties.GetValue(PropertyOpacityId, layerTime, 0.0) * 0.01);
            var blur = (Vector2)(properties.GetValue(PropertyBlurId, layerTime, Vector3d.Zero) / downSamplingRate) / 3.0F;
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Replace);

            switch (gridSizeType)
            {
                case GridLineGridSizeType.CornerPoint:
                    (gridWidth, gridHeight) = Vector2.Abs(cornerPoint - anchorPoint);
                    break;
                case GridLineGridSizeType.Width:
                    gridHeight = gridWidth;
                    break;
            }

            color.W = opacity;

            if (gridWidth <= thickness || gridHeight <= thickness || thickness <= 0.0F)
            {
                var fillColor = Const.EmptyPixel;
                if ((thickness <= 0.0F && invert) ||
                    ((gridWidth <= thickness || gridHeight <= thickness) && !invert))
                {
                    fillColor = color;
                }

                if (useGpu && AcceleratorObject != null)
                {
                    var device = AcceleratorObject.CurrentDevice;
                    var gpuImage = image.ToGpu(device);
                    using var emptyImage = new NGPUImage(image.Width, image.Height, device, fillColor);
                    ImageBlendProcessor.SameSizeGpu(device, gpuImage, emptyImage, roi, blendMode);

                    return gpuImage;
                }
                else
                {
                    var managedImage = image.ToManaged();
                    using var emptyImage = new NManagedImage(image.Width, image.Height, fillColor);
                    ImageBlendProcessor.SameSizeCpu(managedImage, emptyImage, roi, blendMode);

                    return managedImage;
                }
            }

            var lineCenter = thickness * 0.5F;
            var startX = anchorPoint.X - (int)MathF.Ceiling(anchorPoint.X / gridWidth) * gridWidth - lineCenter;
            var startY = anchorPoint.Y - (int)MathF.Ceiling(anchorPoint.Y / gridHeight) * gridHeight - lineCenter;
            var paths = new List<ISimplePath>();
            if (invert)
            {
                startX += thickness;
                startY += thickness;
                for (var y = startY; y < image.Height; y += gridHeight)
                {
                    for (var x = startX; x < image.Width; x += gridWidth)
                    {
                        paths.Add(new RectangularPolygon(x, y, gridWidth - thickness, gridHeight - thickness));
                    }
                }
            }
            else
            {
                for (var x = startX; x <= image.Width; x += gridWidth)
                {
                    paths.Add(new RectangularPolygon(x, -lineCenter, thickness, image.Height + thickness));
                }

                for (var y = startY; y <= image.Height; y += gridHeight)
                {
                    paths.Add(new RectangularPolygon(-lineCenter, y, image.Width + thickness, thickness));
                }
            }
            var polygons = paths.Select(p => new Polygon(p.Points.Span)).ToArray();

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, polygons, color, blur, blendMode);
            }
            else
            {
                return ProcessCpu(image, roi, polygons, color, blur, blendMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Polygon[] polygons, Vector4 color, Vector2 blur, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            using var canvas = new NManagedImage(managedImage.Width, managedImage.Height);
            ShapeRendererCPU.FillPolygonNonZero(polygons, canvas, color);

            if (blur.X > 0.0F || blur.Y > 0.0F)
            {
                BoxBlurProcessor.ProcessCpu(canvas, roi, blur.X, blur.Y, 3, EdgeRepeatMode.Wrap);
            }

            ImageBlendProcessor.SameSizeCpu(managedImage, canvas, roi, blendMode);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Polygon[] polygons, Vector4 color, Vector2 blur, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            using var canvas = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            ShapeRendererGPU.FillPolygonNonZero(device, polygons, canvas, new SolidBrush(color));

            if (blur.X > 0.0F || blur.Y > 0.0F)
            {
                BoxBlurProcessor.ProcessGpu(device, canvas, roi, blur.X, blur.Y, 3, EdgeRepeatMode.Wrap);
            }

            ImageBlendProcessor.SameSizeGpu(device, gpuImage, canvas, roi, blendMode);

            return gpuImage;
        }
    }

    enum GridLineGridSizeType
    {
        CornerPoint,
        Width,
        WidthAndHeight
    }

    file static class Vector2Extensions
    {
        public static void Deconstruct(this Vector2 v, out float x, out float y)
        {
            x = v.X;
            y = v.Y;
        }
    }
}

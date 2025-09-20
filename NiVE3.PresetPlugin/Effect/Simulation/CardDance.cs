using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Drawing.Primitive3D;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_CardDance_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_CardDance_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class CardDance : IEffect
    {
        const string ID = "05A641A8-F97D-4E33-9F2B-B6575242385F";

        const string PropertyRowCountId = nameof(PropertyRowCountId);

        const string PropertyColumnCountId = nameof(PropertyColumnCountId);

        const string PropertySourceLayer1Id = nameof(PropertySourceLayer1Id);

        const string PropertySourceLayer2Id = nameof(PropertySourceLayer2Id);

        const string PropertyRotateOrderTypeId = nameof(PropertyRotateOrderTypeId);

        const string PropertyTransformOrderTypeId = nameof(PropertyTransformOrderTypeId);

        const string ValueGroupSourceLayerIndexIdPostfix = "SourceLayerIndexId";

        const string ValueGroupSourceTypeIdPostfix = "SourceType";

        const string ValueGroupIsInvertIdPostfix = "IsInvert";

        const string ValueGroupMultiplyIdPostfix = "Multiply";

        const string ValueGroupOffsetIdPostfix = "Offset";

        const string PropertyPositionXGroupId = nameof(PropertyPositionXGroupId);

        const string PropertyPositionXGroupIdPrefix = "PropertyPositionX";

        const string PropertyPositionXSourceLayerIndexId = $"{PropertyPositionXGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyPositionXSourceTypeId = $"{PropertyPositionXGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyPositionXIsInvertId = $"{PropertyPositionXGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyPositionXMultiplyId = $"{PropertyPositionXGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyPositionXOffsetId = $"{PropertyPositionXGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyPositionYGroupId = nameof(PropertyPositionYGroupId);

        const string PropertyPositionYGroupIdPrefix = "PropertyPositionY";

        const string PropertyPositionYSourceLayerIndexId = $"{PropertyPositionYGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyPositionYSourceTypeId = $"{PropertyPositionYGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyPositionYIsInvertId = $"{PropertyPositionYGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyPositionYMultiplyId = $"{PropertyPositionYGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyPositionYOffsetId = $"{PropertyPositionYGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyPositionZGroupId = nameof(PropertyPositionZGroupId);

        const string PropertyPositionZGroupIdPrefix = "PropertyPositionZ";

        const string PropertyPositionZSourceLayerIndexId = $"{PropertyPositionZGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyPositionZSourceTypeId = $"{PropertyPositionZGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyPositionZIsInvertId = $"{PropertyPositionZGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyPositionZMultiplyId = $"{PropertyPositionZGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyPositionZOffsetId = $"{PropertyPositionZGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyRotateXGroupId = nameof(PropertyRotateXGroupId);

        const string PropertyRotateXGroupIdPrefix = "PropertyRotateX";

        const string PropertyRotateXSourceLayerIndexId = $"{PropertyRotateXGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyRotateXSourceTypeId = $"{PropertyRotateXGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyRotateXIsInvertId = $"{PropertyRotateXGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyRotateXMultiplyId = $"{PropertyRotateXGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyRotateXOffsetId = $"{PropertyRotateXGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyRotateYGroupId = nameof(PropertyRotateYGroupId);

        const string PropertyRotateYGroupIdPrefix = "PropertyRotateY";

        const string PropertyRotateYSourceLayerIndexId = $"{PropertyRotateYGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyRotateYSourceTypeId = $"{PropertyRotateYGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyRotateYIsInvertId = $"{PropertyRotateYGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyRotateYMultiplyId = $"{PropertyRotateYGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyRotateYOffsetId = $"{PropertyRotateYGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyRotateZGroupId = nameof(PropertyRotateZGroupId);

        const string PropertyRotateZGroupIdPrefix = "PropertyRotateZ";

        const string PropertyRotateZSourceLayerIndexId = $"{PropertyRotateZGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyRotateZSourceTypeId = $"{PropertyRotateZGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyRotateZIsInvertId = $"{PropertyRotateZGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyRotateZMultiplyId = $"{PropertyRotateZGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyRotateZOffsetId = $"{PropertyRotateZGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyScaleXGroupId = nameof(PropertyScaleXGroupId);

        const string PropertyScaleXGroupIdPrefix = "PropertyScaleX";

        const string PropertyScaleXSourceLayerIndexId = $"{PropertyScaleXGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyScaleXSourceTypeId = $"{PropertyScaleXGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyScaleXIsInvertId = $"{PropertyScaleXGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyScaleXMultiplyId = $"{PropertyScaleXGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyScaleXOffsetId = $"{PropertyScaleXGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyScaleYGroupId = nameof(PropertyScaleYGroupId);

        const string PropertyScaleYGroupIdPrefix = "PropertyScaleY";

        const string PropertyScaleYSourceLayerIndexId = $"{PropertyScaleYGroupIdPrefix}{ValueGroupSourceLayerIndexIdPostfix}";

        const string PropertyScaleYSourceTypeId = $"{PropertyScaleYGroupIdPrefix}{ValueGroupSourceTypeIdPostfix}";

        const string PropertyScaleYIsInvertId = $"{PropertyScaleYGroupIdPrefix}{ValueGroupIsInvertIdPostfix}";

        const string PropertyScaleYMultiplyId = $"{PropertyScaleYGroupIdPrefix}{ValueGroupMultiplyIdPostfix}";

        const string PropertyScaleYOffsetId = $"{PropertyScaleYGroupIdPrefix}{ValueGroupOffsetIdPostfix}";

        const string PropertyCameraGroupId = nameof(PropertyCameraGroupId);

        const string PropertyRenderingAntiAliasId = nameof(PropertyRenderingAntiAliasId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var cameraZoom = sourceSize.Width / Const.DefaultCameraFov * 0.5;
            return
            [
                new DoubleProperty(PropertyRowCountId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RowCount, 9.0, 1.0, 1000.0, digit: 0),
                new DoubleProperty(PropertyColumnCountId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_ColumnCount, 16.0, 1.0, 1000.0, digit: 0),
                new UseLayerImageProperty(PropertySourceLayer1Id, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_SourceLayer1, selectBoxWidth: 90.0),
                new UseLayerImageProperty(PropertySourceLayer2Id, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_SourceLayer2, selectBoxWidth: 90.0),
                new EnumProperty(PropertyRotateOrderTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RotateOrderType, typeof(CardDanceRotateOrderType), typeof(LanguageResourceDictionary), CardDanceRotateOrderType.ZYX, selectBoxWidth: 90.0),
                new EnumProperty(PropertyTransformOrderTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_TransformOrderType, typeof(CardDanceTransformOrderType), typeof(LanguageResourceDictionary), CardDanceTransformOrderType.ScaleRotateTranslate, selectBoxWidth: 90.0),
                new PropertyGroup(PropertyPositionXGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_PositionX,
                [
                    new EnumProperty(PropertyPositionXSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyPositionXSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyPositionXIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyPositionXMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 1.0, double.MinValue, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyPositionXOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyPositionYGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_PositionY,
                [
                    new EnumProperty(PropertyPositionYSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyPositionYSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyPositionYIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyPositionYMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 1.0, double.MinValue, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyPositionYOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyPositionZGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_PositionZ,
                [
                    new EnumProperty(PropertyPositionZSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyPositionZSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyPositionZIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyPositionZMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 1.0, double.MinValue, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyPositionZOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyRotateXGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RotateX,
                [
                    new EnumProperty(PropertyRotateXSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyRotateXSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyRotateXIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyRotateXMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 90.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                    new DoubleProperty(PropertyRotateXOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyRotateYGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RotateY,
                [
                    new EnumProperty(PropertyRotateYSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyRotateYSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyRotateYIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyRotateYMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 90.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                    new DoubleProperty(PropertyRotateYOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyRotateZGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RotateZ,
                [
                    new EnumProperty(PropertyRotateZSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyRotateZSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyRotateZIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyRotateZMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 90.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                    new DoubleProperty(PropertyRotateZOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new PropertyGroup(PropertyScaleXGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_ScaleX,
                [
                    new EnumProperty(PropertyScaleXSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyScaleXSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyScaleXIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyScaleXMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 100.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyScaleXOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                ]),
                new PropertyGroup(PropertyScaleYGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_ScaleY,
                [
                    new EnumProperty(PropertyScaleYSourceLayerIndexId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceLayerIndex, typeof(CardDanceSourceLayerIndex), typeof(LanguageResourceDictionary), CardDanceSourceLayerIndex.Index1, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyScaleYSourceTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_SourceType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyScaleYIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_IsInvert, false),
                    new DoubleProperty(PropertyScaleYMultiplyId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Multiply, 100.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyScaleYOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Values_Offset, 0.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                ]),
                new PropertyGroup(
                    PropertyCameraGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_Camera,
                    [
                        new CheckBoxProperty(CameraProperties.PropertyCameraUseCompositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_UseComposition, false),
                        new Vector3dProperty(CameraProperties.PropertyCameraPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true, useInteraction : true),
                        new Vector3dProperty(CameraProperties.PropertyCameraPositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Position, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, -cameraZoom), digit: 2, is3D: true, useInteraction : true),
                        new DirectionProperty(CameraProperties.PropertyCameraOrientationId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Orientation, Vector3d.Zero, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraXAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_XAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraYAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_YAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraZAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_ZAngle, 0.0, digit: 2),
                        new DoubleProperty(CameraProperties.PropertyCameraZoomId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Zoom, cameraZoom, 0.01, double.MaxValue, digit: 2)
                    ]
                ),
                new CheckBoxProperty(PropertyRenderingAntiAliasId, LanguageResourceDictionary.ResourceKeys.Simulation_CardDance_RenderingAntiAlias, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var rowCount = (int)properties.GetValue(PropertyRowCountId, layerTime, 1.0);
            var colCount = (int)properties.GetValue(PropertyColumnCountId, layerTime, 1.0);
            var sourceLayer1Target = properties.GetValue(PropertySourceLayer1Id, layerTime, UseLayerImageTarget.Empty);
            var sourceLayer2Target = properties.GetValue(PropertySourceLayer2Id, layerTime, UseLayerImageTarget.Empty);
            var rotateOrderType = properties.GetValue(PropertyRotateOrderTypeId, layerTime, CardDanceRotateOrderType.ZYX);
            var transformOrderType = properties.GetValue(PropertyTransformOrderTypeId, layerTime, CardDanceTransformOrderType.ScaleRotateTranslate);
            var positionXMapping = GetMappingValue(properties.First(p => p.Id == PropertyPositionXGroupId).GetChildren() ?? [], layerTime, PropertyPositionXGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var positionYMapping = GetMappingValue(properties.First(p => p.Id == PropertyPositionYGroupId).GetChildren() ?? [], layerTime, PropertyPositionYGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var positionZMapping = GetMappingValue(properties.First(p => p.Id == PropertyPositionZGroupId).GetChildren() ?? [], layerTime, PropertyPositionZGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var rotateXMapping = GetMappingValue(properties.First(p => p.Id == PropertyRotateXGroupId).GetChildren() ?? [], layerTime, PropertyRotateXGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var rotateYMapping = GetMappingValue(properties.First(p => p.Id == PropertyRotateYGroupId).GetChildren() ?? [], layerTime, PropertyRotateYGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var rotateZMapping = GetMappingValue(properties.First(p => p.Id == PropertyRotateZGroupId).GetChildren() ?? [], layerTime, PropertyRotateZGroupIdPrefix, 1.0F, 1.0F, 0.0F);
            var scaleXMapping = GetMappingValue(properties.First(p => p.Id == PropertyScaleXGroupId).GetChildren() ?? [], layerTime, PropertyScaleXGroupIdPrefix, 0.01F, 0.01F, 1.0F);
            var scaleYMapping = GetMappingValue(properties.First(p => p.Id == PropertyScaleYGroupId).GetChildren() ?? [], layerTime, PropertyScaleYGroupIdPrefix, 0.01F, 0.01F, 1.0F);
            var antialias = properties.GetValue(PropertyRenderingAntiAliasId, layerTime, false);

            var globalTime = layerTime + layer.SourceStartPoint;
            using var gradientImage1 = sourceLayer1Target.GetImage(composition, globalTime, downSamplingRateX, useGpu);
            using var gradientImage2 = sourceLayer2Target.GetImage(composition, globalTime, downSamplingRateX, useGpu);
            var managedGradientImage1 = gradientImage1?.ToManaged();
            var managedGradientImage2 = gradientImage2?.ToManaged();

            var realWidth = (int)(image.Width * downSamplingRateX);
            var realHeight = (int)(image.Height * downSamplingRateY);
            var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(properties.First(p => p.Id == PropertyCameraGroupId).GetChildren() ?? [], composition, layer, layerTime, roi, image.Width, image.Height, downSamplingRateX, downSamplingRateY);

            NImage canvas;
            Renderer3DBase renderer;
            if (useGpu && AcceleratorObject != null)
            {
                canvas = new NGPUImage(image.Width, image.Height, AcceleratorObject.CurrentDevice);
                renderer = new GPURenderer3D((NGPUImage)canvas, AcceleratorObject.CurrentDevice, realWidth, realHeight, [], [], [], []);
            }
            else
            {
                canvas = new NManagedImage(image.Width, image.Height);
                renderer = new CPURenderer3D((NManagedImage)canvas, realWidth, realHeight, [], [], [], []);
            }
            renderer.ViewMatrix = viewMatrix;
            renderer.FieldOfView = fov;

            var size = Math.Max(realWidth, realHeight);
            var rectWidth = realWidth / (double)colCount / size;
            var rectHeight = realHeight / (double)rowCount / size;
            var textureUAdd = 1.0 / colCount;
            var textureVAdd = 1.0 / rowCount;
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var source1 = GetColor(managedGradientImage1, r, c, rowCount, colCount);
                    var source2 = GetColor(managedGradientImage2, r, c, rowCount, colCount);

                    var positionX = positionXMapping.GetValue(source1, source2) / downSamplingRateX / size;
                    var positionY = positionYMapping.GetValue(source1, source2) / downSamplingRateY / size;
                    var positionZ = positionZMapping.GetValue(source1, source2) / downSamplingRateX / size;
                    var rotateX = rotateXMapping.GetValue(source1, source2);
                    var rotateY = rotateYMapping.GetValue(source1, source2);
                    var rotateZ = rotateZMapping.GetValue(source1, source2);
                    var scaleX = scaleXMapping.GetValue(source1, source2);
                    var scaleY = scaleYMapping.GetValue(source1, source2);

                    var modelMatrix = Matrix4x4d.CreateTranslate(rectWidth * -0.5, rectHeight * -0.5, 0.0);
                    switch (transformOrderType)
                    {
                        case CardDanceTransformOrderType.TranslateRotateScale:
                            modelMatrix *= Matrix4x4d.CreateTranslate(positionX, positionY, positionZ) * CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType) * Matrix4x4d.CreateScale(scaleX, scaleY, 1.0);
                            break;
                        case CardDanceTransformOrderType.TranslateScaleRotate:
                            modelMatrix *= Matrix4x4d.CreateTranslate(positionX, positionY, positionZ) * Matrix4x4d.CreateScale(scaleX, scaleY, 1.0) * CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType);
                            break;
                        case CardDanceTransformOrderType.RotateTranslateScale:
                            modelMatrix *= CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType) * Matrix4x4d.CreateTranslate(positionX, positionY, positionZ) * Matrix4x4d.CreateScale(scaleX, scaleY, 1.0);
                            break;
                        case CardDanceTransformOrderType.RotateScaleTranslate:
                            modelMatrix *= CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType) * Matrix4x4d.CreateScale(scaleX, scaleY, 1.0) * Matrix4x4d.CreateTranslate(positionX, positionY, positionZ);
                            break;
                        case CardDanceTransformOrderType.ScaleTranslateRotate:
                            modelMatrix *= Matrix4x4d.CreateScale(scaleX, scaleY, 1.0) * Matrix4x4d.CreateTranslate(positionX, positionY, positionZ) * CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType);
                            break;
                        default:
                            modelMatrix *= Matrix4x4d.CreateScale(scaleX, scaleY, 1.0) * CreateRotateMatrix(rotateX, rotateY, rotateZ, rotateOrderType) * Matrix4x4d.CreateTranslate(positionX, positionY, positionZ);
                            break;
                    }
                    modelMatrix *= Matrix4x4d.CreateTranslate(rectWidth * (c + 0.5), rectHeight * (r + 0.5), 0.0);

                    var uv1 = new UVVertex(Vector256.Create(0.0, 0.0, 0.0, 1.0), textureUAdd * c, textureVAdd * r);
                    var uv2 = new UVVertex(Vector256.Create(rectWidth, 0.0, 0.0, 1.0), textureUAdd * (c + 1), textureVAdd * r);
                    var uv3 = new UVVertex(Vector256.Create(rectWidth, rectHeight, 0.0, 1.0), textureUAdd * (c + 1), textureVAdd * (r + 1));
                    var uv4 = new UVVertex(Vector256.Create(0.0, rectHeight, 0.0, 1.0), textureUAdd * c, textureVAdd * (r + 1));

                    renderer.AddTriangle(
                        Int32Point.Zero,
                        image,
                        ImageInterpolationQuality.Level2,
                        Vector4.One,
                        uv1, uv2, uv3,
                        1.0F,
                        BlendMode.Normal,
                        modelMatrix,
                        ShadowCastMode.None,
                        0.0F,
                        false,
                        false,
                        1.0F,
                        1.0F,
                        0.0F,
                        0.0F,
                        0.0F,
                        null
                    );
                    renderer.AddTriangle(
                        Int32Point.Zero,
                        image,
                        ImageInterpolationQuality.Level2,
                        Vector4.One,
                        uv1, uv3, uv4,
                        1.0F,
                        BlendMode.Normal,
                        modelMatrix,
                        ShadowCastMode.None,
                        0.0F,
                        false,
                        false,
                        1.0F,
                        1.0F,
                        0.0F,
                        0.0F,
                        0.0F,
                        null
                    );
                }
            }

            var resultImage = image;
            switch (renderer)
            {
                case GPURenderer3D g when AcceleratorObject != null:
                    {
                        g.Render(antialias, antialias);
                        var device = AcceleratorObject.CurrentDevice;
                        var gpuImage = image.ToGpu(device);
                        ImageBlendProcessor.TransferSameSizeGpu(device, gpuImage, (NGPUImage)canvas, roi);

                        resultImage = gpuImage;
                    }
                    break;
                case CPURenderer3D c:
                    {
                        c.Render(antialias, antialias);
                        var managedImage = image.ToManaged();
                        ImageBlendProcessor.TransferSameSizeCpu(managedImage, (NManagedImage)canvas, roi);

                        resultImage = managedImage;
                    }
                    break;
            }

            if (managedGradientImage1 != gradientImage1)
            {
                managedGradientImage1?.Dispose();
            }
            if (managedGradientImage2 != gradientImage2)
            {
                managedGradientImage2?.Dispose();
            }

            return resultImage;
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static MappingValue GetMappingValue(IReadOnlyCollection<IPropertyObject> properties, Time layerTime, string propertyPrefix, float multiplyRate, float offsetRate, float defaultValue)
        {
            var index = properties.GetValue($"{propertyPrefix}{ValueGroupSourceLayerIndexIdPostfix}", layerTime, CardDanceSourceLayerIndex.Index1);
            var sourceType = properties.GetValue($"{propertyPrefix}{ValueGroupSourceTypeIdPostfix}", layerTime, LuminanceAndSingleChannelType.Luminance);
            var isInvert = properties.GetValue($"{propertyPrefix}{ValueGroupIsInvertIdPostfix}", layerTime, false);
            var multiply = (float)properties.GetValue($"{propertyPrefix}{ValueGroupMultiplyIdPostfix}", layerTime, 0.0) * multiplyRate;
            var offset = (float)properties.GetValue($"{propertyPrefix}{ValueGroupOffsetIdPostfix}", layerTime, 0.0) * offsetRate;

            return new MappingValue(index, sourceType, isInvert, multiply, offset, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4? GetColor(NManagedImage? image, int row, int col, int rowCount, int colCount)
        {
            if (image == null)
            {
                return null;
            }

            var x = ((col + 0.5F) / colCount) * image.Width;
            var y = ((row + 0.5F) / rowCount) * image.Height;

            return ImageInterpolation.Bilinear(image.Data, image.Width, image.Height, x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Matrix4x4d CreateRotateMatrix(double rotateX, double rotateY, double rotateZ, CardDanceRotateOrderType order)
        {
            return order switch
            {
                CardDanceRotateOrderType.XYZ => Matrix4x4d.CreateRotateX(rotateX) * Matrix4x4d.CreateRotateY(rotateY) * Matrix4x4d.CreateRotateZ(rotateZ),
                CardDanceRotateOrderType.XZY => Matrix4x4d.CreateRotateX(rotateX) * Matrix4x4d.CreateRotateZ(rotateZ) * Matrix4x4d.CreateRotateY(rotateY),
                CardDanceRotateOrderType.YXZ => Matrix4x4d.CreateRotateY(rotateY) * Matrix4x4d.CreateRotateX(rotateX) * Matrix4x4d.CreateRotateZ(rotateZ),
                CardDanceRotateOrderType.YZX => Matrix4x4d.CreateRotateY(rotateY) * Matrix4x4d.CreateRotateZ(rotateZ) * Matrix4x4d.CreateRotateX(rotateX),
                CardDanceRotateOrderType.ZXY => Matrix4x4d.CreateRotateZ(rotateZ) * Matrix4x4d.CreateRotateX(rotateX) * Matrix4x4d.CreateRotateY(rotateY),
                _ => Matrix4x4d.CreateRotateZ(rotateZ) * Matrix4x4d.CreateRotateY(rotateY) * Matrix4x4d.CreateRotateX(rotateX)
            };
        }
    }

    enum CardDanceRotateOrderType
    {
        XYZ,
        XZY,
        YXZ,
        YZX,
        ZXY,
        ZYX
    }

    enum CardDanceTransformOrderType
    {
        TranslateRotateScale,
        TranslateScaleRotate,
        RotateTranslateScale,
        RotateScaleTranslate,
        ScaleTranslateRotate,
        ScaleRotateTranslate
    }

    enum CardDanceSourceLayerIndex
    {
        Index1,
        Index2
    }

    record MappingValue(CardDanceSourceLayerIndex Index, LuminanceAndSingleChannelType SourceType, bool IsInvert, float Multiply, float Offset, float DefaultValue)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(Vector4? source1, Vector4? source2)
        {
            var color = Index == CardDanceSourceLayerIndex.Index1 ? source1 : source2;

            if (color == null)
            {
                return DefaultValue;
            }

            var rawColor = color.Value;
            var v = SourceType switch
            {
                LuminanceAndSingleChannelType.R => rawColor.Z,
                LuminanceAndSingleChannelType.G => rawColor.Y,
                LuminanceAndSingleChannelType.B => rawColor.X,
                LuminanceAndSingleChannelType.A => rawColor.W,
                _ => Vector4.Dot(rawColor, Const.ConvertToGrayScale)
            };
            if (IsInvert)
            {
                v = 1.0F - v;
            }

            return v * Multiply + Offset;
        }
    }
}

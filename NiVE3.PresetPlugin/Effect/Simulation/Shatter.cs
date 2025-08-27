using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using DelaunatorSharp;
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
using NiVE3.Shape;
using NiVE3.Shared.Extension;
using NiVE3.Shared.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using Point = DelaunatorSharp.Point;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_Shatter_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_Shatter_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Shatter : IEffect
    {
        const float ShapeWireframeThickness = 1.0F;

        const float ForceWireframeThickness = 2.0F;

        const int RandomShapePointCountInGrid = 5;

        const int ForceSamplingCount = 10;

        const string ID = "6F0AF393-2661-4B2E-B637-89D1E34D4216";

        const string PropertyShapeGroupId = nameof(PropertyShapeGroupId);

        const string PropertyShapeShapeTypeId = nameof(PropertyShapeShapeTypeId);

        const string PropertyShapeRandomSeedId = nameof(PropertyShapeRandomSeedId);

        const string PropertyShapeSizeId = nameof(PropertyShapeSizeId);

        const string PropertyForcesId = nameof(PropertyForcesId);

        const string PropertyForceForceItemId = nameof(PropertyForceForceItemId);

        const string PropertyForceForceGroupId = nameof(PropertyForceForceGroupId);

        const string PropertyForcePositionId = nameof(PropertyForcePositionId);

        const string PropertyForceRadiusId = nameof(PropertyForceRadiusId);

        const string PropertyForcePowerId = nameof(PropertyForcePowerId);

        const string PropertyForceStartTimeId = nameof(PropertyForceStartTimeId);

        const string PropertyWorldGroupId = nameof(PropertyWorldGroupId);

        const string PropertyWorldGravityId = nameof(PropertyWorldGravityId);

        const string PropertyWorldGravityDirectionId = nameof(PropertyWorldGravityDirectionId);

        const string PropertyWorldAirRegistanceId = nameof(PropertyWorldAirRegistanceId);

        const string PropertyCameraGroupId = nameof(PropertyCameraGroupId);

        const string PropertyRenderingGroupId = nameof(PropertyRenderingGroupId);

        const string PropertyRenderingDisplayTypeId = nameof(PropertyRenderingDisplayTypeId);

        const string PropertyRenderingAntiAliasId = nameof(PropertyRenderingAntiAliasId);

        const string PropertyOptionGroupId = nameof(PropertyOptionGroupId);

        const string PropertyOptionSimulationRateId = nameof(PropertyOptionSimulationRateId);

        const string PropertyOptionRandomSeedId = nameof(PropertyOptionRandomSeedId);

        static readonly Vector4 ShapeWireframeColor = Vector4.One;

        static readonly Vector4 ForceWireframeColor = new Vector4(1.0F, 0.0F, 0.0F, 1.0F);

        IAcceleratorObject? AcceleratorObject { get; set; }

        Dictionary<Time, SimulatedShatterShapeData[]> SimulatedShapes { get; } = [];

        List<Time> SimulatedShapeTimes { get; } = [];

        Int128 LastPropertyHash { get; set; }

        ShatterShape[] CurrentShapes { get; set; } = [];

        Time LastSimulateTime { get; set; } = Time.Zero;

        double LastSimulateFrameRate { get; set; }

        int LastSimulationRate { get; set; }

        int LastSimulationRandomSeed { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var cameraZoom = sourceSize.Width / Const.DefaultCameraFov * 0.5;
            return
            [
                new PropertyGroup(PropertyShapeGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Shape,
                [
                    new EnumProperty(PropertyShapeShapeTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Shape_ShapeType, typeof(ShatterShapeType), typeof(LanguageResourceDictionary), ShatterShapeType.Triangle1, false, 90.0),
                    new DoubleProperty(PropertyShapeRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Shape_ShapeRandomSeed, 0.0, 0.0, int.MaxValue, false, digit: 0),
                    new DoubleProperty(PropertyShapeSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Shape_Size, 100.0, 2.0, double.MaxValue, false, digit: 2)
                ]),
                new AppendableProperty(PropertyForcesId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force,
                [
                    new AppendablePropertyItem(PropertyForceForceItemId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_ForceItem, () =>
                        new PropertyGroup(PropertyForceForceGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_Force,
                        [
                            new Vector3dProperty(PropertyForcePositionId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_Position, new Vector3d(sourceSize.Width, sourceSize.Height, 400.0) * 0.5, false, 2, true, useInteraction: true),
                            new DoubleProperty(PropertyForceRadiusId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_Radius, Math.Min(sourceSize.Width, sourceSize.Height) * 0.5, 0.0, double.MaxValue, false, digit: 2),
                            new DoubleProperty(PropertyForcePowerId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_Power, 200.0, 0.0, double.MaxValue, false, digit: 2),
                            new DoubleProperty(PropertyForceStartTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Force_StartTime, 0.01, double.MinValue, double.MaxValue, false, digit: 6, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second)
                        ])
                    )
                ], 0, true),
                new PropertyGroup(
                    PropertyWorldGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_World,
                    [
                        new DoubleProperty(PropertyWorldGravityId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_World_Gravity, 50.0, 0.0, double.MaxValue, digit: 2),
                        new DirectionProperty(PropertyWorldGravityDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_World_GravityDirection, new Vector3d(0.0, 0.0, 180.0), digit: 2),
                        new DoubleProperty(PropertyWorldAirRegistanceId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_World_AirRegistance, 0.001, 0.0, double.MaxValue, digit: 6)
                    ]
                ),
                new PropertyGroup(
                    PropertyCameraGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera,
                    [
                        new CheckBoxProperty(CameraProperties.PropertyCameraUseCompositionId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_UseComposition, false),
                        new Vector3dProperty(CameraProperties.PropertyCameraPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true, useInteraction : true),
                        new Vector3dProperty(CameraProperties.PropertyCameraPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_Position, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, -cameraZoom), digit: 2, is3D: true, useInteraction : true),
                        new DirectionProperty(CameraProperties.PropertyCameraOrientationId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_Orientation, Vector3d.Zero, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraXAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_XAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraYAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_YAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraZAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_ZAngle, 0.0, digit: 2),
                        new DoubleProperty(CameraProperties.PropertyCameraZoomId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Camera_Zoom, cameraZoom, 0.01, double.MaxValue, digit: 2)
                    ]
                ),
                new PropertyGroup(
                    PropertyRenderingGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Rendering,
                    [
                        new EnumProperty(PropertyRenderingDisplayTypeId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Rendering_DisplayType, typeof(ShatterDisplayType), typeof(LanguageResourceDictionary), ShatterDisplayType.Wireframe, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyRenderingAntiAliasId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Rendering_AntiAlias, true)
                    ]
                ),
                new PropertyGroup(
                    PropertyOptionGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Option,
                    [
                        new DoubleProperty(PropertyOptionSimulationRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_SimulationRate, 16.0, 1.0, 100.0, false, digit: 0),
                        new DoubleProperty(PropertyOptionRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Shatter_Option_RandomSeed, 0.0, 0.0, int.MaxValue, false, digit: 0)
                    ]
                )
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var realWidth = (int)Math.Round(image.Width * downSamplingRateX);
            var realHeight = (int)Math.Round(image.Height * downSamplingRateY);

            var shapeGroup = properties.First(p => p.Id == PropertyShapeGroupId).GetChildren() ?? [];
            var forces = properties.First(p => p.Id == PropertyForcesId).GetChildren() ?? [];
            var worldGroup = properties.First(p => p.Id == PropertyWorldGroupId).GetChildren() ?? [];
            var cameraGroup = properties.First(p => p.Id == PropertyCameraGroupId).GetChildren() ?? [];
            var renderingGroup = properties.First(p => p.Id == PropertyRenderingGroupId).GetChildren() ?? [];
            var optionGroup = properties.First(p => p.Id == PropertyOptionGroupId).GetChildren() ?? [];
            var simulationRate = (int)optionGroup.GetValue(PropertyOptionSimulationRateId, layerTime, 0.0);
            var simulationRandomSeed = (int)optionGroup.GetValue(PropertyOptionRandomSeedId, layerTime, 0.0);

            var hash = new XxHash3();
            foreach (var p in shapeGroup.Concat(forces).Concat(worldGroup))
            {
                p.CalcValuesHash(hash);
            }
            var propertyHash = hash.ToInt128();
            if (LastSimulateFrameRate != composition.FrameRate || LastSimulationRate != simulationRate || LastSimulationRandomSeed != simulationRandomSeed || LastPropertyHash != propertyHash)
            {
                CurrentShapes = GenerateShapes(realWidth, realHeight, roi.OriginalImageSize, roi.OriginalImagePosition, shapeGroup, layerTime);
                SimulatedShapes.Clear();
                SimulatedShapeTimes.Clear();
                LastSimulateTime = Time.Zero;
                LastSimulateFrameRate = composition.FrameRate;
                LastSimulationRate = simulationRate;
                LastSimulationRandomSeed = simulationRandomSeed;
                LastPropertyHash = propertyHash;
            }

            if (LastSimulateTime <= layerTime)
            {
                var size = Math.Max(realWidth, realHeight);
                SimulateShatter(size, layerTime + new Time(1, composition.FrameRate), composition.FrameRate, forces, worldGroup, simulationRate, simulationRandomSeed);

                LastSimulateTime = layerTime;
            }

            var displayType = renderingGroup.GetValue(PropertyRenderingDisplayTypeId, layerTime, ShatterDisplayType.Wireframe);
            var antialias = renderingGroup.GetValue(PropertyRenderingAntiAliasId, layerTime, false);
            var shapes = SimulatedShapes[SimulatedShapeTimes.First(t => t >= layerTime)];
            switch (displayType)
            {
                case ShatterDisplayType.WireframeWithForce:
                    if (useGpu && AcceleratorObject != null)
                    {
                        return RenderWireframeWithForceGpu(AcceleratorObject.CurrentDevice, image, roi, shapes, forces, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY, antialias);
                    }
                    else
                    {
                        return RenderWireframeWithForceCpu(image, roi, shapes, forces, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY, antialias);
                    }
                case ShatterDisplayType.Rendering:
                    {
                        var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(cameraGroup, composition, layer, layerTime, roi, image.Width, image.Height, downSamplingRateX, downSamplingRateY);

                        NImage canvas;
                        Renderer3DBase renderer;
                        if (useGpu && AcceleratorObject != null)
                        {
                            var device = AcceleratorObject.CurrentDevice;
                            canvas = new NGPUImage(image.Width, image.Height, device);
                            renderer = new GPURenderer3D((NGPUImage)canvas, device, realWidth, realHeight, [], [], [], []);
                        }
                        else
                        {
                            canvas = new NManagedImage(image.Width, image.Height);
                            renderer = new CPURenderer3D((NManagedImage)canvas, realWidth, realHeight, [], [], [], []);
                        }

                        renderer.ViewMatrix = viewMatrix;
                        renderer.FieldOfView = fov;

                        foreach (var shape in shapes)
                        {
                            foreach (var (v1, v2, v3) in shape.Triangles)
                            {
                                renderer.AddTriangle(
                                    Int32Point.Zero,
                                    image,
                                    antialias ? ImageInterpolationQuality.Level1 : ImageInterpolationQuality.Level2,
                                    Vector4.One,
                                    v3,
                                    v2,
                                    v1,
                                    1.0F,
                                    BlendMode.Normal,
                                    shape.ModelMatrix,
                                    ShadowCastMode.None,
                                    0.0F,
                                    false,
                                    false,
                                    1.0F,
                                    1.0F,
                                    1.0F,
                                    1.0F,
                                    1.0F,
                                    null
                                );
                            }
                        }

                        switch (renderer)
                        {
                            case GPURenderer3D g when AcceleratorObject != null:
                                {
                                    g.Render(antialias, antialias);
                                    var device = AcceleratorObject.CurrentDevice;
                                    var gpuImage = image.ToGpu(device);
                                    ImageBlendProcessor.TransferImageGpu(device, gpuImage, (NGPUImage)canvas, roi);

                                    return gpuImage;
                                }
                            case CPURenderer3D c:
                                {
                                    c.Render(antialias, antialias);
                                    var managedImage = image.ToManaged();
                                    ImageBlendProcessor.TransferImageCpu(managedImage, (NManagedImage)canvas, roi);

                                    return managedImage;
                                }
                            default:
                                return image;
                        }
                    }
                default:
                    if (useGpu && AcceleratorObject != null)
                    {
                        return RenderWireframeGpu(AcceleratorObject.CurrentDevice, image, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY, antialias);
                    }
                    else
                    {
                        return RenderWireframeCpu(image, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY, antialias);
                    }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        void SimulateShatter(int size, Time toTime, double frameRate, IReadOnlyCollection<IPropertyObject> forces, IReadOnlyCollection<IPropertyObject> worldGroup, int simulationRate, int simulationRandomSeed)
        {
            if (toTime <= 0.0)
            {
                return;
            }

            var gravityProperty = worldGroup.First(p => p.Id == PropertyWorldGravityId);
            var gravityDirectionProperty = worldGroup.First(p => p.Id == PropertyWorldGravityDirectionId);
            var airRegistanceProperty = worldGroup.First(p => p.Id == PropertyWorldAirRegistanceId);

            var forceValues = forces.Where(p => p.IsEnable).Select(p =>
            {
                var forceProperties = p.GetChildren() ?? [];
                var position = forceProperties.GetValue(PropertyForcePositionId, toTime, Vector3d.Zero).AsVector256() / size;
                var radius = forceProperties.GetValue(PropertyForceRadiusId, toTime, 0.0) / size;
                var power = forceProperties.GetValue(PropertyForcePowerId, toTime, 0.0) / size;
                var startTime = forceProperties.GetValue(PropertyForceStartTimeId, toTime, 0.0);

                return (position, radius, power, startTime);
            });

            var frameDuration = new Time(1, frameRate * simulationRate);
            var doubleFrameDuration = (double)frameDuration;
            var rand = new Xoroshiro(simulationRandomSeed);
            for (var currentTime = LastSimulateTime; currentTime < toTime; currentTime += frameDuration)
            {
                if (SimulatedShapes.ContainsKey(currentTime))
                {
                    continue;
                }

                var doubleCurrentTime = (double)currentTime;
                var gravity = gravityProperty.GetValue(currentTime, 0.0) / size;
                var gravityDirection = CalcRotatedVector(gravityDirectionProperty.GetValue(currentTime, Vector3d.Zero));
                var airRegistance = airRegistanceProperty.GetValue(currentTime, 0.0) / size;
                foreach (var shape in CurrentShapes)
                {
                    shape.Advance(doubleFrameDuration * 5.0, gravity, gravityDirection, airRegistance);
                }

                foreach (var (position, radius, power, startTime) in forceValues)
                {
                    var timeDiff = doubleCurrentTime - startTime;
                    if (timeDiff < 0.0 || timeDiff >= doubleFrameDuration)
                    {
                        continue;
                    }

                    foreach (var shape in CurrentShapes)
                    {
                        var speed = Vector256<double>.Zero;
                        var isHit = false;
                        var powerRate = power / shape.Triangles.Sum(t => t.Area) / (ForceSamplingCount * shape.Triangles.Length);
                        foreach (var triangle in shape.Triangles)
                        {
                            var currentPowerRate = powerRate * triangle.Area;
                            for (var i = 0; i < ForceSamplingCount; i++)
                            {
                                var c1 = rand.NextDouble();
                                var c2 = rand.NextDouble();
                                if (c1 + c2 > 1.0)
                                {
                                    c1 = 1.0 - c1;
                                    c2 = 1.0 - c2;
                                }
                                var c3 = 1.0 - c1 - c2;
                                var impactPoint = triangle.V1.Vertex * c1 + triangle.V2.Vertex * c2 + triangle.V3.Vertex * c3;
                                var diff = (position - impactPoint) & Const.WithoutWMask256;
                                var distance = diff.Length();
                                if (distance > radius)
                                {
                                    continue;
                                }
                                if (distance == 0.0)
                                {
                                    speed += Vector256.Create(0.0, 0.0, 1.0, 0.0) * currentPowerRate;
                                    isHit = true;
                                    continue;
                                }

                                var currentPower = Math.Pow(1.0 - distance / radius, 0.1) * currentPowerRate;
                                speed += -diff.Normalize() * currentPower;
                                isHit = true;
                            }
                        }

                        shape.Speed += (Vector3d)speed;
                        shape.RotateSpeed += (Vector3d)((Vector256.Create(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), 0.0) * 2.0 - Vector256.Create(1.0, 1.0, 1.0, 0.0)) * speed.Length() * 360.0);
                        shape.IsForceTouched |= isHit;
                    }
                }

                SimulatedShapes.Add(currentTime, [..CurrentShapes.Select(s => s.ToSimulated())]);
                SimulatedShapeTimes.Add(currentTime);
                LastSimulateTime = currentTime;
            }
        }

        static ShatterShape[] GenerateShapes(int realWidth, int realHeight, Int32Size originalSize, Int32Point origin, IReadOnlyCollection<IPropertyObject> shapeGroup, Time layerTime)
        {
            var size = Math.Max(realWidth, realHeight);

            var shapeSize = shapeGroup.GetValue(PropertyShapeSizeId, layerTime, 0.0);
            if (shapeSize <= 0.0)
            {
                return [];
            }

            var shapes = new List<ShatterShape>();
            switch (shapeGroup.GetValue(PropertyShapeShapeTypeId, layerTime, ShatterShapeType.Triangle1))
            {
                case ShatterShapeType.Triangle2:
                    {
                        const double Sqrt3 = 1.7320508075688772; // Math.Sqrt(3)
                        var shapeSideLength = shapeSize * Sqrt3;
                        var shapeWidth = shapeSideLength * 0.5;
                        var rowCount = (int)Math.Ceiling(realHeight / shapeSideLength) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / shapeSideLength) + 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSideLength) * shapeSideLength;
                        var endX = startX + colCount * shapeSideLength;
                        var y = originalSize.Height * 0.5 + origin.Y - (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeSideLength) * shapeSideLength - shapeSideLength * 0.5;
                        for (var r = 0; r < rowCount && y < realHeight; r++, y += shapeSideLength)
                        {
                            var x = startX;
                            var prevUV1 = new UVVertex(Vector256.Create(x - shapeSideLength, y, 0.0, size) / size, (x - shapeSideLength) / realWidth, y / realHeight);
                            var prevUV2 = new UVVertex(Vector256.Create(x - shapeWidth, y + shapeSideLength, 0.0, size) / size, (x - shapeWidth) / realWidth, (y + shapeSideLength) / realHeight);
                            for (var c = 0; c < colCount && x <= endX; c++, x += shapeSideLength)
                            {
                                var nextV1 = Vector256.Create(x, y, 0.0, size);
                                var nextV2 = Vector256.Create(x + shapeWidth, y + shapeSideLength, 0.0, size);
                                var nextUV1 = new UVVertex(nextV1 / size, nextV1.GetElement(0) / realWidth, nextV1.GetElement(1) / realHeight);
                                var nextUV2 = new UVVertex(nextV2 / size, nextV2.GetElement(0) / realWidth, nextV2.GetElement(1) / realHeight);

                                shapes.Add(new ShatterShape([(prevUV1, nextUV1, prevUV2)]));
                                shapes.Add(new ShatterShape([(nextUV1, nextUV2, prevUV2)]));

                                prevUV1 = nextUV1;
                                prevUV2 = nextUV2;
                            }
                        }
                    }
                    break;
                case ShatterShapeType.Rectangle:
                    {
                        var rowCount = (int)Math.Ceiling(realHeight / shapeSize) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / shapeSize) + 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSize) * shapeSize - shapeSize * 0.5;
                        var y = originalSize.Height * 0.5 + origin.Y - (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeSize) * shapeSize - shapeSize * 0.5;
                        for (var r = 0; r < rowCount && y < realHeight; r++, y += shapeSize)
                        {
                            var x = startX;
                            for (var c = 0; c < colCount && x < realWidth; c++, x += shapeSize)
                            {
                                var v1 = Vector256.Create(x, y, 0.0, size);
                                var v2 = Vector256.Create(x + shapeSize, y, 0.0, size);
                                var v3 = Vector256.Create(x + shapeSize, y + shapeSize, 0.0, size);
                                var v4 = Vector256.Create(x, y + shapeSize, 0.0, size);
                                var uv1 = new UVVertex(v1 / size, v1.GetElement(0) / realWidth, v1.GetElement(1) / realHeight);
                                var uv2 = new UVVertex(v2 / size, v2.GetElement(0) / realWidth, v2.GetElement(1) / realHeight);
                                var uv3 = new UVVertex(v3 / size, v3.GetElement(0) / realWidth, v3.GetElement(1) / realHeight);
                                var uv4 = new UVVertex(v4 / size, v4.GetElement(0) / realWidth, v4.GetElement(1) / realHeight);
                                shapes.Add(new ShatterShape([(uv1, uv2, uv3), (uv1, uv3, uv4)]));
                            }
                        }
                    }
                    break;
                case ShatterShapeType.Hexagon:
                    {
                        var shapeRadius = shapeSize * 0.5;
                        var hexHeight = shapeRadius * Math.Sin(Math.PI / 3.0);
                        var hexSlopeWidth = shapeRadius * Math.Cos(Math.PI / 3.0);
                        var hexSpan = shapeSize + shapeRadius;

                        var rowCount = (int)Math.Ceiling(realHeight / hexHeight) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / hexSpan) + 2;
                        var centerRowIndex = (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / hexHeight);
                        var centerLoopIndex = centerRowIndex % 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / hexSpan) * hexSpan;
                        var y = originalSize.Height * 0.5 + origin.Y - centerRowIndex * hexHeight; // NOTE: どうも行/列の数ではなくシェイプのサイズで変わるっぽい
                        var endY = realHeight + hexHeight;
                        var endX = realWidth + hexSpan;

                        var hexV1 = Vector256.Create(-shapeRadius, 0.0, 0.0, size);
                        var hexV2 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0), hexHeight, 0.0, size);
                        var hexV3 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 2.0), hexHeight, 0.0, size);
                        var hexV4 = Vector256.Create(shapeRadius, 0.0, 0.0, size);
                        var hexV5 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 4.0), -hexHeight, 0.0, size);
                        var hexV6 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 5.0), -hexHeight, 0.0, size);

                        for (var r = 0; r < rowCount && y < endY; r++, y += hexHeight)
                        {
                            var x = startX - ((r % 2) == centerLoopIndex ? 0.0 : (shapeRadius + hexSlopeWidth));
                            for (var c = 0; c < colCount && x < endX; c++, x += hexSpan)
                            {
                                var pos = Vector256.Create(x, y, 0.0, 0.0);
                                var v1 = hexV1 + pos;
                                var v2 = hexV2 + pos;
                                var v3 = hexV3 + pos;
                                var v4 = hexV4 + pos;
                                var v5 = hexV5 + pos;
                                var v6 = hexV6 + pos;
                                var uv1 = new UVVertex(v1 / size, v1.GetElement(0) / realWidth, v1.GetElement(1) / realHeight);
                                var uv2 = new UVVertex(v2 / size, v2.GetElement(0) / realWidth, v2.GetElement(1) / realHeight);
                                var uv3 = new UVVertex(v3 / size, v3.GetElement(0) / realWidth, v3.GetElement(1) / realHeight);
                                var uv4 = new UVVertex(v4 / size, v4.GetElement(0) / realWidth, v4.GetElement(1) / realHeight);
                                var uv5 = new UVVertex(v5 / size, v5.GetElement(0) / realWidth, v5.GetElement(1) / realHeight);
                                var uv6 = new UVVertex(v6 / size, v6.GetElement(0) / realWidth, v6.GetElement(1) / realHeight);

                                shapes.Add(new ShatterShape(
                                [
                                    (uv2, uv3, uv1),
                                    (uv3, uv4, uv1),
                                    (uv4, uv5, uv1),
                                    (uv5, uv6, uv1)
                                ]));
                            }
                        }
                    }
                    break;
                case ShatterShapeType.Brick:
                    {
                        var shapeHeight = shapeSize / 3.0;
                        var rowCount = (int)Math.Ceiling(realHeight / shapeHeight) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / shapeSize) + 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSize) * shapeSize - shapeSize * 0.5;
                        var centerRowIndex = (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeHeight);
                        var centerLoopIndex = centerRowIndex % 2;
                        var y = originalSize.Height * 0.5 + origin.Y - centerRowIndex * shapeHeight - shapeHeight * 0.5;
                        for (var r = 0; r < rowCount && y < realHeight; r++, y += shapeHeight)
                        {
                            var x = startX - ((r % 2) == centerLoopIndex ? -shapeSize * 0.5 : 0.0);
                            for (var c = 0; c < colCount && x < realWidth; c++, x += shapeSize)
                            {   
                                var v1 = Vector256.Create(x, y, 0.0, size);
                                var v2 = Vector256.Create(x + shapeSize, y, 0.0, size);
                                var v3 = Vector256.Create(x + shapeSize, y + shapeHeight, 0.0, size);
                                var v4 = Vector256.Create(x, y + shapeHeight, 0.0, size);
                                var uv1 = new UVVertex(v1 / size, v1.GetElement(0) / realWidth, v1.GetElement(1) / realHeight);
                                var uv2 = new UVVertex(v2 / size, v2.GetElement(0) / realWidth, v2.GetElement(1) / realHeight);
                                var uv3 = new UVVertex(v3 / size, v3.GetElement(0) / realWidth, v3.GetElement(1) / realHeight);
                                var uv4 = new UVVertex(v4 / size, v4.GetElement(0) / realWidth, v4.GetElement(1) / realHeight);
                                shapes.Add(new ShatterShape([(uv1, uv2, uv3), (uv1, uv3, uv4)]));
                            }
                        }
                    }
                    break;
                case ShatterShapeType.Rhombus:
                    {
                        var shapeRadius = shapeSize * 0.5;
                        var hexHeight = shapeRadius * Math.Sin(Math.PI / 3.0);
                        var hexSlopeWidth = shapeRadius * Math.Cos(Math.PI / 3.0);
                        var hexSpan = shapeSize + shapeRadius;

                        var rowCount = (int)Math.Ceiling(realHeight / hexHeight) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / hexSpan) + 2;
                        var centerRowIndex = (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / hexHeight);
                        var centerLoopIndex = centerRowIndex % 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / hexSpan) * hexSpan;
                        var y = originalSize.Height * 0.5 + origin.Y - centerRowIndex * hexHeight; // NOTE: どうも行/列の数ではなくシェイプのサイズで変わるっぽい
                        var endY = realHeight + hexHeight;
                        var endX = realWidth + hexSpan;

                        var hexV1 = Vector256.Create(-shapeRadius, 0.0, 0.0, size);
                        var hexV2 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0), hexHeight, 0.0, size);
                        var hexV3 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 2.0), hexHeight, 0.0, size);
                        var hexV4 = Vector256.Create(shapeRadius, 0.0, 0.0, size);
                        var hexV5 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 4.0), -hexHeight, 0.0, size);
                        var hexV6 = Vector256.Create(-shapeRadius * Math.Cos(Math.PI / 3.0 * 5.0), -hexHeight, 0.0, size);

                        for (var r = 0; r < rowCount && y < endY; r++, y += hexHeight)
                        {
                            var x = startX - ((r % 2) == centerLoopIndex ? 0.0 : (shapeRadius + hexSlopeWidth));
                            for (var c = 0; c < colCount && x < endX; c++, x += hexSpan)
                            {
                                var pos = Vector256.Create(x, y, 0.0, 0.0);
                                var v1 = hexV1 + pos;
                                var v2 = hexV2 + pos;
                                var v3 = hexV3 + pos;
                                var v4 = hexV4 + pos;
                                var v5 = hexV5 + pos;
                                var v6 = hexV6 + pos;
                                var center = Vector256.Create(x, y, 0.0, size);
                                var uv1 = new UVVertex(v1 / size, v1.GetElement(0) / realWidth, v1.GetElement(1) / realHeight);
                                var uv2 = new UVVertex(v2 / size, v2.GetElement(0) / realWidth, v2.GetElement(1) / realHeight);
                                var uv3 = new UVVertex(v3 / size, v3.GetElement(0) / realWidth, v3.GetElement(1) / realHeight);
                                var uv4 = new UVVertex(v4 / size, v4.GetElement(0) / realWidth, v4.GetElement(1) / realHeight);
                                var uv5 = new UVVertex(v5 / size, v5.GetElement(0) / realWidth, v5.GetElement(1) / realHeight);
                                var uv6 = new UVVertex(v6 / size, v6.GetElement(0) / realWidth, v6.GetElement(1) / realHeight);
                                var uvCenter = new UVVertex(center / size, x / realWidth, y / realHeight);

                                shapes.Add(new ShatterShape(
                                [
                                    (uv1, uv2, uvCenter),
                                    (uv2, uv3, uvCenter)
                                ]));
                                shapes.Add(new ShatterShape(
                                [
                                    (uv3, uv4, uvCenter),
                                    (uv4, uv5, uvCenter)
                                ]));
                                shapes.Add(new ShatterShape(
                                [
                                    (uv5, uv6, uvCenter),
                                    (uv6, uv1, uvCenter)
                                ]));
                            }
                        }
                    }
                    break;
                case ShatterShapeType.Random:
                    {
                        var rowCount = (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeSize) + (int)Math.Ceiling((realHeight - originalSize.Height * 0.5 + origin.Y) / shapeSize);
                        var colCount = (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSize) + (int)Math.Ceiling((realWidth - originalSize.Width * 0.5 - origin.X) / shapeSize);
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSize) * shapeSize - shapeSize * 0.5;
                        var startY = originalSize.Height * 0.5 + origin.Y - (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeSize) * shapeSize - shapeSize * 0.5;

                        // see: http://kikikiroku.session.jp/unity-shattered-display-effect-ff10/

                        var points = new List<Point>
                        {
                            new Point(),
                            new Point(realWidth, 0.0),
                            new Point(realWidth, realHeight),
                            new Point(0.0, realHeight)
                        };

                        for (var yc = 0; yc < rowCount; yc++)
                        {
                            var py = yc * shapeSize + startY;
                            points.Add(new Point(0.0, py));
                            points.Add(new Point(realWidth, py));
                        }
                        for (var xc = 0; xc < colCount; xc++)
                        {
                            var px = xc * shapeSize + startX;
                            points.Add(new Point(px, 0.0));
                            points.Add(new Point(px, realHeight));
                        }

                        var randomSeed = (int)shapeGroup.GetValue(PropertyShapeRandomSeedId, layerTime, 0.0);
                        var y = startY;
                        var rand = new Xoroshiro(randomSeed);
                        for (var r = 0; r < rowCount && y <= realHeight; r++, y += shapeSize)
                        {
                            var x = startX;
                            for (var c = 0; c < colCount && x <= realWidth; c++, x += shapeSize)
                            {
                                for (var i = 0; i < RandomShapePointCountInGrid; i++)
                                {
                                    var px = x + rand.NextDouble() * shapeSize;
                                    var py = y + rand.NextDouble() * shapeSize;

                                    points.Add(new Point(px, py));
                                }
                            }
                        }

                        var delaunator = new Delaunator([..points]);
                        delaunator.ForEachTriangle(t =>
                        {
                            var vp = t.Points.Select(p => new UVVertex(Vector256.Create(p.X, p.Y, 0.0, size) / size, p.X / realWidth, p.Y / realHeight)).ToArray();
                            if (vp.Length == 3)
                            {
                                shapes.Add(new ShatterShape([(vp[0], vp[1], vp[2])]));
                            }
                        });
                    }
                    break;
                default:
                    {
                        var rowCount = (int)Math.Ceiling(realHeight / shapeSize) + 2;
                        var colCount = (int)Math.Ceiling(realWidth / shapeSize) + 2;
                        var startX = originalSize.Width * 0.5 + origin.X - (int)Math.Ceiling((originalSize.Width * 0.5 + origin.X) / shapeSize) * shapeSize - shapeSize * 0.5;
                        var y = originalSize.Height * 0.5 + origin.Y - (int)Math.Ceiling((originalSize.Height * 0.5 + origin.Y) / shapeSize) * shapeSize - shapeSize * 0.5;
                        for (var r = 0; r < rowCount && y < realHeight; r++, y += shapeSize)
                        {
                            var x = startX;
                            for (var c = 0; c < colCount && x < realWidth; c++, x += shapeSize)
                            {
                                var v1 = Vector256.Create(x, y, 0.0, size);
                                var v2 = Vector256.Create(x + shapeSize, y, 0.0, size);
                                var v3 = Vector256.Create(x + shapeSize, y + shapeSize, 0.0, size);
                                var v4 = Vector256.Create(x, y + shapeSize, 0.0, size);
                                var uv1 = new UVVertex(v1 / size, v1.GetElement(0) / realWidth, v1.GetElement(1) / realHeight);
                                var uv2 = new UVVertex(v2 / size, v2.GetElement(0) / realWidth, v2.GetElement(1) / realHeight);
                                var uv3 = new UVVertex(v3 / size, v3.GetElement(0) / realWidth, v3.GetElement(1) / realHeight);
                                var uv4 = new UVVertex(v4 / size, v4.GetElement(0) / realWidth, v4.GetElement(1) / realHeight);
                                shapes.Add(new ShatterShape([(uv1, uv2, uv3)]));
                                shapes.Add(new ShatterShape([(uv1, uv3, uv4)]));
                            }
                        }
                    }
                    break;
            }

            return [..shapes];
        }

        static NManagedImage RenderWireframeCpu(NImage image, ROI roi, SimulatedShatterShapeData[] shapes, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY, bool antialias)
        {
            var polygons = ConvertShapeToPolygon(image.Width, image.Height, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);
            using var canvas = new NManagedImage(image.Width, image.Height);
            var brush = new SolidBrush(ShapeWireframeColor);
            if (antialias)
            {
                ShapeRendererCPU.FillPolygonNonZero(polygons, canvas, brush);
            }
            else
            {
                ShapeRendererCPU.FillPolygonNonZeroAliased(polygons, canvas, brush);
            }

            var managedImage = image.ToManaged();
            ImageBlendProcessor.TransferImageCpu(managedImage, canvas, roi);

            return managedImage;
        }

        static NManagedImage RenderWireframeWithForceCpu(NImage image, ROI roi, SimulatedShatterShapeData[] shapes, IReadOnlyCollection<IPropertyObject> forces, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY, bool antialias)
        {
            var polygons = ConvertShapeToPolygon(image.Width, image.Height, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);
            var forcePolygons = CreateForcePolygon(image.Width, image.Height, roi, forces, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);

            using var canvas = new NManagedImage(image.Width, image.Height);
            var brush = new SolidBrush(ShapeWireframeColor);
            var forceBrush = new SolidBrush(ForceWireframeColor);
            if (antialias)
            {
                ShapeRendererCPU.FillPolygonNonZero(polygons, canvas, brush);
                ShapeRendererCPU.FillPolygonNonZero(forcePolygons, canvas, forceBrush);
            }
            else
            {
                ShapeRendererCPU.FillPolygonNonZeroAliased(polygons, canvas, brush);
                ShapeRendererCPU.FillPolygonNonZeroAliased(forcePolygons, canvas, forceBrush);
            }

            var managedImage = image.ToManaged();
            ImageBlendProcessor.TransferImageCpu(managedImage, canvas, roi);

            return managedImage;
        }

        static NGPUImage RenderWireframeGpu(GraphicsDevice device, NImage image, ROI roi, SimulatedShatterShapeData[] shapes, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY, bool antialias)
        {
            var polygons = ConvertShapeToPolygon(image.Width, image.Height, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);
            using var canvas = new NGPUImage(image.Width, image.Height, device);
            var brush = new SolidBrush(ShapeWireframeColor);
            if (antialias)
            {
                ShapeRendererGPU.FillPolygonNonZero(device, polygons, canvas, brush);
            }
            else
            {
                ShapeRendererGPU.FillPolygonNonZeroAliased(device, polygons, canvas, brush);
            }

            var gpuImage = image.ToGpu(device);
            ImageBlendProcessor.TransferImageGpu(device, gpuImage, canvas, roi);

            return gpuImage;
        }

        static NGPUImage RenderWireframeWithForceGpu(GraphicsDevice device, NImage image, ROI roi, SimulatedShatterShapeData[] shapes, IReadOnlyCollection<IPropertyObject> forces, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY, bool antialias)
        {
            var polygons = ConvertShapeToPolygon(image.Width, image.Height, roi, shapes, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);
            var forcePolygons = CreateForcePolygon(image.Width, image.Height, roi, forces, cameraGroup, composition, layer, layerTime, downSamplingRateX, downSamplingRateY);

            using var canvas = new NGPUImage(image.Width, image.Height, device);
            var brush = new SolidBrush(ShapeWireframeColor);
            var forceBrush = new SolidBrush(ForceWireframeColor);
            if (antialias)
            {
                ShapeRendererGPU.FillPolygonNonZero(device, polygons, canvas, brush);
                ShapeRendererGPU.FillPolygonNonZero(device, forcePolygons, canvas, forceBrush);
            }
            else
            {
                ShapeRendererGPU.FillPolygonNonZeroAliased(device, polygons, canvas, brush);
                ShapeRendererGPU.FillPolygonNonZeroAliased(device, forcePolygons, canvas, forceBrush);
            }

            var gpuImage = image.ToGpu(device);
            ImageBlendProcessor.TransferImageGpu(device, gpuImage, canvas, roi);

            return gpuImage;
        }

        static Polygon[] ConvertShapeToPolygon(int imageWidth, int imageHeight, ROI roi, SimulatedShatterShapeData[] shapes, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY)
        {
            var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(cameraGroup, composition, layer, layerTime, roi, imageWidth, imageHeight, downSamplingRateX, downSamplingRateY);
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var realWidth = (int)Math.Round(imageWidth * downSamplingRateX);
            var realHeight = (int)Math.Round(imageHeight * downSamplingRateY);
            var size = Math.Max(realWidth, realHeight);
            var offsetX = (size - realWidth) * 0.5 / size;
            var offsetY = (size - realHeight) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;

            var mvt = viewMatrix * offsetMatrix;
            Matrix4x4d.Invert(viewMatrix, out var invertedViewMatrix);
            invertedViewMatrix = Matrix4x4d.Transpose(invertedViewMatrix);
            var farPoint = viewMatrix.Transform(Vector256.Create(0.0, 0.0, -10000.0, 1.0)) & Const.WithoutWMask256;

            var result = new List<Polygon>();
            foreach (var shape in shapes)
            {
                var triangles = shape.GetTransformedTriangles().Select(t =>
                    new BoundingBoxTriangle(mvt.Transform(t.Item1), mvt.Transform(t.Item2), mvt.Transform(t.Item3), farPoint, invertedViewMatrix)
                );
                triangles = TriangleDivider.ClipAndDivide(triangles);

                var points = new List<Vector128<double>>();
                foreach (var triangle in triangles)
                {
                    var uv1 = triangle.V1.Transform(projectionMatrix).Vertex;
                    var uv2 = triangle.V2.Transform(projectionMatrix).Vertex;
                    var uv3 = triangle.V3.Transform(projectionMatrix).Vertex;

                    uv1 /= Math.Abs(uv1.GetElement(3));
                    uv2 /= Math.Abs(uv2.GetElement(3));
                    uv3 /= Math.Abs(uv3.GetElement(3));
                    var dvv1 = Avx.ExtractVector128((uv1 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                    var dvv2 = Avx.ExtractVector128((uv2 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                    var dvv3 = Avx.ExtractVector128((uv3 + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);

                    points.Add(dvv1);
                    points.Add(dvv2);
                    points.Add(dvv3);
                }

                points = [.. points.Distinct()];
                if (points.Count < 3)
                {
                    continue;
                }

                var pointSpan = CollectionsMarshal.AsSpan(points);
                var orderedPoints = new Vector128<double>[points.Count];
                var used = 1;
                orderedPoints[0] = points.MinBy(p => p.GetElement(0));
                var prev = orderedPoints[0];
                while (used < orderedPoints.Length)
                {
                    var b = pointSpan[0];
                    for (var i = 1; i < pointSpan.Length; i++)
                    {
                        var c = pointSpan[i];
                        if (b == prev)
                        {
                            b = c;
                        }
                        else
                        {
                            var ab = b - prev;
                            var ac = c - prev;
                            var v = ab.CrossProduct(ac);
                            if (v > 0.0 || (v == 0.0 && ac.LengthSquared() > ab.LengthSquared()))
                            {
                                b = c;
                            }
                        }
                    }

                    orderedPoints[used] = b;
                    prev = b;
                    used++;
                }

                var polygons = new Path([..orderedPoints.Select(p => new PointF((float)p.GetElement(0), (float)p.GetElement(1)))])
                    .AsClosedPath()
                    .GenerateOutline(ShapeWireframeThickness)
                    .Flatten()
                    .Select(p => new Polygon(p.Points.Span));
                result.AddRange(polygons);
            }

            return [..result];
        }

        static Polygon[] CreateForcePolygon(int imageWidth, int imageHeight, ROI roi, IReadOnlyCollection<IPropertyObject> forces, IReadOnlyCollection<IPropertyObject> cameraGroup, ICompositionObject composition, ILayerObject layer, Time layerTime, double downSamplingRateX, double downSamplingRateY)
        {
            var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(cameraGroup, composition, layer, layerTime, roi, imageWidth, imageHeight, downSamplingRateX, downSamplingRateY);
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(fov, 1.0, double.Epsilon, double.PositiveInfinity);
            var realWidth = (int)Math.Round(imageWidth * downSamplingRateX);
            var realHeight = (int)Math.Round(imageHeight * downSamplingRateY);
            var size = Math.Max(realWidth, realHeight);
            var offsetX = (size - realWidth) * 0.5 / size;
            var offsetY = (size - realHeight) * 0.5 / size;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var projectionOffset = Vector256.Create(offsetX, offsetY, 0.0, 0.0) * size;

            var mvt = viewMatrix * offsetMatrix;

            var result = new List<Polygon>();
            foreach (var force in forces)
            {
                var forceProperties = force.GetChildren() ?? [];
                var position = forceProperties.GetValue(PropertyForcePositionId, layerTime, Vector3d.Zero).AsVector256() + Vector256.Create(0.0, 0.0, 0.0, size);
                var radius = forceProperties.GetValue(PropertyForceRadiusId, layerTime, 0.0);
                var forceTopPos = position + Vector256.Create(0.0, radius, 0.0, 0.0);

                var uvp = projectionMatrix.Transform(mvt.Transform(position / size));
                var uvr = projectionMatrix.Transform(mvt.Transform(forceTopPos / size));
                uvp /= uvp.GetElement(3);
                uvr /= uvr.GetElement(3);
                var dvp = Avx.ExtractVector128((uvp + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);
                var dvr = Avx.ExtractVector128((uvr + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(size * 0.5, size * 0.5, 1.0, 1.0) - projectionOffset, 0);

                var length = (float)Math.Sqrt((dvr - dvp).LengthSquared());
                var circle = new EllipsePolygon(new PointF((float)dvp.GetElement(0), (float)dvp.GetElement(1)), length);
                result.AddRange(circle.GenerateOutline(ForceWireframeThickness).Flatten().Select(p => new Polygon(p.Points.Span)));
            }

            return [..result];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3d CalcRotatedVector(in Vector3d angles)
        {
            return Matrix4x4d.CreateRotateZ(angles.Z + 180.0).RotateY(angles.Y).RotateX(angles.X).Transform(new Vector3d(0.0, 1.0, 0.0));
        }
    }

    record SimulatedShatterShapeData(ShatterTriangle[] Triangles, Matrix4x4d ModelMatrix)
    {
        public (Vector256<double>, Vector256<double>, Vector256<double>)[] GetTransformedTriangles()
        {
            return [.. Triangles.Select(t => (ModelMatrix.Transform(t.V1.Vertex), ModelMatrix.Transform(t.V2.Vertex), ModelMatrix.Transform(t.V3.Vertex)))];
        }
    }

    class ShatterShape
    {
        public ShatterTriangle[] Triangles { get; }

        public Vector3d Centroid { get; }

        public Vector3d RotateSpeed { get; set; }

        public Vector3d Speed { get; set; }

        public bool IsForceTouched { get; set; }

        Vector3d Position { get; set; }

        Vector3d Angles { get; set; }

        public ShatterShape((UVVertex, UVVertex, UVVertex)[] triangles)
        {
            Triangles = [..triangles.Select(t => new ShatterTriangle(t.Item1, t.Item2, t.Item3))];

            var triangleCentroids = triangles.Select(t => (t.Item1.Vertex + t.Item2.Vertex + t.Item3.Vertex) / 3.0);
            var triangleAreas = triangles.Select(t => (t.Item2.Vertex - t.Item1.Vertex).CrossProduct((t.Item3.Vertex - t.Item1.Vertex)).Length() * 0.5).ToArray();
            var totalArea = triangleAreas.Sum();

            Centroid = (Vector3d)(triangleCentroids.Zip(triangleAreas, (c, a) => c * a).Aggregate(Vector256<double>.Zero, (m, v) => m + v) / totalArea);
        }

        public void Advance(double time, double gravity, in Vector3d gravityDirection, double airRegistance)
        {
            if (!IsForceTouched)
            {
                return;
            }
            var newSpeed = Speed + gravity * gravityDirection * time;
            newSpeed -= newSpeed * airRegistance * time;

            Position += Speed * time;
            Angles += RotateSpeed * time;
            Speed = newSpeed;
            RotateSpeed -= RotateSpeed * airRegistance * time;
        }

        public SimulatedShatterShapeData ToSimulated()
        {
            return new SimulatedShatterShapeData(Triangles, Matrix4x4d.AffineTransform(Centroid, Vector3d.One, Angles, 0.0, 0.0, 0.0, Centroid + Position));
        }
    }

    class ShatterTriangle
    {
        public UVVertex V1 { get; }

        public UVVertex V2 { get; }

        public UVVertex V3 { get; }

        public Vector3d Centroid { get; }

        public double Area { get; }

        public ShatterTriangle(UVVertex v1, UVVertex v2, UVVertex v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Centroid = (Vector3d)((v1.Vertex + v2.Vertex + v3.Vertex) / 3.0);
            Area = (v2.Vertex - v1.Vertex).CrossProduct3D(v3.Vertex - v1.Vertex).Length() * 0.5;
        }
    }

    file static class ShatterTriangleExtension
    {
        public static void Deconstruct(this ShatterTriangle triangle, out UVVertex v1, out UVVertex v2, out UVVertex v3)
        {
            v1 = triangle.V1;
            v2 = triangle.V2;
            v3 = triangle.V3;
        }
    }

    enum ShatterShapeType
    {
        Triangle1,
        Triangle2,
        Rectangle,
        Hexagon,
        Brick,
        Rhombus,
        Random
    }

    enum ShatterDisplayType
    {
        Wireframe,
        WireframeWithForce,
        Rendering
    }
}

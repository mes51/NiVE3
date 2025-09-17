using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_SpotLight_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_SpotLight_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class SpotLight : IEffect
    {
        const string ID = "2F08C4FB-4A3D-4CCD-AA8D-8F08A91D3ED0";

        const string PropertyLightsId = nameof(PropertyLightsId);

        const string PropertyLightGroupId = nameof(PropertyLightGroupId);

        const string PropertyLightPositionId = nameof(PropertyLightPositionId);

        const string PropertyLightPointOfInterestId = nameof(PropertyLightPointOfInterestId);

        const string PropertyLightIntensityId = nameof(PropertyLightIntensityId);

        const string PropertyLightColorId = nameof(PropertyLightColorId);

        const string PropertyLightConeAngleId = nameof(PropertyLightConeAngleId);

        const string PropertyLightConeAttenuationId = nameof(PropertyLightConeAttenuationId);

        const string PropertyLightFalloffTypeId = nameof(PropertyLightFalloffTypeId);

        const string PropertyLightFalloffStartId = nameof(PropertyLightFalloffStartId);

        const string PropertyLightFalloffLengthId = nameof(PropertyLightFalloffLengthId);

        const string PropertyAmbientLightsId = nameof(PropertyAmbientLightsId);

        const string PropertyAmbientLightGroupId = nameof(PropertyAmbientLightGroupId);

        const string PropertyAmbientLightColorId = nameof(PropertyAmbientLightColorId);

        const string PropertyAmbientLightIntensityId = nameof(PropertyAmbientLightIntensityId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var dialogOk = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            var title = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            return
            [
                new AppendableProperty(PropertyLightsId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Lights,
                [
                    new AppendablePropertyItem(PropertyLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light, () => new PropertyGroup(PropertyLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light,
                    [
                        new Vector3dProperty(PropertyLightPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_Position, new Vector3d(sourceSize.Width * 0.5 - 100.0, sourceSize.Height * 0.25, -300.0), digit: 2, is3D: true, useInteraction: true),
                        new Vector3dProperty(PropertyLightPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                        new DoubleProperty(PropertyLightIntensityId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_Intensity, 100.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new ColorProperty(PropertyLightColorId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_Color, title, dialogOk, dialogCancel, Vector4.One),
                        new DoubleProperty(PropertyLightConeAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_ConeAngle, 90.0, 0.0, 180.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                        new DoubleProperty(PropertyLightConeAttenuationId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_ConeAttenuation, 50.0, 0.0, 100.0, digit: 2),
                        new EnumProperty(PropertyLightFalloffTypeId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_FalloffType, typeof(LightFalloffType), typeof(LanguageResourceDictionary), LightFalloffType.Linear, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLightFalloffStartId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_FalloffStart, 500.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyLightFalloffLengthId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_Light_FalloffLength, 500.0, 0.0, double.MaxValue, digit: 2)
                    ]))
                ], 0, true),
                new AppendableProperty(PropertyAmbientLightsId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_AmbientLights,
                [
                    new AppendablePropertyItem(PropertyAmbientLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_AmbientLight, () => new PropertyGroup(PropertyAmbientLightGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_AmbientLight,
                    [
                        new DoubleProperty(PropertyAmbientLightIntensityId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_AmbientLight_Intensity, 20.0, 0.0, double.MaxValue, digit: 2),
                        new ColorProperty(PropertyAmbientLightColorId, LanguageResourceDictionary.ResourceKeys.Stylize_SpotLight_AmbientLight_Color, title, dialogOk, dialogCancel, Vector4.One)
                    ]))
                ], 0, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var lights = (properties.First(p => p.Id == PropertyLightsId).GetChildren() ?? []).Where(p => p.IsEnable).ToArray();
            var ambients = (properties.First(p => p.Id == PropertyAmbientLightsId).GetChildren() ?? []).Where(p => p.IsEnable).ToArray();

            if (lights.Length < 1 && ambients.Length < 1)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, lights, ambients, layerTime, new Vector3d(downSamplingRateX, downSamplingRateY, downSamplingRateX));
            }
            else
            {
                return ProcessCpu(image, roi, lights, ambients, layerTime, new Vector3d(downSamplingRateX, downSamplingRateY, downSamplingRateX));
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, IPropertyObject[] lights, IPropertyObject[] ambients, Time layerTime, Vector3d downSamplingRate)
        {
            var managedImage = image.ToManaged();
            var imageWidth = managedImage.Width;

            var ambientColor = Vector4.Zero;
            foreach (var a in ambients)
            {
                var ambientProperties = a.GetChildren() ?? [];
                var intensity = (float)(ambientProperties.GetValue(PropertyAmbientLightIntensityId, layerTime, 0.0) * 0.01);
                ambientColor += ambientProperties.GetValue(PropertyAmbientLightColorId, layerTime, Vector4.Zero) * intensity;
            }

            var lightPowers = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);
            lightPowers.AsSpan(0, managedImage.DataLength).Fill(ambientColor);

            var offset = new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0);
            var size = Math.Max(managedImage.Width, managedImage.Height);
            foreach (var l in lights)
            {
                var lightProperties = l.GetChildren() ?? [];
                var coneRadian = (float)(lightProperties.GetValue(PropertyLightConeAngleId, layerTime, 0.0) / 180.0 * Math.PI);
                if (coneRadian <= 0.0F)
                {
                    continue;
                }

                var position = (Vector3)(lightProperties.GetValue(PropertyLightPositionId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
                var poi = (Vector3)(lightProperties.GetValue(PropertyLightPointOfInterestId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
                var intensity = (float)(lightProperties.GetValue(PropertyLightIntensityId, layerTime, 0.0) * 0.01);
                var color = lightProperties.GetValue(PropertyLightColorId, layerTime, Vector4.Zero) * intensity;
                var coneAttenuation = (float)(lightProperties.GetValue(PropertyLightConeAttenuationId, layerTime, 0.0) * 0.01);
                var falloffType = lightProperties.GetValue(PropertyLightFalloffTypeId, layerTime, LightFalloffType.None);
                var falloffStart = (float)(lightProperties.GetValue(PropertyLightFalloffStartId, layerTime, 0.0) / downSamplingRate.X / size);
                var falloffLength = (float)(lightProperties.GetValue(PropertyLightFalloffLengthId, layerTime, 0.0) / downSamplingRate.X / size);

                var direction = Vector3.Normalize(poi - position);
                var innerCone = coneRadian * (1.0F - coneAttenuation) * 0.5F;
                var outerCone = coneRadian * 0.5F;
                var outerConeCos = MathF.Cos(outerCone);
                var invertInnerConeCos = 1.0F / (MathF.Cos(innerCone) - MathF.Cos(outerCone));
                var coneAttenuationRate = outerCone * coneAttenuation;

                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var lightPowersSpan = lightPowers.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var lightDiff = new Vector3(x, y, 0.0F) - position;
                        var light = Vector3.Normalize(lightDiff);
                        var spotCone = MathF.Acos(Vector3.Dot(direction, light));

                        if (spotCone > outerCone)
                        {
                            continue;
                        }

                        var attenuation = 1.0F;
                        if (coneAttenuationRate > 0.0F)
                        {
                            attenuation = MathF.Cos((1.0F - Math.Min((MathF.Cos(spotCone) - outerConeCos) * invertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                        }

                        var falloff = CalcFalloff(lightDiff / size, falloffType, falloffStart, falloffLength);
                        var diffuseFactor = Math.Abs(Vector3.Dot(light, new Vector3(0.0F, 0.0F, -1.0F)));

                        lightPowersSpan[x] += color * diffuseFactor * falloff * attenuation;
                    }
                });
            }

            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var lightPowersSpan = lightPowers.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = lightPowersSpan[x];
                    color.W = 1.0F;
                    imageDataSpan[x] *= color;
                }
            });

            ArrayPool<Vector4>.Shared.Return(lightPowers);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, IPropertyObject[] lights, IPropertyObject[] ambients, Time layerTime, Vector3d downSamplingRate)
        {
            var gpuImage = image.ToGpu(device);

            var ambientColor = Vector4.Zero;
            foreach (var a in ambients)
            {
                var ambientProperties = a.GetChildren() ?? [];
                var intensity = (float)(ambientProperties.GetValue(PropertyAmbientLightIntensityId, layerTime, 0.0) * 0.01);
                ambientColor += ambientProperties.GetValue(PropertyAmbientLightColorId, layerTime, Vector4.Zero) * intensity;
            }

            var gpuLights = new SpotLightLightData[Math.Max(lights.Length, 1)];
            var offset = new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0);
            var size = Math.Max(gpuImage.Width, gpuImage.Height);
            for (var i = 0; i < lights.Length; i++)
            {
                var lightProperties = lights[i].GetChildren() ?? [];
                var coneRadian = (float)(lightProperties.GetValue(PropertyLightConeAngleId, layerTime, 0.0) / 180.0 * Math.PI);
                if (coneRadian <= 0.0F)
                {
                    continue;
                }

                var position = (Vector3)(lightProperties.GetValue(PropertyLightPositionId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
                var poi = (Vector3)(lightProperties.GetValue(PropertyLightPointOfInterestId, layerTime, Vector3d.Zero) / downSamplingRate + offset);
                var intensity = (float)(lightProperties.GetValue(PropertyLightIntensityId, layerTime, 0.0) * 0.01);
                var color = lightProperties.GetValue(PropertyLightColorId, layerTime, Vector4.Zero) * intensity;
                var coneAttenuation = (float)(lightProperties.GetValue(PropertyLightConeAttenuationId, layerTime, 0.0) * 0.01);
                var falloffType = lightProperties.GetValue(PropertyLightFalloffTypeId, layerTime, LightFalloffType.None);
                var falloffStart = (float)(lightProperties.GetValue(PropertyLightFalloffStartId, layerTime, 0.0) / downSamplingRate.X / size);
                var falloffLength = (float)(lightProperties.GetValue(PropertyLightFalloffLengthId, layerTime, 0.0) / downSamplingRate.X / size);

                var direction = Vector3.Normalize(poi - position);
                var innerCone = coneRadian * (1.0F - coneAttenuation) * 0.5F;
                var outerCone = coneRadian * 0.5F;
                var outerConeCos = MathF.Cos(outerCone);
                var invertInnerConeCos = 1.0F / (MathF.Cos(innerCone) - MathF.Cos(outerCone));
                var coneAttenuationRate = outerCone * coneAttenuation;

                gpuLights[i] = new SpotLightLightData(
                    true,
                    color,
                    position,
                    direction,
                    outerCone,
                    outerConeCos,
                    invertInnerConeCos,
                    coneAttenuationRate,
                    (int)falloffType,
                    falloffStart,
                    falloffLength
                );
            }

            using var lightBuffer = device.AllocateReadOnlyBuffer(gpuLights);

            device.For(roi.Width, roi.Height, new SpotLightProcess(gpuImage.Data, gpuImage.Width, size, ambientColor, lightBuffer, roi.Left, roi.Top));

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcFalloff(in Vector3 diff, LightFalloffType type, float falloffStart, float falloffLength)
        {
            var length = diff.Length();
            if (length <= falloffStart)
            {
                return 1.0F;
            }
            length -= falloffStart;
            return type switch
            {
                LightFalloffType.Linear => Math.Max((falloffLength - length) / falloffLength, 0.0F),
                LightFalloffType.Exponential => Math.Min(1.0F / MathF.Pow(1.0F + length, 2.0F), 1.0F),
                _ => 1.0F
            };
        }
    }

    readonly record struct SpotLightLightData(
        Bool IsEnable,
        Float4 Color,
        Float3 Position,
        Float3 Direction,
        float OuterCone,
        float OuterConeCos,
        float InvertInnerConeCos,
        float ConeAttenuationRate,
        int FalloffType,
        float FalloffStart,
        float FalloffLength
    )
    {
        public readonly Bool IsEnable = IsEnable;

        public readonly Float4 Color = Color;

        public readonly Float3 Position = Position;

        public readonly Float3 Direction = Direction;

        public readonly float OuterCone = OuterCone;

        public readonly float OuterConeCos = OuterConeCos;

        public readonly float InvertInnerConeCos = InvertInnerConeCos;

        public readonly float ConeAttenuationRate = ConeAttenuationRate;

        public readonly int FalloffType = FalloffType;

        public readonly float FalloffStart = FalloffStart;

        public readonly float FalloffLength = FalloffLength;
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SpotLightProcess(ReadWriteBuffer<Float4> image, int width, int size, Float4 ambientColor, ReadOnlyBuffer<SpotLightLightData> lights, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = new Float3(x, y, 0.0F);

            var lightPower = ambientColor;

            for (var i = 0; i < lights.Length; i++)
            {
                var currentLight = lights[i];
                if (!currentLight.IsEnable)
                {
                    continue;
                }

                var lightDiff = pos - currentLight.Position;
                var light = Hlsl.Normalize(lightDiff);
                var spotCone = Hlsl.Acos(Hlsl.Dot(currentLight.Direction, light));

                if (spotCone > currentLight.OuterCone)
                {
                    continue;
                }

                var attenuation = 1.0F;
                if (currentLight.ConeAttenuationRate > 0.0F)
                {
                    attenuation = Hlsl.Cos((1.0F - Hlsl.Min((Hlsl.Cos(spotCone) - currentLight.OuterConeCos) * currentLight.InvertInnerConeCos, 1.0F)) * MathF.PI * 0.5F);
                }

                var falloff = CalcFalloff(lightDiff / size, currentLight.FalloffType, currentLight.FalloffStart, currentLight.FalloffLength);
                var diffuseFactor = Hlsl.Abs(Hlsl.Dot(light, new Float3(0.0F, 0.0F, -1.0F)));

                lightPower += currentLight.Color * diffuseFactor * falloff * attenuation;
            }
            lightPower.W = 1.0F;

            image[y * width + x] *= lightPower;
        }

        static float CalcFalloff(Float3 diff, int type, float falloffStart, float falloffLength)
        {
            var length = Hlsl.Length(diff);
            if (length <= falloffStart)
            {
                return 1.0F;
            }
            length -= falloffStart;

            switch (type)
            {
                case 1: //LightFalloffType.Linear
                    return Hlsl.Max((falloffLength - length) / falloffLength, 0.0F);
                case 2: // LightFalloffType.Exponential
                    return Hlsl.Min(1.0F / Hlsl.Pow(1.0F + length, 2.0F), 1.0F);
                default:
                    return 1.0F;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Input;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using Prism.Mvvm;
using NiVE3.View.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.Property;
using System.ComponentModel;
using NiVE3.Shared.Extension;
using NiVE3.Extension;
using NiVE3.Plugin.Image;
using NiVE3.Input.Special;
using NiVE3.Plugin.Interfaces.RendererParams;
using System.Numerics;
using System.Runtime.Intrinsics;
using NiVE3.Data.Json.Project;
using NiVE3.Image.Drawing;
using NiVE3.Util;
using System.Buffers;
using System.Runtime.InteropServices;
using NiVE3.Plugin.Attributes;

namespace NiVE3.Model
{
    partial class LayerModel : BindableBase, IDisposable, ILayerObject
    {
        const string TransformGroupId = nameof(TransformGroupId);

        const string LayerOptionGroupId = nameof(LayerOptionGroupId);

        const string TextGroupId = nameof(TextGroupId);

        const string ShapeGroupId = nameof(ShapeGroupId);

        const string SourceOptionGroupId = nameof(SourceOptionGroupId);

        const string AudioOptionGroupId = nameof(AudioOptionGroupId);

        const double DefaultCameraFov = 0.360000466176267;// Math.Tan(39.5978 * 0.5 * (Math.PI / 180.0))

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string comment = "";
        public string Comment
        {
            get { return comment; }
            set { SetProperty(ref comment, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double sourceStartPoint;
        public double SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private double inPoint;
        public double InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private double outPoint;
        public double OutPoint
        {
            get { return outPoint; }
            set { SetProperty(ref outPoint, value); }
        }

        private bool isEnableTimeRemap;
        public bool IsEnableTimeRemap
        {
            get { return isEnableTimeRemap; }
            set { SetProperty(ref isEnableTimeRemap, value); }
        }

        private SourceType sourceType;
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private Color tagColor = Colors.Red;
        public Color TagColor
        {
            get { return tagColor; }
            set { SetProperty(ref tagColor, value); }
        }

        private bool isEnableVideo;
        public bool IsEnableVideo
        {
            get { return isEnableVideo; }
            set { SetProperty(ref isEnableVideo, value); }
        }

        private bool isEnableAudio;
        public bool IsEnableAudio
        {
            get { return isEnableAudio; }
            set { SetProperty(ref isEnableAudio, value); }
        }

        private bool isEnableSolo;
        public bool IsEnableSolo
        {
            get { return isEnableSolo; }
            set { SetProperty(ref isEnableSolo, value); }
        }

        private bool isLock;
        public bool IsLock
        {
            get { return isLock; }
            set { SetProperty(ref isLock, value); }
        }

        private bool isEnableShy;
        public bool IsEnableShy
        {
            get { return isEnableShy; }
            set { SetProperty(ref isEnableShy, value); }
        }

        private bool isEnableCollapse;
        public bool IsEnableCollapse
        {
            get { return isEnableCollapse; }
            set { SetProperty(ref isEnableCollapse, value); }
        }

        private bool isEnableEffect = true;
        public bool IsEnableEffect
        {
            get { return isEnableEffect; }
            set { SetProperty(ref isEnableEffect, value); }
        }

        private bool isEnableFrameBlend;
        public bool IsEnableFrameBlend
        {
            get { return isEnableFrameBlend; }
            set { SetProperty(ref isEnableFrameBlend, value); }
        }

        private bool isEnableMotionBlur;
        public bool IsEnableMotionBlur
        {
            get { return isEnableMotionBlur; }
            set { SetProperty(ref isEnableMotionBlur, value); }
        }

        private bool isEnableAdjustmentLayer;
        public bool IsEnableAdjustmentLayer
        {
            get { return isEnableAdjustmentLayer; }
            set { SetProperty(ref isEnableAdjustmentLayer, value); }
        }

        private bool isEnable3D;
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private ImageInterpolationQuality interpolationQuality = ImageInterpolationQuality.Level2;
        public ImageInterpolationQuality InterpolationQuality
        {
            get { return interpolationQuality; }
            set { SetProperty(ref interpolationQuality, value); }
        }

        private bool hasEffect;
        public bool HasEffect
        {
            get { return hasEffect; }
            set { SetProperty(ref hasEffect, value); }
        }

        private BlendMode blendMode = BlendMode.Normal;
        public BlendMode BlendMode
        {
            get { return blendMode; }
            set { SetProperty(ref blendMode, value); }
        }

        private Guid? trackMatteLayerId;
        public Guid? TrackMatteLayerId
        {
            get { return trackMatteLayerId; }
            set { SetProperty(ref trackMatteLayerId, value); }
        }

        private TrackMatteMode trackMatteMode = TrackMatteMode.Alpha;
        public TrackMatteMode TrackMatteMode
        {
            get { return trackMatteMode; }
            set { SetProperty(ref trackMatteMode, value); }
        }

        private Guid? parentLayerId;
        public Guid? ParentLayerId
        {
            get { return parentLayerId; }
            set { SetProperty(ref parentLayerId, value); }
        }

        public Guid LayerId { get; }

        public string SourceName => FootageModel.Name;

        public bool IsComposition => FootageModel.InputModel.Input is CompositionInput;

        public bool IsSpecial => FootageModel.InputModel.IsSpecial;

        public bool IsCamera => FootageModel.InputModel.Input is CameraInput;

        public bool IsLight => FootageModel.InputModel.Input is LightInput;

        public bool IsNullObject => FootageModel.InputModel.Input is NullObjectInput;

        public bool IsNotRenderable => IsCamera || IsLight || IsNullObject;

        public bool IsText => FootageModel.InputModel.Input is TextInput;

        public bool HasImage => SourceType.HasFlag(SourceType.Image) || SourceType.HasFlag(SourceType.Video);

        public bool HasAudio => SourceType.HasFlag(SourceType.Audio);

        private ObservableCollection<EffectModel> effects = [];
        public ObservableCollection<EffectModel> Effects
        {
            get { return effects; }
            set
            {
                if (effects!= value)
                {
                    effects.CollectionChanged -= Effects_CollectionChanged;
                    value.CollectionChanged += Effects_CollectionChanged;
                }
                SetProperty(ref effects, value);
            }
        }

        public FootageModel FootageModel { get; set; }

        public PropertyGroupModel? TransformProperties { get; }

        public PropertyGroupModel? LayerOptionProperties { get; }

        public PropertyGroupModel? TextProperties { get; }

        public PropertyGroupModel? ShapeProperties { get; }

        public PropertyGroupModel? SourceOptionProperties { get; }

        public PropertyGroupModel? AudioOptionProperties { get; }

        public event EventHandler<EventArgs>? LayerUpdated;

        EffectListModel EffectListModel { get; }

        CompositionModel CompositionModel { get; }

        HistoryModel HistoryModel { get; set; }

        double PrevInPoint { get; set; }

        double PrevOutPoint { get; set; }

        double PrevSourceStartPoint { get; set; }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel) : this(compositionModel, footageModel, effectListModel, historyModel, null) { }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, Guid? layerId)
        {
            Effects = [];
            FootageModel = footageModel;
            EffectListModel = effectListModel;
            CompositionModel = compositionModel;
            HistoryModel = historyModel;
            Name = footageModel.Name;
            Duration = footageModel.Duration;
            OutPoint = footageModel.Duration;
            SourceType = footageModel.InputType;
            LayerId = layerId ?? Guid.NewGuid();

            IsEnableVideo = SourceType.HasFlag(SourceType.Video) || SourceType.HasFlag(SourceType.Image);
            IsEnableAudio = SourceType.HasFlag(SourceType.Audio);

            switch (footageModel.InputModel.Input)
            {
                case NullObjectInput:
                    TransformProperties = new PropertyGroupModel(new PropertyGroup(TransformGroupId, LanguageResourceDictionary.ResourceKeys.Layer_Transform,
                    [
                        new Vector2DOr3DProperty(ILayerObject.TransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.TransformProperty_AnchorPoint, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), digit: 2),
                        new Vector2DOr3DProperty(ILayerObject.TransformPositionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Translate, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), digit: 2),
                        new Scale2DOr3DProperty(ILayerObject.TransformScaleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Scale, new Vector3d(100.0, 100.0, 100.0), digit: 2),
                        new Direction3DProperty(ILayerObject.TransformDirectionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Direction, new Vector3d(), digit: 2),
                        new Angle3DElementProperty(ILayerObject.TransformXAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_XAngle3D, 0.0, digit: 2),
                        new Angle3DElementProperty(ILayerObject.TransformYAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_YAngle3D, 0.0, digit: 2),
                        new ZAngleProperty(ILayerObject.TransformZAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle2D, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle3D, 0.0, digit: 2)
                    ]), compositionModel, this, historyModel);
                    LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Layer, []), compositionModel, this, historyModel);
                    break;
                case CameraInput:
                    var zoom = compositionModel.Width / DefaultCameraFov * 0.5;
                    IsEnableVideo = true;
                    TransformProperties = new PropertyGroupModel(new PropertyGroup(TransformGroupId, LanguageResourceDictionary.ResourceKeys.Layer_Transform,
                    [
                        new Vector3dProperty(ILayerObject.TransformPointOfInterestId, LanguageResourceDictionary.ResourceKeys.TransformProperty_CameraPointOfInterest, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), true, 2, true),
                        new Vector3dProperty(ILayerObject.TransformPositionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Translate, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, -zoom), true, 2, true),
                        new DirectionProperty(ILayerObject.TransformOrientationId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Direction, new Vector3d(), digit: 2),
                        new AngleProperty(ILayerObject.TransformXAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_XAngle3D, 0.0, digit: 2),
                        new AngleProperty(ILayerObject.TransformYAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_YAngle3D, 0.0, digit: 2),
                        new AngleProperty(ILayerObject.TransformZAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle3D, 0.0, digit: 2),
                    ]), compositionModel, this, historyModel);
                    LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Camera,
                    [
                        new DoubleProperty(ILayerObject.CameraLayerOptionZoomId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_CameraZoom, zoom, 0.01, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel)
                    ]), compositionModel, this, historyModel);
                    break;
                case LightInput:
                    IsEnableVideo = true;
                    var offset = compositionModel.Width / 24.0;
                    var zPos = compositionModel.Width / DefaultCameraFov * 0.125;
                    TransformProperties = new PropertyGroupModel(new PropertyGroup(TransformGroupId, LanguageResourceDictionary.ResourceKeys.Layer_Transform,
                    [
                        new Vector3dProperty(ILayerObject.TransformPointOfInterestId, LanguageResourceDictionary.ResourceKeys.TransformProperty_CameraPointOfInterest, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), true, 2, true),
                        new Vector3dProperty(ILayerObject.TransformPositionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Translate, new Vector3d(compositionModel.Width * 0.5 + offset, compositionModel.Height * 0.5 - offset, -zPos), true, 2, true),
                        new DirectionProperty(ILayerObject.TransformOrientationId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Direction, new Vector3d(), digit: 2),
                        new AngleProperty(ILayerObject.TransformXAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_XAngle3D, 0.0, digit: 2),
                        new AngleProperty(ILayerObject.TransformYAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_YAngle3D, 0.0, digit: 2),
                        new AngleProperty(ILayerObject.TransformZAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle3D, 0.0, digit: 2),
                    ]), compositionModel, this, historyModel);
                    LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Light,
                    [
                        new EnumProperty(ILayerObject.LightLayerOptionLightTypeId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_LightType, typeof(LightType), typeof(LanguageResourceDictionary), LightType.Spot, false),
                        new ColorProperty(
                            ILayerObject.LightLayerOptionColorId,
                            LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_Color,
                            LanguageResourceDictionary.ResourceKeys.ColorPickerDialog_Title,
                            LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                            LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                            Vector4.One
                        ),
                        new DoubleProperty(ILayerObject.LightLayerOptionIntensityId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_Intensity, 100.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(ILayerObject.LightLayerOptionConeAngleId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_ConeAngle, 90.0, 0.0, 180.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                        new DoubleProperty(ILayerObject.LightLayerOptionConeAttenuationId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_ConeAttenuation, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(ILayerObject.LightLayerOptionFalloffTypeId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_FalloffType, typeof(LightFalloffType), typeof(LanguageResourceDictionary), LightFalloffType.None, selectBoxWidth: 100.0),
                        new DoubleProperty(ILayerObject.LightLayerOptionFalloffStartId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_FalloffStart, 500.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(ILayerObject.LightLayerOptionFalloffLengthId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_FalloffLength, 500.0, 0.0, double.MaxValue, digit: 2),
                        new CheckBoxProperty(ILayerObject.LightLayerOptionEnableShadowId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_EnableShadow, true),
                        new DoubleProperty(ILayerObject.LightLayerOptionShadowStrengthId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_ShadowStrength, 100.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(ILayerObject.LightLayerOptionShadowScatterSizeId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_ShadowScatterSize, 0.0, 0.0, double.MaxValue, slideChangeValue: 0.1, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel)
                    ]), compositionModel, this, historyModel);
                    break;
                default:
                    if (footageModel.InputModel.Input is TextInput)
                    {
                        TextProperties = new PropertyGroupModel(new PropertyGroup(TextGroupId, LanguageResourceDictionary.ResourceKeys.Layer_TextOption, footageModel.GetOptionProperties()), compositionModel, this, historyModel);
                    }
                    else if (footageModel.InputModel.Input is ShapeInput)
                    {
                        ShapeProperties = new PropertyGroupModel(new PropertyGroup(ShapeGroupId, LanguageResourceDictionary.ResourceKeys.Layer_ShapeOption, footageModel.GetOptionProperties()), compositionModel, this, historyModel);
                    }
                    else if (footageModel.IsCustomizableFootageSource)
                    {
                        SourceOptionProperties = new PropertyGroupModel(new PropertyGroup(SourceOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_SourceOption, footageModel.GetOptionProperties()), compositionModel, this, historyModel);
                    }
                    if (footageModel.InputType.HasFlag(SourceType.Audio))
                    {
                        AudioOptionProperties = new PropertyGroupModel(new PropertyGroup(AudioOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_AudioOption,
                        [
                            new Vector3dProperty(
                                ILayerObject.AudioLevelId,
                                LanguageResourceDictionary.ResourceKeys.Layer_AudioOption_AudioLevel,
                                new Vector3d(),
                                new Vector3d(-192.0),
                                new Vector3d(24.0),
                                digit: 2,
                                unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Decibel,
                                separator: ",",
                                useLinkRatio: true
                            )
                        ]), compositionModel, this, historyModel);
                    }
                    if (footageModel.InputType.HasFlag(SourceType.Video) || footageModel.InputType.HasFlag(SourceType.Image))
                    {
                        TransformProperties = new PropertyGroupModel(new PropertyGroup(TransformGroupId, LanguageResourceDictionary.ResourceKeys.Layer_Transform,
                        [
                            new Vector2DOr3DProperty(ILayerObject.TransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.TransformProperty_AnchorPoint, new Vector3d(footageModel.Width * 0.5, footageModel.Height * 0.5, 0.0), digit: 2),
                            new Vector2DOr3DProperty(ILayerObject.TransformPositionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Translate, new Vector3d(compositionModel.Width * 0.5, compositionModel.Height * 0.5, 0.0), digit: 2),
                            new Scale2DOr3DProperty(ILayerObject.TransformScaleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Scale, new Vector3d(100.0, 100.0, 100.0), digit: 2),
                            new Direction3DProperty(ILayerObject.TransformDirectionId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Direction, new Vector3d(), digit: 2),
                            new Angle3DElementProperty(ILayerObject.TransformXAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_XAngle3D, 0.0, digit: 2),
                            new Angle3DElementProperty(ILayerObject.TransformYAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_YAngle3D, 0.0, digit: 2),
                            new ZAngleProperty(ILayerObject.TransformZAngleId, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle2D, LanguageResourceDictionary.ResourceKeys.TransformProperty_ZAngle3D, 0.0, digit: 2),
                            new DoubleProperty(ILayerObject.TransformPropertyOpacityId, LanguageResourceDictionary.ResourceKeys.TransformProperty_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                        ]), compositionModel, this, historyModel);
                        LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Layer,
                        [
                            new CheckBoxProperty(ILayerObject.ImageLayerOptionIsCastShadowId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_IsCastShadow, false),
                            new DoubleProperty(ILayerObject.ImageLayerOptionLightTransmissionId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_LightTransmission, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new CheckBoxProperty(ILayerObject.ImageLayerOptionIsAcceptShadowId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_IsAcceptShadow, true),
                            new CheckBoxProperty(ILayerObject.ImageLayerOptionIsAcceptLightId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_IsAcceptLight, true),
                            new DoubleProperty(ILayerObject.ImageLayerOptionAmbientId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_Ambient, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new DoubleProperty(ILayerObject.ImageLayerOptionDiffuseId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_Diffuse, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new DoubleProperty(ILayerObject.ImageLayerOptionSpecularIntensityId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_SpecularIntensity, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new DoubleProperty(ILayerObject.ImageLayerOptionSpecularShininessId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_SpecularShininess, 5.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new DoubleProperty(ILayerObject.ImageLayerOptionMetalId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_Metal, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        ]), compositionModel, this, historyModel);
                    }
                    break;
            }

            if (TransformProperties != null)
            {
                TransformProperties.ValueUpdated += Properties_ValueUpdated;
                TransformProperties.ValueCommited += Properties_ValueCommited;
            }
            if (LayerOptionProperties != null)
            {
                LayerOptionProperties.ValueUpdated += Properties_ValueUpdated;
                LayerOptionProperties.ValueCommited += Properties_ValueCommited;
            }
            if (TextProperties != null)
            {
                TextProperties.ValueUpdated += Properties_ValueUpdated;
                TextProperties.ValueCommited += Properties_ValueCommited;
            }
            if (ShapeProperties != null)
            {
                ShapeProperties.ValueUpdated += Properties_ValueUpdated;
                ShapeProperties.ValueCommited += Properties_ValueCommited;
            }
            if (SourceOptionProperties != null)
            {
                SourceOptionProperties.ValueUpdated += Properties_ValueUpdated;
                SourceOptionProperties.ValueCommited += Properties_ValueCommited;
            }
            if (AudioOptionProperties != null)
            {
                AudioOptionProperties.ValueUpdated += Properties_ValueUpdated;
                AudioOptionProperties.ValueCommited += Properties_ValueCommited;
            }
            PropertyChanged += LayerModel_PropertyChanged;
        }

        public RenderableImage? GetImage(double time, double downSamplingRate, bool withTrackMatte, bool useGpu)
        {
            if (!HasImage)
            {
                throw new InvalidOperationException("this source type is does not return images");
            }

            var transform = GetTransform(time);
            if ((double)(transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) <= 0.0)
            {
                return null;
            }

            var layerTime = time - SourceStartPoint;
            // TODO: タイムリマップ反映
            var sourceTime = layerTime;

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            var image = FootageModel.ReadImage(sourceTime, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
            var roi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);

            if (IsEnableEffect)
            {
                // TODO: モジュラーエフェクト&ROI反映
                foreach (var e in Effects.Where(e => !e.IsDummyEffect && e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType)))
                {
                    image = e.ProcessImage(image, roi, layerTime);
                }
            }

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.First(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer.GetImage(time, downSamplingRate, false, useGpu);
            }

            return new RenderableImage(
                image,
                roi,
                downSamplingRate,
                IsEnableMotionBlur,
                IsEnable3D,
                InterpolationQuality,
                BlendMode,
                transform,
                GetParentTransforms(time),
                LayerOptionProperties?.GetValues(layerTime),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public RenderableImage? GetRawImage(double time, double downSamplingRate, bool withTrackMatte, bool useGpu)
        {
            if (!HasImage)
            {
                throw new InvalidOperationException("this source type is does not return images");
            }

            var transform = GetTransform(time);
            if ((double)(transform[ILayerObject.TransformPropertyOpacityId] ?? 0.0) <= 0.0)
            {
                return null;
            }

            var layerTime = time - SourceStartPoint;
            // TODO: タイムリマップ反映
            var sourceTime = layerTime;

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            var image = FootageModel.ReadImage(sourceTime, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
            var roi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.First(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer.GetImage(time, downSamplingRate, false, useGpu);
            }

            return new RenderableImage(
                image,
                roi,
                downSamplingRate,
                IsEnableMotionBlur,
                IsEnable3D,
                InterpolationQuality,
                BlendMode,
                transform,
                GetParentTransforms(time),
                LayerOptionProperties?.GetValues(layerTime),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public (ROI, NImage) ProcessAdjustment(double time, double downSamplingRate, NImage currentFrame)
        {
            var layerTime = time - SourceStartPoint;
            var roi = new ROI(new Int32Point(), new Int32Size(currentFrame.Width, currentFrame.Height), 0, 0, currentFrame.Width, currentFrame.Height);

            if (IsEnableEffect)
            {
                // TODO: モジュラーエフェクト&ROI反映
                foreach (var e in Effects.Where(e => !e.IsDummyEffect && e.IsEnable))
                {
                    currentFrame = e.ProcessImage(currentFrame, roi, layerTime);
                }
            }

            return (roi, currentFrame);
        }

        public float[] GetAudio(double time, double length)
        {
            var layerTime = Math.Max(time - SourceStartPoint, InPoint);
            var audio = GetRawAudio(time, length);

            foreach (var effect in Effects.Where(e => !e.IsDummyEffect && e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType)))
            {
                audio = effect.ProcessAudio(audio, layerTime);
            }

            if (AudioOptionProperties != null && AudioOptionProperties.Children.First(p => p.PropertyId == ILayerObject.AudioLevelId) is PropertyModel level && (level.KeyFrames.Count > 0 || ((Vector3d)(level.Value ?? Vector3d.Zero)) != Vector3d.Zero))
            {
                var audioSpan = audio.AsSpan();
                if (level.KeyFrames.Count > 1)
                {
                    var lLevel = MathF.Pow(10.0F, (float)(((Vector3d)(level.KeyFrames.First().Value ?? Vector3d.Zero)).X * 0.05));
                    var rLevel = MathF.Pow(10.0F, (float)(((Vector3d)(level.KeyFrames.First().Value ?? Vector3d.Zero)).Y * 0.05));
                    var prevTime = level.KeyFrames.First().Time;
                    var lastTime = level.KeyFrames.Last().Time;
                    for (int i = 0, si = 0; i < audioSpan.Length; i += 2, si++)
                    {
                        var sampleTime = layerTime + Const.AudioSampleTime * si;
                        if (sampleTime < prevTime || sampleTime > lastTime)
                        {
                            audioSpan[i] = audioSpan[i] * lLevel;
                            audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                        }
                        else
                        {
                            var audioLevel = ((Vector3d)(level.GetValue(sampleTime) ?? Vector3d.Zero));
                            lLevel = MathF.Pow(10.0F, (float)(audioLevel.X * 0.05));
                            rLevel = MathF.Pow(10.0F, (float)(audioLevel.Y * 0.05));

                            audioSpan[i] = audioSpan[i] * lLevel;
                            audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                        }
                    }
                }
                else
                {
                    var audioLevel = ((Vector3d)(level.KeyFrames.FirstOrDefault()?.Value ?? level.Value ?? Vector3d.Zero));
                    var lLevel = MathF.Pow(10.0F, (float)(audioLevel.X * 0.05));
                    var rLevel = MathF.Pow(10.0F, (float)(audioLevel.Y * 0.05));

                    var i = 0;
                    if (Vector<float>.IsSupported)
                    {
                        var audioVectorSpan = MemoryMarshal.Cast<float, Vector<float>>(audioSpan[..((audioSpan.Length / Vector<float>.Count) * Vector<float>.Count)]);
                        var levelVector = new Vector<float>(Enumerable.Range(0, Vector<float>.Count / 2).SelectMany(_ => new float[] { lLevel, rLevel }).ToArray());
                        for (var vi = 0; vi < audioVectorSpan.Length; vi++)
                        {
                            audioVectorSpan[vi] = Vector.Multiply(audioVectorSpan[vi], levelVector);
                        }
                        i = audioVectorSpan.Length * Vector<float>.Count;
                    }
                    for (; i < audioSpan.Length; i += 2)
                    {
                        audioSpan[i] = audioSpan[i] * lLevel;
                        audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                    }
                }
            }

            return audio;
        }

        public float[] GetRawAudio(double time, double length)
        {
            var layerTime = Math.Max(time - SourceStartPoint, InPoint);
            var layerLength = Math.Min(length, OutPoint - layerTime);

            // TODO: タイムリマップ反映
            var sourceTime = Math.Max(layerTime, 0.0);
            var sourceLength = Math.Max(layerLength - (layerTime - sourceTime), 0.0);

            var result = new float[(int)(length * Const.AudioSamplingRate) * 2];
            var audio = FootageModel.ReadAudio(sourceTime, sourceLength);
            var startPos = (int)(Math.Max((InPoint + SourceStartPoint) - time, 0.0) * Const.AudioSamplingRate) * 2;
            audio.AsSpan(0, Math.Min(audio.Length, result.Length - startPos)).CopyTo(result.AsSpan(startPos));

            return result;
        }

        public bool IsContainsTime(double time)
        {
            var layerTime = time - SourceStartPoint;
            return layerTime >= inPoint && layerTime < OutPoint;
        }

        public bool IsContainsTimeRange(double begin, double end)
        {
            var layerBeginTime = begin - SourceStartPoint;
            var layerEndTime = end - SourceStartPoint;

            return (layerBeginTime < OutPoint && layerEndTime > InPoint);
        }

        public CameraSetting? GetCameraSetting(double time)
        {
            if (!IsCamera || !IsContainsTime(time))
            {
                return null;
            }

            var transform = GetTransform(time);
            var options = LayerOptionProperties?.GetValues(time - SourceStartPoint);

            return new CameraSetting(
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformOrientationId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0),
                (double)(options?[ILayerObject.CameraLayerOptionZoomId] ?? 0.0),
                GetParentTransforms(time)
            );
        }

        public LightSetting? GetLightSetting(double time)
        {
            if (!IsLight || !IsContainsTime(time))
            {
                return null;
            }

            var transform = GetTransform(time);
            var options = LayerOptionProperties?.GetValues(time - SourceStartPoint);
            if (options == null)
            {
                return null;
            }

            return new LightSetting(
                (LightType)(options[ILayerObject.LightLayerOptionLightTypeId] ?? LightType.Spot),
                (Vector3d)(transform[ILayerObject.TransformPointOfInterestId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformPositionId] ?? new Vector3d()),
                (Vector3d)(transform[ILayerObject.TransformDirectionId] ?? new Vector3d()),
                (double)(transform[ILayerObject.TransformXAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformYAngleId] ?? 0.0),
                (double)(transform[ILayerObject.TransformZAngleId] ?? 0.0),
                ((Vector4)(options[ILayerObject.LightLayerOptionColorId] ?? Vector4.One)).AsVector128().AsVector3(),
                (double)(options[ILayerObject.LightLayerOptionIntensityId] ?? 0.0),
                (double)(options[ILayerObject.LightLayerOptionConeAngleId] ?? 0.0),
                (double)(options[ILayerObject.LightLayerOptionConeAttenuationId] ?? 0.0),
                (LightFalloffType)(options[ILayerObject.LightLayerOptionFalloffTypeId] ?? LightFalloffType.None),
                (double)(options[ILayerObject.LightLayerOptionFalloffStartId] ?? 0.0),
                (double)(options[ILayerObject.LightLayerOptionFalloffLengthId] ?? 0.0),
                (bool)(options[ILayerObject.LightLayerOptionEnableShadowId] ?? false),
                (double)(options[ILayerObject.LightLayerOptionShadowStrengthId] ?? 0.0),
                (double)(options[ILayerObject.LightLayerOptionShadowScatterSizeId] ?? 0.0),
                GetParentTransforms(time)
            );
        }

        public PropertyValueGroup GetTransform(double time)
        {
            if (TransformProperties == null)
            {
                throw new InvalidOperationException();
            }
            var layerTime = time - SourceStartPoint;
            return TransformProperties.GetValues(layerTime);
        }

        public PropertyValueGroup? GetLayerOptions(double time)
        {
            var layerTime = time - SourceStartPoint;
            return LayerOptionProperties?.GetValues(layerTime);
        }

        public PropertyValueGroup? GetTextProperties(double time)
        {
            var layerTime = time - SourceStartPoint;
            return TextProperties?.GetValues(layerTime);
        }

        public ParentTransform[] GetParentTransforms(double time)
        {
            var parentTransforms = new List<ParentTransform>();
            var parentId = ParentLayerId;
            while (parentId != null)
            {
                var parent = CompositionModel.Layers.FirstOrDefault(l => l.LayerId == parentId.Value);
                if (parent == null)
                {
                    break;
                }

                if (parent.IsCamera)
                {
                    parentTransforms.Add(new ParentTransform(ParentType.Camera, parent.GetTransform(time)));
                }
                else if (parent.IsLight)
                {
                    var options = parent.GetLayerOptions(time);
                    if (options == null)
                    {
                        continue;
                    }
                    var parentType = ((LightType)(options[ILayerObject.LightLayerOptionLightTypeId] ?? LightType.Spot)) switch
                    {
                        LightType.Point => ParentType.PointLight,
                        LightType.Ambient => ParentType.AmbientLight,
                        _ => ParentType.SpotOrParallelLight
                    };
                    parentTransforms.Add(new ParentTransform(parentType, parent.GetTransform(time)));
                }
                else if (parent.IsNullObject)
                {
                    parentTransforms.Add(new ParentTransform(ParentType.NullObject, parent.GetTransform(time)));
                }
                else
                {
                    parentTransforms.Add(new ParentTransform(ParentType.Normal, parent.GetTransform(time)));
                }
                parentId = parent.ParentLayerId;
            }

            return [..parentTransforms];
        }

        public void BeginEditDuration()
        {
            PrevInPoint = InPoint;
            PrevOutPoint = OutPoint;
            PrevSourceStartPoint = SourceStartPoint;
        }

        public void CommitEditDuration()
        {
            if (PrevInPoint != inPoint || PrevOutPoint != OutPoint || PrevSourceStartPoint != SourceStartPoint)
            {
                HistoryModel.Add(new EditDurationHistoryCommand(this, PrevInPoint, PrevOutPoint, PrevSourceStartPoint, InPoint, OutPoint, SourceStartPoint));
            }
        }

        public void ChangeName(string name)
        {
            if (Name != name)
            {
                var prevName = Name;
                Name = name;
                HistoryModel.Add(new ChangeNameHistoryCommand(this, prevName, name));
            }
        }

        public void ChangeComment(string comment)
        {
            if (Comment != comment)
            {
                var prevComment = Comment;
                Comment = comment;
                HistoryModel.Add(new ChangeCommentHistoryCommand(this, prevComment, comment));
            }
        }

        public void InsertEffect(Guid[] effectUuids, int index)
        {
            var effectModels = effectUuids.Select(id => EffectListModel.CreateEffect(id, CompositionModel, this, HistoryModel)).OfType<EffectModel>().ToArray();

            var i = index;
            foreach (var e in effectModels)
            {
                Effects.Insert(i, e);
                i++;
            }

            HistoryModel.Add(new InsertEffectsHistoryCommand(this, effectModels, index));
        }

        public void MoveEffect(Guid effectId, int newIndex)
        {
            MoveEffects([effectId], effectId, newIndex);
        }

        public void MoveEffects(Guid[] effectIds, Guid referenceEffectId, int newIndex)
        {
            if (Effects.Count == effectIds.Length)
            {
                return;
            }

            var effects = Effects.Where(l => effectIds.Contains(l.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var prevIndices = effects.Select(l => Effects.IndexOf(l)).ToArray();
            var startIndex = newIndex - effects.IndexOf(l => l.EffectId == referenceEffectId);
            var newOrderedEffects = new List<EffectModel>(Effects.Count);
            newOrderedEffects.AddRange(Effects.Except(effects).Take(startIndex));
            newOrderedEffects.AddRange(effects);
            newOrderedEffects.AddRange(Effects.Except([..newOrderedEffects]));

            Effects.SortBy(l => newOrderedEffects.IndexOf(l));

            if (!prevIndices.SequenceEqual(effects.Select(l => Effects.IndexOf(l))))
            {
                // TODO: 古いインデックスを保持するのでは無く、古いEffectsを配列にしたものを渡す
                HistoryModel.Add(new MoveEffectsHistoryCommand(this, effects, prevIndices, [..newOrderedEffects]));
            }
        }

        public void ChangeEffectEnable(Guid[] effectIds, bool isEnable)
        {
            var effects = Effects.Where(e => effectIds.Contains(e.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldValues = effects.Select(e => e.IsEnable).ToArray();

            foreach (var e in effects)
            {
                e.IsEnable = isEnable;
            }

            HistoryModel.Add(new ChangeEffectEnableHistoryCommand(effects, oldValues, isEnable));
        }

        public void DeleteEffect(Guid[] effectIds)
        {
            var effects = Effects.Where(l => effectIds.Contains(l.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldIndices = effects.Select(Effects.IndexOf).ToArray();

            foreach (var e in effects)
            {
                Effects.Remove(e);
            }

            HistoryModel.Add(new DeleteEffectEnableHistoryCommand(this, effects, oldIndices));
        }

        public void ChangeTagColor(Color color)
        {
            if (TagColor != color)
            {
                var oldColor = TagColor;
                TagColor = color;

                HistoryModel.Add(new ChangeTagColorHistoryCommand(this, oldColor, color));
            }
        }

        public void ReplaceFootage(FootageModel footageModel)
        {
            FootageModel = footageModel;
        }

        public void UpdateTextProperty(string propertyId, object? value)
        {
            var property = TextProperties?.FindProperty(propertyId) as PropertyModel;
            property?.CommitProperty(value, property?.Value);
        }

        public LayerData SaveData()
        {
            return new LayerData
            {
                LayerId = LayerId,
                Name = Name,
                Comment = Comment,
                FootageId = FootageModel.FootageId,
                IsCamera = IsCamera,
                IsLight = IsLight,
                IsNullObject = IsNullObject,
                IsText = IsText,
                SourceStartPoint = SourceStartPoint,
                InPoint = InPoint,
                OutPoint = OutPoint,
                IsEnableTimeRemap = IsEnableTimeRemap,
                TagColor = TagColor,
                IsEnableVideo = IsEnableVideo,
                IsEnableAudio = IsEnableAudio,
                IsEnableSolo = IsEnableSolo,
                IsLock = IsLock,
                IsEnableShy = IsEnableShy,
                IsEnableExplodeLayers = IsEnableCollapse,
                IsEnableEffect = IsEnableEffect,
                IsEnableFrameBlend = IsEnableFrameBlend,
                IsEnableMotionBlur = IsEnableMotionBlur,
                IsEnableAdjustmentLayer = IsEnableAdjustmentLayer,
                IsEnable3D = IsEnable3D,
                InterpolationQuality = InterpolationQuality,
                BlendMode = BlendMode,
                TrackMatteLayerId = TrackMatteLayerId,
                TrackMatteMode = TrackMatteMode,
                ParentLayerId = ParentLayerId,
                Effects = Effects.Select(e => e.SaveData()).ToArray(),
                TransformProperties = TransformProperties?.SaveData(),
                LayerOptionProperties = LayerOptionProperties?.SaveData(),
                TextProperties = TextProperties?.SaveData(),
                ShapeProperties = ShapeProperties?.SaveData(),
                SourceOptionProperties = SourceOptionProperties?.SaveData(),
                AudioOptionProperties = AudioOptionProperties?.SaveData()
            };
        }

        public void LoadData(LayerData data)
        {
            Name = data.Name;
            Comment = data.Comment;
            SourceStartPoint = data.SourceStartPoint;
            InPoint = data.InPoint;
            OutPoint = data.OutPoint;
            IsEnableTimeRemap = data.IsEnableTimeRemap;
            TagColor = data.TagColor;
            IsEnableVideo = data.IsEnableVideo;
            IsEnableAudio = data.IsEnableAudio;
            IsEnableSolo = data.IsEnableSolo;
            IsLock = data.IsLock;
            IsEnableShy = data.IsEnableShy;
            IsEnableCollapse = data.IsEnableExplodeLayers;
            IsEnableEffect = data.IsEnableEffect;
            IsEnableFrameBlend = data.IsEnableFrameBlend;
            IsEnableMotionBlur = data.IsEnableMotionBlur;
            IsEnableAdjustmentLayer = data.IsEnableAdjustmentLayer;
            IsEnable3D = data.IsEnable3D;
            InterpolationQuality = data.InterpolationQuality;
            BlendMode = data.BlendMode;
            TrackMatteLayerId = data.TrackMatteLayerId;
            TrackMatteMode = data.TrackMatteMode;
            ParentLayerId = data.ParentLayerId;

            if (TransformProperties != null && data.TransformProperties != null)
            {
                TransformProperties.LoadData(data.TransformProperties);
            }
            if (LayerOptionProperties != null && data.LayerOptionProperties != null)
            {
                LayerOptionProperties.LoadData(data.LayerOptionProperties);
            }
            if (TextProperties != null && data.TextProperties != null)
            {
                TextProperties.LoadData(data.TextProperties);
            }
            if (ShapeProperties != null && data.ShapeProperties != null)
            {
                ShapeProperties.LoadData(data.ShapeProperties);
            }
            if (SourceOptionProperties != null && data.SourceOptionProperties != null)
            {
                SourceOptionProperties.LoadData(data.SourceOptionProperties);
            }
            if (AudioOptionProperties != null && data.AudioOptionProperties != null)
            {
                AudioOptionProperties.LoadData(data.AudioOptionProperties);
            }

            foreach (var effectData in data.Effects)
            {
                var effectModel = EffectListModel.CreateEffect(effectData.EffectPluginId, CompositionModel, this, HistoryModel, effectData.EffectId);
                if (effectModel == null)
                {
                    continue;
                }

                effectModel.LoadData(effectData);
                Effects.Add(effectModel);
            }
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasEffect = Effects.Count > 0;

            foreach (var oldEffect in (e.OldItems?.Cast<EffectModel>() ?? []))
            {
                oldEffect.EffectUpdated -= Effect_EffectUpdated;
            }
            foreach (var newEffect in (e.NewItems?.Cast<EffectModel>() ?? []))
            {
                newEffect.EffectUpdated += Effect_EffectUpdated;
            }

            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Properties_ValueUpdated(object? sender, EventArgs e)
        {
            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Properties_ValueCommited(object? sender, EventArgs e)
        {
            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void LayerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Effect_EffectUpdated(object? sender, EventArgs e)
        {
            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            foreach (var e in Effects)
            {
                e.Dispose();
            }
        }
    }

    file static class EffectSupportedSourceExtensions
    {
        public static bool IsSupportedSource(this EffectSupportedSource supported, SourceType sourceType)
        {
            return (supported.HasFlag(EffectSupportedSource.Image) && (sourceType.HasFlag(SourceType.Image) || sourceType.HasFlag(SourceType.Video))) ||
                (supported.HasFlag(EffectSupportedSource.Audio) && sourceType.HasFlag(SourceType.Audio));
        }
    }
}

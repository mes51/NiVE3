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
using NiVE3.Data.Clipboard;
using System.IO.Hashing;
using NiVE3.Cache;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using ComputeSharp;
using NiVE3.InternalShader;

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

        public bool IsImage => SourceType.HasFlag(SourceType.Image);

        public bool IsCustomizableFootageSource => FootageModel.IsCustomizableFootageSource;

        public Guid ParentCompositionId => CompositionModel.CompositionId;

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

        public PropertyGroupModel? TransformProperties { get; }

        public PropertyGroupModel? LayerOptionProperties { get; }

        public PropertyGroupModel? TextProperties { get; }

        public PropertyGroupModel? ShapeProperties { get; }

        public PropertyGroupModel? SourceOptionProperties { get; }

        public PropertyGroupModel? AudioOptionProperties { get; }

        public event EventHandler<EventArgs>? LayerUpdated;

        FootageModel FootageModel { get; set; }

        EffectListModel EffectListModel { get; }

        CompositionModel CompositionModel { get; }

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        double PrevInPoint { get; set; }

        double PrevOutPoint { get; set; }

        double PrevSourceStartPoint { get; set; }

        bool HasRenderEveryFrameEffect { get; set; }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, AcceleratorModel acceleratorModel) : this(compositionModel, footageModel, effectListModel, historyModel, acceleratorModel, null) { }

        public LayerModel(CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, AcceleratorModel acceleratorModel, Guid? layerId)
        {
            Effects = [];
            FootageModel = footageModel;
            EffectListModel = effectListModel;
            CompositionModel = compositionModel;
            HistoryModel = historyModel;
            AcceleratorModel = acceleratorModel;
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
                    var zoom = compositionModel.Width / Const.DefaultCameraFov * 0.5;
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
                    var zPos = compositionModel.Width / Const.DefaultCameraFov * 0.125;
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
                            new EnumProperty(ILayerObject.ImageLayerOptionIsCastShadowId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_IsCastShadow, typeof(ShadowCastMode), typeof(LanguageResourceDictionary), ShadowCastMode.None, selectBoxWidth: 100),
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

        public NImage GetRawImage(double layerTime, double downSamplingRate, bool useGpu)
        {
            var sourceTime = CalcSourceTime(layerTime);
            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);

            return FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
        }

        public NImage GetEffectedImage(double layerTime, double downSamplingRate, bool useGpu)
        {
            using var entry = CycleChecker.TryEnter(LayerId);
            if (entry == null)
            {
                return GetRawImage(layerTime, downSamplingRate, useGpu);
            }

            var sourceTime = CalcSourceTime(layerTime);
            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);

            NImage? image = null;
            var hash = new XxHash3();
            if (downSamplingRate == 1.0)
            {
                sourceOptionProperties?.CalcHash(hash);
                hash.Append(IsEnableTimeRemap);
                hash.Append(IsEnableEffect);
                hash.Append(IsEnableFrameBlend);
                hash.Append(InterpolationQuality);
                foreach (var e in Effects)
                {
                    if (e.IsEnable)
                    {
                        e.CalcPropertyHash(layerTime, hash);
                    }
                }

                if (SourceType.HasFlag(SourceType.Video) || HasRenderEveryFrameEffect)
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), layerTime, out var cachedImage))
                    {
                        (image, _) = cachedImage;
                    }
                }
                else
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), out var cachedImage))
                    {
                        (image, _) = cachedImage;
                    }
                }
            }

            if (image != null)
            {
                return image;
            }

            image = FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
            var roi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);

            var originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(sourceTime, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties) : new SourceFootageRect(Vector2d.Zero, image.Width, image.Height);
            var downSamplingRateX = originalImageSize.Width / (float)image.Width;
            var downSamplingRateY = originalImageSize.Height / (float)image.Height;
            if (IsEnableEffect)
            {
                // TODO: モジュラーエフェクト反映
                var (newRoi, expandedImage) = CalcAndExpandImage(image, downSamplingRateX, downSamplingRateY, layerTime);
                if (image != expandedImage)
                {
                    image.Dispose();
                    image = expandedImage;
                }
                roi = newRoi;

                var processedImage = ApplyEffect(image, roi, downSamplingRateX, downSamplingRateY, layerTime, useGpu);
                if (image != processedImage)
                {
                    image.Dispose();
                    image = processedImage;
                }

                if (downSamplingRate == 1.0)
                {
                    if (image is NGPUImage gpuImage)
                    {
                        using var managedImage = gpuImage.CopyToCpu();
                        ImageCache.Add(LayerId, hash.ToInt128(), layerTime, managedImage, roi);
                    }
                    else if (image is NManagedImage managedImage)
                    {
                        ImageCache.Add(LayerId, hash.ToInt128(), layerTime, managedImage, roi);
                    }
                }
            }

            return image;
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

            using var entry = CycleChecker.TryEnter(LayerId);
            if (entry == null)
            {
                return GetRawImage(time, downSamplingRate, withTrackMatte, useGpu);
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);

            NImage? image = null;
            ROI? roi = null;
            var downSamplingRateX = 1.0F;
            var downSamplingRateY = 1.0F;
            var hash = new XxHash3();

            if (downSamplingRate == 1.0)
            {
                sourceOptionProperties?.CalcHash(hash);
                hash.Append(IsEnableTimeRemap);
                hash.Append(IsEnableEffect);
                hash.Append(IsEnableFrameBlend);
                hash.Append(InterpolationQuality);
                foreach (var e in Effects)
                {
                    if (e.IsEnable)
                    {
                        e.CalcPropertyHash(layerTime, hash);
                    }
                }

                if (SourceType.HasFlag(SourceType.Video) || HasRenderEveryFrameEffect)
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), layerTime, out var cachedImage))
                    {
                        (image, roi) = cachedImage;
                    }
                }
                else
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), out var cachedImage))
                    {
                        (image, roi) = cachedImage;
                    }
                }
            }

            if (image == null || !roi.HasValue)
            {
                image = FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
                roi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);

                var originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(sourceTime, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties) : new SourceFootageRect(Vector2d.Zero, image.Width, image.Height);
                downSamplingRateX = originalImageSize.Width / (float)image.Width;
                downSamplingRateY = originalImageSize.Height / (float)image.Height;
                if (IsEnableEffect)
                {
                    // TODO: モジュラーエフェクトの反映
                    var (newRoi, expandedImage) = CalcAndExpandImage(image, downSamplingRateX, downSamplingRateY, layerTime);
                    if (expandedImage != image)
                    {
                        image.Dispose();
                        image = expandedImage;
                    }
                    roi = newRoi;

                    var processedImage = ApplyEffect(image, roi.Value, downSamplingRateX, downSamplingRateY, layerTime, useGpu);
                    if (image != processedImage)
                    {
                        image.Dispose();
                        image = processedImage;
                    }

                    roi = newRoi;
                }

                if (downSamplingRate == 1.0)
                {
                    if (image is NGPUImage gpuImage)
                    {
                        using var managedImage = gpuImage.CopyToCpu();
                        ImageCache.Add(LayerId, hash.ToInt128(), layerTime, managedImage, roi.Value);
                    }
                    else if (image is NManagedImage managedImage)
                    {
                        ImageCache.Add(LayerId, hash.ToInt128(), layerTime, managedImage, roi.Value);
                    }
                }
            }

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.FirstOrDefault(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer?.GetImage(time, downSamplingRate, false, useGpu);
            }

            return new RenderableImage(
                image,
                roi.Value,
                downSamplingRateX,
                downSamplingRateY,
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
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            var image = FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties, InterpolationQuality, useGpu);
            var originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties) : new SourceFootageRect(Vector2d.Zero, image.Width, image.Height);
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
                originalImageSize.Width / (float)image.Width,
                originalImageSize.Height / (float)image.Height,
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

        public RenderableImage GetSameImage(double time, double downSamplingRate, bool withTrackMatte, bool useGpu, RenderableImage baseImage)
        {
            var layerTime = time - SourceStartPoint;

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.First(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer.GetImage(time, downSamplingRate, false, useGpu);
            }

            return new RenderableImage(
                baseImage.Image,
                baseImage.ROI,
                baseImage.DownSampleRateX,
                baseImage.DownSampleRateY,
                IsEnableMotionBlur,
                IsEnable3D,
                InterpolationQuality,
                BlendMode,
                GetTransform(time),
                GetParentTransforms(time),
                LayerOptionProperties?.GetValues(layerTime),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public (ROI, NImage) ProcessAdjustment(double time, NImage currentFrame, double downSamplingRateX, double downSamplingRateY, bool useGpu)
        {
            var layerTime = time - SourceStartPoint;
            var roi = new ROI(new Int32Point(), new Int32Size(currentFrame.Width, currentFrame.Height), 0, 0, currentFrame.Width, currentFrame.Height);

            if (IsEnableEffect)
            {
                // TODO: モジュラーエフェクト反映
                var (newRoi, expandedImage) = CalcAndExpandImage(currentFrame, downSamplingRateX, downSamplingRateY, layerTime);
                if (expandedImage != currentFrame)
                {
                    currentFrame.Dispose();
                    currentFrame = expandedImage;
                }
                roi = newRoi;

                var processedImage = ApplyEffect(currentFrame, roi, downSamplingRateX, downSamplingRateY, layerTime, useGpu);
                if (currentFrame != processedImage)
                {
                    currentFrame.Dispose();
                    currentFrame = processedImage;
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

            if (AudioOptionProperties != null && AudioOptionProperties.Children.First(p => p.Property.Id == ILayerObject.AudioLevelId) is PropertyModel level && (level.KeyFrames.Count > 0 || ((Vector3d)(level.Value ?? Vector3d.Zero)) != Vector3d.Zero))
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

            var sourceTime = Math.Max(CalcSourceTime(layerTime), 0.0);
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

        public bool IsSameFootage(FootageModel footage)
        {
            return FootageModel == footage;
        }

        public bool IsSameFootage(LayerModel layerModel)
        {
            return IsSameFootage(layerModel.FootageModel);
        }

        public bool FootageIsPlaceholder(Guid footageId)
        {
            return FootageModel.IsPlaceholder && FootageModel.FootageId == footageId;
        }

        public CompositionModel? GetNestedComposition()
        {
            return (FootageModel.InputModel.Input as CompositionInput)?.Composition;
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

        public SourceFootageRect GetSourceFootageRect(double time)
        {
            if (!HasImage || !IsContainsTime(time))
            {
                return SourceFootageRect.Empty;
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            return FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties);
        }

        public LayerSkeleton? GetLayerSkeleton(double time)
        {
            if (!HasImage || !IsContainsTime(time))
            {
                return null;
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var transform = GetTransform(time);
            var parentTransforms = GetParentTransforms(time);
            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            var rect = FootageModel.CalcSize(sourceTime, CompositionModel.Width, CompositionModel.Height, sourceOptionProperties);

            return new LayerSkeleton(LayerId, rect, IsEnable3D, transform, parentTransforms);
        }

        public void CalcCacheKeyHash(XxHash3 hash, double time, bool withTrackMatte)
        {
            hash.Append(LayerId);

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime);
            sourceOptionProperties?.CalcHash(hash);
            hash.Append(SourceStartPoint);
            hash.Append(InPoint);
            hash.Append(OutPoint);
            hash.Append(IsEnableTimeRemap);
            hash.Append(IsEnableEffect);
            hash.Append(IsEnableFrameBlend);
            hash.Append(IsEnableMotionBlur);
            hash.Append(IsEnableAdjustmentLayer);
            hash.Append(IsEnable3D);
            hash.Append(InterpolationQuality);
            hash.Append(BlendMode);
            hash.Append(TrackMatteLayerId);
            hash.Append(TrackMatteMode);
            hash.Append(ParentLayerId);
            foreach (var e in Effects)
            {
                if (e.IsEnable)
                {
                    e.CalcPropertyHash(layerTime, hash);
                }
            }

            LayerOptionProperties?.GetValues(layerTime)?.CalcHash(hash);
            foreach (var pt in GetParentTransforms(time))
            {
                hash.Append(pt.ParentType);
                pt.Transform.CalcHash(hash);
            }

            if (TrackMatteLayerId != null)
            {
                CompositionModel.Layers.FirstOrDefault(l => l.LayerId == TrackMatteLayerId)?.CalcCacheKeyHash(hash, time, false);
            }

            GetTransform(time).CalcHash(hash);
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

        public void AddEffects(Guid[] effectUuids)
        {
            InsertEffect(effectUuids, Effects.Count);
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

        public void DeleteEffect(Guid[] ids)
        {
            DeleteEffectInternal(ids, false);
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

        public CopyData<EffectData> CutEffects(Guid[] ids)
        {
            var result = CopyEffects(ids);
            DeleteEffectInternal(ids, true);

            return result;
        }

        public CopyData<EffectData> CopyEffects(Guid[] ids)
        {
            var effects = Effects.Where(e => ids.Contains(e.EffectId)).OrderBy(Effects.IndexOf);
            return new CopyData<EffectData>(CopyDataType.Effect, [..effects.Select(e => e.SaveData())]);
        }

        public void PasteEffects(CopyData<EffectData> data, Guid[] selectedEffectIds, Guid? insertTargetId)
        {
            if (data.Type != CopyDataType.Effect || data.Data.Length < 1)
            {
                return;
            }

            if (selectedEffectIds.Length == 1 && Effects.FirstOrDefault(e => e.EffectId == selectedEffectIds[0]) is EffectModel targetEffect && targetEffect.EffectPluginId == data.Data[0].EffectPluginId)
            {
                targetEffect.OverwriteEffect(data.Data[0]);
            }
            else
            {
                PasteEffectsInternal(data, selectedEffectIds, insertTargetId, false);
            }
        }

        public void DuplicateEffects(Guid[] ids, Guid? insertTargetId)
        {
            var data = CopyEffects(ids);
            PasteEffectsInternal(data, [], insertTargetId, true);
        }

        public void UpdateCompositionDependProperties()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByCompositionStateChanged));

            LayerOptionProperties?.UpdateValueByCompositionStateChanged();
            SourceOptionProperties?.UpdateValueByCompositionStateChanged();
            foreach (var effect in Effects)
            {
                effect.UpdateCompositionDependProperties();
            }

            HistoryModel.EndGroup();
        }

        public void ClearCacheByLayerUpdated()
        {
            if ((LayerOptionProperties?.HasCompositionDependProperty() ?? false) ||
                (SourceOptionProperties?.HasCompositionDependProperty() ?? false) ||
                effects.Any(e => e.HasCompositionDependProperty()))
            {
                ImageCache.Clear(LayerId);
            }
        }

        public bool IsMotionBlurTarget()
        {
            return IsEnableMotionBlur &&
                ((TransformProperties?.HasKeyFrames() ?? false) ||
                (LayerOptionProperties?.HasKeyFrames() ?? false) ||
                (TextProperties?.HasKeyFrames() ?? false) ||
                (ShapeProperties?.HasKeyFrames() ?? false) ||
                (SourceOptionProperties?.HasKeyFrames() ?? false) ||
                Effects.Any(e => e.IsRenderEveryFrame || e.PropertyHasKeyFrame()));
        }

        void DeleteEffectInternal(Guid[] ids, bool isCut)
        {
            var effects = Effects.Where(l => ids.Contains(l.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldIndices = effects.Select(Effects.IndexOf).ToArray();

            foreach (var e in effects)
            {
                Effects.Remove(e);
            }

            HistoryModel.Add(new DeleteEffectHistoryCommand(this, effects, oldIndices, isCut));
        }

        void PasteEffectsInternal(CopyData<EffectData> data, Guid[] selectedEffectIds, Guid? insertTargetId, bool isDuplicate)
        {
            var addedEffect = new List<EffectModel>();
            var insertStartIndex = insertTargetId.HasValue ? Effects.IndexOf(e => e.EffectId == insertTargetId) : -1;
            if (insertStartIndex < 0)
            {
                insertStartIndex = Effects.Count;
            }
            else
            {
                insertStartIndex++;
            }

            var index = insertStartIndex;
            foreach (var effectData in data.Data)
            {
                var newEffect = EffectListModel.CreateEffect(effectData.EffectPluginId, CompositionModel, this, HistoryModel);
                if (newEffect == null)
                {
                    continue;
                }

                newEffect.LoadData(effectData);
                Effects.Insert(index, newEffect);
                addedEffect.Add(newEffect);
                index++;
            }

            HistoryModel.Add(new PasteNewEffectsHistoryCommand(this, [.. addedEffect], insertStartIndex, isDuplicate));
        }

        double CalcSourceTime(double layerTime)
        {
            // TODO: タイムリマップ反映
            return layerTime;
        }

        (ROI, NImage) CalcAndExpandImage(NImage image, double downSamplingRateX, double downSamplingRateY, double layerTime)
        {
            var newRoi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);
            foreach (var e in Effects.Where(e => e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType)))
            {
                newRoi = e.CalcRoi(newRoi, downSamplingRateX, downSamplingRateY, layerTime);
                var (left, right) = newRoi.Left > newRoi.Right ? (newRoi.Right, newRoi.Left) : (newRoi.Left, newRoi.Right);
                var (top, bottom) = newRoi.Top > newRoi.Bottom ? (newRoi.Bottom, newRoi.Top) : (newRoi.Top, newRoi.Bottom);
                newRoi = new ROI(newRoi.OriginalImagePosition, newRoi.OriginalImageSize, left, top, right, bottom);
            }

            if (newRoi.Left < 0 || newRoi.Top < 0 || newRoi.Right > image.Width || newRoi.Bottom > image.Height)
            {
                var expandLeft = Math.Max(-newRoi.Left, 0);
                var expandTop = Math.Max(-newRoi.Top, 0);
                var newSize = new Int32Size(Math.Max(newRoi.Right, image.Width) + expandLeft, Math.Max(newRoi.Bottom, image.Height) + expandTop);

                if (image is NGPUImage gpuImage)
                {
                    var newGpuImage = new NGPUImage(newSize.Width, newSize.Height, AcceleratorModel.CurrentDevice);
                    using (var context = AcceleratorModel.CurrentDevice.CreateComputeContext())
                    {
                        context.For(gpuImage.Width, gpuImage.Height, new CopyImage(gpuImage.Data, newGpuImage.Data, gpuImage.Width, newSize.Width, newSize.Height, expandLeft, expandTop));
                    }

                    image = newGpuImage;
                }
                else if (image is NManagedImage managedImage)
                {
                    var newManagedImage = new NManagedImage(newSize.Width, newSize.Height);
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        managedImage.Data.AsSpan(y * managedImage.Width, managedImage.Width).CopyTo(newManagedImage.Data.AsSpan((y + expandTop) * newSize.Width + expandLeft));
                    });

                    image = newManagedImage;
                }

                var newLeft = Math.Max(newRoi.Left, 0);
                var newTop = Math.Max(newRoi.Top, 0);
                newRoi = new ROI(newRoi.OriginalImagePosition + new Int32Point(expandLeft, expandTop), new Int32Size(image.Width, image.Height), newLeft, newTop, newRoi.Right + expandLeft, newRoi.Bottom + expandTop);
            }

            return (newRoi, image);
        }

        NImage ApplyEffect(NImage image, in ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, bool useGpu)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
            {
                return image;
            }

            var firstImage = image;
            foreach (var e in Effects.Where(e => !e.IsDummyEffect && e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType)))
            {
                var processedImage = e.ProcessImage(image, roi, downSamplingRateX, downSamplingRateY, layerTime, useGpu);
                if (processedImage != image && firstImage != image)
                {
                    image.Dispose();
                }
                image = processedImage;
            }

            return image;
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasEffect = Effects.Count > 0;
            HasRenderEveryFrameEffect = Effects.Any(e => e.IsRenderEveryFrame);

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
            if (e.PropertyName != nameof(IsLock) &&
                e.PropertyName != nameof(IsEnableShy))
            {
                LayerUpdated?.Invoke(this, EventArgs.Empty);
            }
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

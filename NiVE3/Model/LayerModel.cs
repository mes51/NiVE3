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
using ComputeSharp;
using NiVE3.InternalShader;
using NiVE3.Config;
using NAudio.Dsp;
using NiVE3.Mvvm;

namespace NiVE3.Model
{
    partial class LayerModel : WeakPropertyChangedBindingBase, IDisposable, ILayerObject, IFootageSourceUsingLayerObject
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

        private Time sourceDuration;
        public Time SourceDuration
        {
            get { return sourceDuration; }
            set { SetProperty(ref sourceDuration, value); }
        }

        private Time duration;
        public Time Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private Time sourceStartPoint;
        public Time SourceStartPoint
        {
            get { return sourceStartPoint; }
            set { SetProperty(ref sourceStartPoint, value); }
        }

        private Time inPoint;
        public Time InPoint
        {
            get { return inPoint; }
            set { SetProperty(ref inPoint, value); }
        }

        private Time outPoint;
        public Time OutPoint
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

        private bool isFreezeFrame;
        public bool IsFreezeFrame
        {
            get { return isFreezeFrame; }
            set { SetProperty(ref isFreezeFrame, value); }
        }

        private Time freezeFrameTime;
        public Time FreezeFrameTime
        {
            get { return freezeFrameTime; }
            set { SetProperty(ref freezeFrameTime, value); }
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

        private double playRate = 100.0;
        public double PlayRate
        {
            get { return playRate; }
            set { SetProperty(ref playRate, value); }
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

        private bool hasNonDummyEffect;
        public bool HasNonDummyEffect
        {
            get { return hasNonDummyEffect; }
            set { SetProperty(ref hasNonDummyEffect, value); }
        }

        private bool hasMask;
        public bool HasMask
        {
            get { return hasMask; }
            set { SetProperty(ref hasMask, value); }
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

        public bool IsDisableDuration => IsEnableTimeRemap || IsFreezeFrame;

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

        public bool IsVideo => SourceType.HasFlag(SourceType.Video);

        public bool IsCustomizableFootageSource => FootageModel.IsCustomizableFootageSource;

        public Guid ParentCompositionId => CompositionModel.CompositionId;

        public int SourceWidth => IsCustomizableFootageSource ? CompositionModel.Width : FootageModel.Width;

        public int SourceHeight => IsCustomizableFootageSource ? CompositionModel.Height : FootageModel.Height;

        public int FootageWidth => IsCustomizableFootageSource ? 0 : FootageModel.Width;

        public int FootageHeight => IsCustomizableFootageSource ? 0 : FootageModel.Height;

        public int Index => CompositionModel.Layers.IndexOf(this) + 1;

        public Guid FootageId => FootageModel.FootageId;

        private ObservableCollection<EffectModel> effects = [];
        public ObservableCollection<EffectModel> Effects
        {
            get { return effects; }
            set
            {
                effects.CollectionChanged -= Effects_CollectionChanged;
                value.CollectionChanged += Effects_CollectionChanged;

                SetProperty(ref effects, value);
            }
        }

        private ObservableCollection<MaskModel> masks = [];
        public ObservableCollection<MaskModel> Masks
        {
            get { return masks; }
            set
            {
                masks.CollectionChanged -= Masks_CollectionChanged;
                value.CollectionChanged += Masks_CollectionChanged;

                SetProperty(ref masks, value);
            }
        }

        public PropertyGroupModel? TransformProperties { get; }

        public PropertyGroupModel? LayerOptionProperties { get; }

        public PropertyGroupModel? TextProperties { get; }

        public PropertyGroupModel? ShapeProperties { get; }

        public PropertyGroupModel? SourceOptionProperties { get; }

        public PropertyGroupModel? AudioOptionProperties { get; }

        public IReadOnlyCollection<Guid> EffectIdentifiers => [..Effects.Select(e => e.EffectId)];

        public IReadOnlyCollection<Guid> MaskIdentifiers => [..Masks.Select(m => m.MaskId)];

        public event EventHandler<EventArgs>? LayerUpdated;

        FootageModel FootageModel { get; set; }

        EffectListModel EffectListModel { get; }

        ProjectModel ProjectModel { get; }

        CompositionModel CompositionModel { get; }

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        bool HasRenderEveryFrameEffect { get; set; }

        public LayerModel(ProjectModel projectModel, CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, AcceleratorModel acceleratorModel) : this(projectModel, compositionModel, footageModel, effectListModel, historyModel, acceleratorModel, null) { }

        public LayerModel(ProjectModel projectModel, CompositionModel compositionModel, FootageModel footageModel, EffectListModel effectListModel, HistoryModel historyModel, AcceleratorModel acceleratorModel, Guid? layerId)
        {
            Effects = [];
            Masks = [];
            FootageModel = footageModel;
            EffectListModel = effectListModel;
            ProjectModel = projectModel;
            CompositionModel = compositionModel;
            HistoryModel = historyModel;
            AcceleratorModel = acceleratorModel;
            Name = footageModel.Name;
            SourceDuration = footageModel.Duration;
            Duration = footageModel.Duration;
            OutPoint = footageModel.Duration;
            SourceType = footageModel.InputType;
            LayerId = layerId ?? Guid.NewGuid();

            IsEnableVideo = SourceType.HasFlag(SourceType.Video) || SourceType.HasFlag(SourceType.Image);
            IsEnableAudio = SourceType.HasFlag(SourceType.Audio);

            if (footageModel.InputModel.Input is ShapeInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultShapeLayerTag;
            }
            else if (footageModel.InputModel.Input is CameraInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultCameraLayerTag;
            }
            else if (footageModel.InputModel.Input is LightInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultLightLayerTag;
            }
            else if (footageModel.InputModel.Input is NullObjectInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultNullObjectLayerTag;
            }
            else if (footageModel.InputModel.Input is TextInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultTextLayerTag;
            }
            else if (footageModel.InputModel.Input is CompositionInput)
            {
                TagColor = ApplicationSetting.Setting.DefaultCompositionLayerTag;
            }
            else if (SourceType.HasFlag(SourceType.Video))
            {
                TagColor = ApplicationSetting.Setting.DefaultVideoLayerTag;
            }
            else if (SourceType.HasFlag(SourceType.Audio))
            {
                TagColor = ApplicationSetting.Setting.DefaultAudioLayerTag;
            }
            else
            {
                TagColor = ApplicationSetting.Setting.DefaultImageLayerTag;
            }

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
                    ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Layer, []), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                    ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    LayerOptionProperties = new PropertyGroupModel(new PropertyGroup(LayerOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_LayerOptions_Camera,
                    [
                        new DoubleProperty(ILayerObject.CameraLayerOptionZoomId, LanguageResourceDictionary.ResourceKeys.LayerOptionsProperty_CameraZoom, zoom, 0.01, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel)
                    ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                    ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                    ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    break;
                default:
                    if (footageModel.InputModel.Input is TextInput)
                    {
                        TextProperties = new PropertyGroupModel(new PropertyGroup(TextGroupId, LanguageResourceDictionary.ResourceKeys.Layer_TextOption, footageModel.GetOptionProperties()), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    }
                    else if (footageModel.InputModel.Input is ShapeInput)
                    {
                        ShapeProperties = new PropertyGroupModel(new PropertyGroup(ShapeGroupId, LanguageResourceDictionary.ResourceKeys.Layer_ShapeOption, footageModel.GetOptionProperties()), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    }
                    else if (footageModel.IsCustomizableFootageSource)
                    {
                        SourceOptionProperties = new PropertyGroupModel(new PropertyGroup(SourceOptionGroupId, LanguageResourceDictionary.ResourceKeys.Layer_SourceOption, footageModel.GetOptionProperties()), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                        ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                        ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
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
                        ]), LayerId.ToInt128(), projectModel, compositionModel, this, null, historyModel);
                    }
                    break;
            }

            FootageModel.FootageUpdated += FootageModel_FootageUpdated;

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

        public IEffectObject? GetEffect(Guid effectIdentifier)
        {
            return Effects.FirstOrDefault(e => e.EffectId == effectIdentifier);
        }

        public IMaskObject? GetMask(Guid maskIdentifier)
        {
            return Masks.FirstOrDefault(m => m.MaskId == maskIdentifier);
        }

        NImage? ILayerObject.GetRawImage(Time globalTime, double downSamplingRate, bool useGpu)
        {
            var sourceTime = CalcSourceTime(globalTime - SourceStartPoint);
            if (SourceType.HasFlag(SourceType.Video) && (sourceTime < Time.Zero || sourceTime > SourceDuration))
            {
                return null;
            }

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime, globalTime, true);

            return FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, sourceOptionProperties, InterpolationQuality, useGpu);
        }

        NImage? ILayerObject.GetMaskedImage(Time globalTime, double downSamplingRate, bool useGpu)
        {
            var layerTime = globalTime - SourceStartPoint;
            var sourceTime = CalcSourceTime(globalTime - SourceStartPoint);
            if (SourceType.HasFlag(SourceType.Video) && (sourceTime < Time.Zero || sourceTime > SourceDuration))
            {
                return null;
            }

            using var entry = CycleChecker.TryEnter(LayerId);
            if (entry == null)
            {
                return ((ILayerObject)this).GetRawImage(globalTime, downSamplingRate, useGpu);
            }

            var (image, originalImageSize, _) = GetFootageImage(globalTime, IsImage ? Time.Zero : new Time(1, FootageModel.FrameRate), downSamplingRate, useGpu, false);
            var downSamplingRateX = originalImageSize.Width / (float)image.Width;
            var downSamplingRateY = originalImageSize.Height / (float)image.Height;
            if (HasMask && Masks.Any(m => m.IsEnable))
            {
                var maskedImage = ApplyMask(image, downSamplingRateX, downSamplingRateY, layerTime, globalTime, useGpu);
                if (maskedImage != image)
                {
                    image.Dispose();
                    image = maskedImage;
                }
            }

            return image;
        }

        NImage? ILayerObject.GetEffectedImage(Time globalTime, double downSamplingRate, bool useGpu)
        {
            var layerTime = globalTime - SourceStartPoint;
            var sourceTime = CalcSourceTime(globalTime - SourceStartPoint);
            if (SourceType.HasFlag(SourceType.Video) && (sourceTime < Time.Zero || sourceTime > SourceDuration))
            {
                return null;
            }

            using var entry = CycleChecker.TryEnter(LayerId);
            if (entry == null)
            {
                return ((ILayerObject)this).GetRawImage(globalTime, downSamplingRate, useGpu);
            }

            NImage? image = null;
            var hash = new XxHash3();
            var device = useGpu ? AcceleratorModel.CurrentDevice : null;
            if (downSamplingRate == 1.0)
            {
                CalcCacheKeyHash(hash, globalTime, false, false);

                if ((IsVideo && !IsFreezeFrame) || HasRenderEveryFrameEffect)
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), layerTime, device, out var cachedImage))
                    {
                        (image, _) = cachedImage;
                    }
                }
                else
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), device, out var cachedImage))
                    {
                        (image, _) = cachedImage;
                    }
                }
            }

            if (image != null)
            {
                return image;
            }

            (image, var originalImageSize, var roi) = GetFootageImage(globalTime, IsImage ? Time.Zero : new Time(1, FootageModel.FrameRate), downSamplingRate, useGpu, false);
            var downSamplingRateX = originalImageSize.Width / (float)image.Width;
            var downSamplingRateY = originalImageSize.Height / (float)image.Height;
            if (HasMask && Masks.Any(m => m.IsEnable))
            {
                var maskedImage = ApplyMask(image, downSamplingRateX, downSamplingRateY, layerTime, globalTime, useGpu);
                if (maskedImage != image)
                {
                    image.Dispose();
                    image = maskedImage;
                }
            }
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
                    ImageCache.Add(LayerId, hash.ToInt128(), layerTime, image, roi);
                }
            }

            return image;
        }

        public RenderableImage? GetImage(Time time, Time frameTime, double downSamplingRate, bool withTrackMatte, bool useGpu, bool frameBlend)
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
                return GetRawImage(time, frameTime, downSamplingRate, withTrackMatte, useGpu, frameBlend);
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            NImage? image = null;
            ROI? roi = null;
            var downSamplingRateX = 1.0F;
            var downSamplingRateY = 1.0F;
            var hash = new XxHash3();

            if (downSamplingRate == 1.0)
            {
                CalcCacheKeyHash(hash, time, withTrackMatte, frameBlend);

                var device = useGpu ? AcceleratorModel.CurrentDevice : null;
                if ((IsVideo && !IsFreezeFrame) || HasRenderEveryFrameEffect)
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), layerTime, device, out var cachedImage))
                    {
                        (image, roi) = cachedImage;
                    }
                }
                else
                {
                    if (ImageCache.TryGet(LayerId, hash.ToInt128(), device, out var cachedImage))
                    {
                        (image, roi) = cachedImage;
                    }
                }
            }

            if (image == null || !roi.HasValue)
            {
                (image, var originalImageSize, roi) = GetFootageImage(time, frameTime, downSamplingRate, useGpu, frameBlend);

                downSamplingRateX = originalImageSize.Width / (float)image.Width;
                downSamplingRateY = originalImageSize.Height / (float)image.Height;
                if (HasMask && Masks.Any(m => m.IsEnable))
                {
                    var maskedImage = ApplyMask(image, downSamplingRateX, downSamplingRateY, layerTime, time, useGpu);
                    if (maskedImage != image)
                    {
                        image.Dispose();
                        image = maskedImage;
                    }
                }
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
                    ImageCache.Add(LayerId, hash.ToInt128(), layerTime, image, roi.Value);
                }
            }

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.FirstOrDefault(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer?.GetImage(time, frameTime, downSamplingRate, false, useGpu, frameBlend);
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
                LayerOptionProperties?.GetValues(layerTime, time),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public RenderableImage? GetRawImage(Time time, Time frameTime, double downSamplingRate, bool withTrackMatte, bool useGpu, bool frameBlend)
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

            var (image, originalImageSize, roi) = GetFootageImage(time, frameTime, downSamplingRate, useGpu, frameBlend);

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.First(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer.GetImage(time, frameTime, downSamplingRate, false, useGpu, frameBlend);
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
                LayerOptionProperties?.GetValues(layerTime, time),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public RenderableImage GetSameImage(Time time, Time frameTime, double downSamplingRate, bool withTrackMatte, bool useGpu, bool frameBlend, RenderableImage baseImage)
        {
            var layerTime = time - SourceStartPoint;

            RenderableImage? trackMatteImage = null;
            if (withTrackMatte && TrackMatteLayerId.HasValue)
            {
                var trackMatteLayer = CompositionModel.Layers.First(l => l.LayerId == TrackMatteLayerId);
                trackMatteImage = trackMatteLayer.GetImage(time, frameTime, downSamplingRate, false, useGpu, frameBlend);
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
                LayerOptionProperties?.GetValues(layerTime, time),
                trackMatteImage,
                TrackMatteLayerId.HasValue ? TrackMatteMode : null
            );
        }

        public (ROI, NImage) ProcessAdjustment(Time time, NImage currentFrame, double downSamplingRateX, double downSamplingRateY, bool useGpu)
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

        public float[] GetAudio(Time time, Time length)
        {
            var layerTime = Time.Max(time - SourceStartPoint, InPoint);
            var audio = GetRawAudio(time, length);

            foreach (var effect in Effects.Where(e => !e.IsDummyEffect && e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType.Audio)))
            {
                audio = effect.ProcessAudio(audio, layerTime);
            }

            if (AudioOptionProperties != null && AudioOptionProperties.Children.First(p => p.Property.Id == ILayerObject.AudioLevelId) is PropertyModel level)
            {
                var audioSpan = audio.AsSpan();
                if (level.IsEnableExpression)
                {
                    for (int i = 0, si = 0; i < audioSpan.Length; i += 2, si++)
                    {
                        var sampleTime = Const.AudioSampleTime * si;
                        var audioLevel = (Vector3d)(level.GetValue(sampleTime + layerTime, sampleTime + time) ?? Vector3d.Zero);
                        var lLevel = MathF.Pow(10.0F, (float)(audioLevel.X * 0.05));
                        var rLevel = MathF.Pow(10.0F, (float)(audioLevel.Y * 0.05));

                        audioSpan[i] = audioSpan[i] * lLevel;
                        audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                    }
                }
                else if (level.KeyFrames.Count > 1)
                {
                    var lLevel = MathF.Pow(10.0F, (float)(((Vector3d)(level.KeyFrames.First().Value ?? Vector3d.Zero)).X * 0.05));
                    var rLevel = MathF.Pow(10.0F, (float)(((Vector3d)(level.KeyFrames.First().Value ?? Vector3d.Zero)).Y * 0.05));
                    var prevTime = level.KeyFrames.First().Time;
                    var lastTime = level.KeyFrames.Last().Time;
                    for (int i = 0, si = 0; i < audioSpan.Length; i += 2, si++)
                    {
                        var sampledLayerTime = layerTime + Const.AudioSampleTime * si;
                        if (sampledLayerTime < prevTime || sampledLayerTime > lastTime)
                        {
                            audioSpan[i] = audioSpan[i] * lLevel;
                            audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                        }
                        else
                        {
                            var audioLevel = (Vector3d)(level.GetRawValue(sampledLayerTime) ?? Vector3d.Zero);
                            lLevel = MathF.Pow(10.0F, (float)(audioLevel.X * 0.05));
                            rLevel = MathF.Pow(10.0F, (float)(audioLevel.Y * 0.05));

                            audioSpan[i] = audioSpan[i] * lLevel;
                            audioSpan[i + 1] = audioSpan[i + 1] * rLevel;
                        }
                    }
                }
                else if (level.GetRawValue(layerTime) is Vector3d audioLevel && audioLevel != Vector3d.Zero)
                {
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

        public float[] GetRawAudio(Time time, Time length)
        {
            var layerTime = Time.Max(time - SourceStartPoint, InPoint);
            var layerLength = Time.Min(length, OutPoint - layerTime);

            var sourceBeginTime = Time.Max(CalcSourceTime(layerTime), Time.Zero);
            var sourceEndTime = CalcSourceTime(layerTime + layerLength);
            var reversed = sourceEndTime < sourceBeginTime;
            if (reversed)
            {
                (sourceBeginTime, sourceEndTime) = (sourceEndTime, sourceBeginTime);
            }
            var sourceLength = Time.Max(sourceEndTime - sourceBeginTime, Time.Zero);
            if ((int)(sourceLength * Const.AudioSamplingRate) < 1)
            {
                return [];
            }

            var audio = FootageModel.ReadAudio(sourceBeginTime, sourceLength);
            if (reversed)
            {
                for (int i = 0, limit = audio.Length / 2; i < limit; i += 2)
                {
                    var (tempL, tempR) = (audio[i], audio[i + 1]);
                    (audio[i], audio[i + 1]) = (audio[audio.Length - i - 2], audio[audio.Length - i - 1]);
                    (audio[audio.Length - i - 2], audio[audio.Length - i - 1]) = (tempL, tempR);
                }
            }
            if (PlayRate != 100.0)
            {
                var virtualSamplingRate = (int)(Const.AudioSamplingRate * Math.Abs(PlayRate) * 0.01);
                var resampler = new WdlResampler();
                resampler.SetMode(true, 2, true);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false);
                resampler.SetRates(virtualSamplingRate, Const.AudioSamplingRate);

                var requestFrameCount = audio.Length / Const.AudioChannelCount;
                var resamplerNeeded = resampler.ResamplePrepare(requestFrameCount, Const.AudioChannelCount, out var inBuffer, out var inBufferOffset) * Const.AudioChannelCount;
                if (resamplerNeeded > 0)
                {
                    audio.AsSpan(0, Math.Min(audio.Length, inBuffer.Length - inBufferOffset)).CopyTo(inBuffer.AsSpan(inBufferOffset));
                    var outCount = resampler.ResampleOut(audio, 0, resamplerNeeded, requestFrameCount, Const.AudioChannelCount) * Const.AudioChannelCount;

                    if (audio.Length > outCount)
                    {
                        var newBuffer = new float[outCount];
                        audio.AsSpan(0, outCount).CopyTo(newBuffer);
                        audio = newBuffer;
                    }
                }
            }

            var result = new float[(int)(length * Const.AudioSamplingRate) * Const.AudioChannelCount];
            var startPos = (int)((double)Time.Max((InPoint + SourceStartPoint) - time, Time.Zero) * Const.AudioSamplingRate) * Const.AudioChannelCount;
            var copyLength = Math.Min(audio.Length, result.Length - startPos);
            if (copyLength > 0)
            {
                audio.AsSpan(0, copyLength).CopyTo(result.AsSpan(startPos));
            }

            return result;
        }

        public bool IsContainsTime(Time time)
        {
            var layerTime = time - SourceStartPoint;
            return layerTime >= inPoint && layerTime < OutPoint;
        }

        public bool IsContainsTimeRange(Time begin, Time end)
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

        public CameraSetting? GetCameraSetting(Time time)
        {
            if (!IsCamera || !IsContainsTime(time))
            {
                return null;
            }

            var transform = GetTransform(time);
            var options = LayerOptionProperties?.GetValues(time - SourceStartPoint, time);

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

        public LightSetting? GetLightSetting(Time time)
        {
            if (!IsLight || !IsContainsTime(time))
            {
                return null;
            }

            var transform = GetTransform(time);
            var options = LayerOptionProperties?.GetValues(time - SourceStartPoint, time);
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

        public PropertyValueGroup GetTransform(Time time)
        {
            if (TransformProperties == null)
            {
                throw new InvalidOperationException();
            }
            var layerTime = time - SourceStartPoint;
            return TransformProperties.GetValues(layerTime, time);
        }

        public PropertyValueGroup? GetLayerOptions(Time time)
        {
            var layerTime = time - SourceStartPoint;
            return LayerOptionProperties?.GetValues(layerTime, time);
        }

        public PropertyValueGroup? GetTextProperties(Time time)
        {
            var layerTime = time - SourceStartPoint;
            return TextProperties?.GetValues(layerTime, time, true);
        }

        public ParentTransform[] GetParentTransforms(Time time)
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

        public SourceFootageRect GetSourceFootageRect(Time time, bool withInvisible)
        {
            if (!HasImage || !IsContainsTime(time))
            {
                return SourceFootageRect.Empty;
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime, time, true);
            return FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, withInvisible, this, sourceOptionProperties);
        }

        public LayerSkeleton? GetLayerSkeleton(Time time)
        {
            if (!HasImage || !IsContainsTime(time))
            {
                return null;
            }

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var transform = GetTransform(time);
            var parentTransforms = GetParentTransforms(time);
            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime, time, true);
            var rect = FootageModel.CalcSize(sourceTime, CompositionModel.Width, CompositionModel.Height, false, this, sourceOptionProperties);

            return new LayerSkeleton(LayerId, rect, IsEnable3D, transform, parentTransforms);
        }

        public bool EffectsIsSupported(Guid[] effectUuids)
        {
            return !IsSpecial || (IsNullObject && effectUuids.All(id => EffectListModel.GetMetadata(id)?.IsDummyEffect ?? false));
        }

        public void CalcCacheKeyHash(XxHash3 hash, Time time, bool withTrackMatte, bool frameBlend)
        {
            hash.Append(Name);
            hash.Append(Comment);
            hash.Append(frameBlend);
            hash.Append(LayerId);
            hash.Append(FootageModel.LastUpdated);

            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime, time, true);
            sourceOptionProperties?.CalcHash(hash);
            hash.Append(SourceStartPoint);
            hash.Append(InPoint);
            hash.Append(OutPoint);
            hash.Append(IsEnableTimeRemap);
            hash.Append(IsFreezeFrame);
            hash.Append(FreezeFrameTime);
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
            hash.Append(PlayRate);
            foreach (var e in Effects)
            {
                e.CalcPropertyHash(layerTime, time, hash);
            }
            foreach (var m in Masks)
            {
                m.CalcPropertyHash(layerTime, time, hash);
            }

            LayerOptionProperties?.GetValues(layerTime, time)?.CalcHash(hash);
            foreach (var pt in GetParentTransforms(time))
            {
                hash.Append(pt.ParentType);
                pt.Transform.CalcHash(hash);
            }

            if (withTrackMatte && TrackMatteLayerId != null)
            {
                CompositionModel.Layers.FirstOrDefault(l => l.LayerId == TrackMatteLayerId)?.CalcCacheKeyHash(hash, time, false, frameBlend);
            }

            GetTransform(time).CalcHash(hash);
        }

        public void CommitEditDuration(Time prevInPoint, Time inPoint, Time prevOutPoint, Time outPoint, Time prevSourceStartPoint, Time sourceStartPoint)
        {
            InPoint = inPoint;
            OutPoint = outPoint;
            SourceStartPoint = sourceStartPoint;
            HistoryModel.Add(new EditDurationHistoryCommand(this, prevInPoint, prevOutPoint, prevSourceStartPoint, inPoint, outPoint, sourceStartPoint));
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

        public void ChangePlayRate(double newRate, double compositionFrameRate)
        {
            var maxPlayRate = (double)Duration * compositionFrameRate * 100.0;
            newRate = Math.Clamp(newRate, -maxPlayRate, maxPlayRate);
            if (PlayRate == newRate)
            {
                return;
            }

            var oldPlayRate = PlayRate;
            var oldInPoint = InPoint;
            var oldOutPoint = OutPoint;
            PlayRate = newRate;
            Duration = Time.Abs(SourceDuration / (newRate * 0.01));
            InPoint = Time.Min(oldInPoint, Duration - CompositionModel.FrameDuration);
            OutPoint = Time.Min(oldOutPoint, Duration);

            HistoryModel.Add(new ChangePlayRateHistoryCommand(this, oldPlayRate, oldInPoint, oldOutPoint, newRate, InPoint, OutPoint));
        }

        public void AddEffects(Guid[] effectUuids)
        {
            InsertEffect(effectUuids, Effects.Count);
        }

        public void InsertEffect(Guid[] effectUuids, int index)
        {
            var effectModels = effectUuids.Select(id => EffectListModel.CreateEffect(id, ProjectModel, CompositionModel, this, HistoryModel)).OfType<EffectModel>().ToArray();

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
            var prevIndices = effects.Select(Effects.IndexOf).ToArray();
            var oldOrderedEffects = Effects.ToArray();
            var startIndex = newIndex - effects.FindIndex(l => l.EffectId == referenceEffectId);
            var newOrderedEffects = new List<EffectModel>(Effects.Count);
            newOrderedEffects.AddRange(Effects.Except(effects).Take(startIndex));
            newOrderedEffects.AddRange(effects);
            newOrderedEffects.AddRange(Effects.Except([..newOrderedEffects]));

            Effects.SortBy(newOrderedEffects.IndexOf);

            if (!prevIndices.SequenceEqual(effects.Select(Effects.IndexOf)))
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveEffects));

                foreach (var effect in Effects)
                {
                    effect.UpdateLayerDependProperties();
                }
                // TODO: TextProperties?.UpdateLayerDependProperties();
                HistoryModel.Add(new MoveEffectsHistoryCommand(this, oldOrderedEffects, [..newOrderedEffects]));

                HistoryModel.EndGroup();
            }
        }

        public void ChangeEffectsEnable(Guid[] effectIds, bool isEnable)
        {
            var effects = Effects.Where(e => effectIds.Contains(e.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldValues = effects.Select(e => e.IsEnable).ToArray();

            foreach (var e in effects)
            {
                e.IsEnable = isEnable;
            }

            HistoryModel.Add(new ChangeEffectsEnableHistoryCommand(effects, oldValues, isEnable));
        }

        public void DeleteEffect(Guid[] effectIds)
        {
            DeleteEffectInternal(effectIds, false);
        }

        public CopyData<EffectData> CutEffects(Guid[] effectIds)
        {
            var result = CopyEffects(effectIds);
            DeleteEffectInternal(effectIds, true);

            return result;
        }

        public CopyData<EffectData> CopyEffects(Guid[] effectIds)
        {
            var effects = Effects.Where(e => effectIds.Contains(e.EffectId)).OrderBy(Effects.IndexOf);
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

        public void DuplicateEffects(Guid[] effectIds, Guid? insertTargetId)
        {
            var data = CopyEffects(effectIds);
            PasteEffectsInternal(data, [], insertTargetId, true);
        }

        public void AddShapedMask(MaskShapeType shapeType)
        {
            InsertShapedMask(shapeType, Masks.Count);
        }

        public void AddBezierMask()
        {
            InsertBezierMask(Masks.Count);
        }

        public void InsertShapedMask(MaskShapeType shapeType, int index)
        {
            var maskModel = new MaskModel(ProjectModel, CompositionModel, this, AcceleratorModel, HistoryModel, false, shapeType);

            InsertMaskInternal(maskModel, index);
        }

        public void InsertBezierMask(int index)
        {
            var maskModel = new MaskModel(ProjectModel, CompositionModel, this, AcceleratorModel, HistoryModel, true);

            InsertMaskInternal(maskModel, index);
        }

        public void MoveMask(Guid maskId, int newIndex)
        {
            MoveMasks([maskId], maskId, newIndex);
        }

        public void MoveMasks(Guid[] maskIds, Guid referenceMaskId, int newIndex)
        {
            if (Masks.Count == maskIds.Length)
            {
                return;
            }

            var masks = Masks.Where(m => maskIds.Contains(m.MaskId)).OrderBy(Masks.IndexOf).ToArray();
            var prevIndices = masks.Select(Masks.IndexOf).ToArray();
            var oldOrderedMasks = Masks.ToArray();
            var startIndex = newIndex - masks.FindIndex(m => m.MaskId == referenceMaskId);
            var newOrderedMasks = new List<MaskModel>(Masks.Count);
            newOrderedMasks.AddRange(Masks.Except(masks).Take(startIndex));
            newOrderedMasks.AddRange(masks);
            newOrderedMasks.AddRange(Masks.Except([..newOrderedMasks]));

            Masks.SortBy(newOrderedMasks.IndexOf);

            if (!prevIndices.SequenceEqual(masks.Select(Masks.IndexOf)))
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveMasks));

                foreach (var effect in Effects)
                {
                    effect.UpdateLayerDependProperties();
                }
                // TODO: TextProperties?.UpdateLayerDependProperties();
                HistoryModel.Add(new MoveMasksHistoryCommand(this, oldOrderedMasks,[..newOrderedMasks]));

                HistoryModel.EndGroup();
            }
        }

        public void DeleteMask(Guid[] maskIds)
        {
            DeleteMaskInternal(maskIds, false);
        }

        public CopyData<MaskData> CutMasks(Guid[] maskIds)
        {
            var result = CopyMasks(maskIds);
            DeleteMaskInternal(maskIds, true);

            return result;
        }

        public CopyData<MaskData> CopyMasks(Guid[] maskIds)
        {
            var masks = Masks.Where(m => maskIds.Contains(m.MaskId)).OrderBy(Masks.IndexOf);
            return new CopyData<MaskData>(CopyDataType.Mask, [..masks.Select(m => m.SaveData())]);
        }

        public void PasteMasks(CopyData<MaskData> data, Guid[] selectedMaskIds, Guid? insertTargetId)
        {
            if (data.Type != CopyDataType.Mask || data.Data.Length < 1)
            {
                return;
            }

            if (selectedMaskIds.Length == 1 && Masks.FirstOrDefault(m => m.MaskId == selectedMaskIds[0]) is MaskModel targetMask)
            {
                targetMask.OverwriteMask(data.Data[0]);
            }
            else
            {
                PasteMasksInternal(data, selectedMaskIds, insertTargetId, false);
            }
        }

        public void DuplicateMasks(Guid[] maskIds, Guid? insertTargetId)
        {
            var data = CopyMasks(maskIds);
            PasteMasksInternal(data, [], insertTargetId, true);
        }

        public void ChangeMasksEnable(Guid[] maskIds, bool isEnable)
        {
            var masks = Masks.Where(m => maskIds.Contains(m.MaskId)).OrderBy(Masks.IndexOf).ToArray();
            var oldValues = masks.Select(m => m.IsEnable).ToArray();

            foreach (var m in masks)
            {
                m.IsEnable = isEnable;
            }

            HistoryModel.Add(new ChangeMasksEnableHistoryCommand(masks, oldValues, isEnable));
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
            FootageModel.FootageUpdated -= FootageModel_FootageUpdated;

            FootageModel = footageModel;

            FootageModel.FootageUpdated += FootageModel_FootageUpdated;
        }

        public void UpdateTextProperty(string propertyId, object? value, object? prevValue, Time layerTime)
        {
            var property = TextProperties?.FindProperty(propertyId) as PropertyModel;
            property?.CommitProperty(value, prevValue ?? property?.GetRawValue(layerTime));
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
                IsFreezeFrame = IsFreezeFrame,
                FreezeFrameTime = FreezeFrameTime,
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
                Effects = [..Effects.Select(e => e.SaveData())],
                Masks = [..Masks.Select(m => m.SaveData())],
                TransformProperties = TransformProperties?.SaveData(),
                LayerOptionProperties = LayerOptionProperties?.SaveData(),
                TextProperties = TextProperties?.SaveData(),
                ShapeProperties = ShapeProperties?.SaveData(),
                SourceOptionProperties = SourceOptionProperties?.SaveData(),
                AudioOptionProperties = AudioOptionProperties?.SaveData()
            };
        }

        public void LoadData(LayerData data, bool coerceProperties)
        {
            Name = data.Name;
            Comment = data.Comment;
            SourceStartPoint = data.SourceStartPoint;
            InPoint = data.InPoint;
            OutPoint = data.OutPoint;
            IsEnableTimeRemap = data.IsEnableTimeRemap;
            IsFreezeFrame = data.IsFreezeFrame;
            FreezeFrameTime = data.FreezeFrameTime;
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
                var effectModel = EffectListModel.CreateEffect(effectData.EffectPluginId, ProjectModel, CompositionModel, this, HistoryModel, effectData.EffectId);
                if (effectModel == null)
                {
                    continue;
                }

                effectModel.LoadData(effectData);
                Effects.Add(effectModel);
            }

            foreach (var maskData in data.Masks)
            {
                var maskModel = new MaskModel(ProjectModel, CompositionModel, this, AcceleratorModel, HistoryModel, maskData.IsBezierPath, maskData.DefaultShapeType, maskData.MaskId);
                maskModel.LoadData(maskData);
                Masks.Add(maskModel);
            }

            if (coerceProperties)
            {
                CoerceProperties();
            }
        }

        public void CoerceProperties()
        {
            TransformProperties?.CoerceValues();
            LayerOptionProperties?.CoerceValues();
            TextProperties?.CoerceValues();
            ShapeProperties?.CoerceValues();
            SourceOptionProperties?.CoerceValues();
            AudioOptionProperties?.CoerceValues();

            foreach (var effect in Effects)
            {
                effect.CoerceProperties();
            }
            foreach (var mask in Masks)
            {
                mask.CoerceProperties();
            }
        }

        public void ChangeFreezeFrame(bool isFreezeFrame, Time time, Time compositionFrameDuration)
        {
            var layerTime = time - SourceStartPoint;
            var newFreezeFrameTime = PlayRate >= 0.0 ? layerTime * PlayRate * 0.01 : SourceDuration + layerTime * PlayRate * 0.01;

            // NOTE: フレーム固定をしない場合は時間変更は受け付けない
            if (isFreezeFrame == IsFreezeFrame && (!isFreezeFrame || newFreezeFrameTime == FreezeFrameTime))
            {
                return;
            }

            var oldIsFreezeFrame = IsFreezeFrame;
            var oldFreezeFrameTime = FreezeFrameTime;
            var oldInPoint = InPoint;
            var oldOutPoint = OutPoint;
            IsFreezeFrame = isFreezeFrame;
            FreezeFrameTime = newFreezeFrameTime;
            if (!isFreezeFrame)
            {
                InPoint = Time.Min(Time.Max(InPoint, Time.Zero), Duration - compositionFrameDuration);
                OutPoint = Time.Min(Time.Max(OutPoint, InPoint + compositionFrameDuration), Duration);
            }

            HistoryModel.Add(new ChangeFreezeFrameHistoryCommand(this, oldIsFreezeFrame, oldFreezeFrameTime, oldInPoint, oldOutPoint, isFreezeFrame, newFreezeFrameTime, InPoint, OutPoint));
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

        public void UpdateLayerDependProperties()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByLayerStateChanged));

            LayerOptionProperties?.UpdateValueByCompositionStateChanged();
            SourceOptionProperties?.UpdateValueByCompositionStateChanged();
            foreach (var effect in Effects)
            {
                effect.UpdateLayerDependProperties();
            }
            TextProperties?.UpdateValueByLayerStateChanged();

            HistoryModel.EndGroup();
        }

        public void ReplaceLayerDependPropertiesEffectId(Dictionary<Guid, Guid> effectIdMap)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByLayerStateChanged));

            LayerOptionProperties?.UpdateValueByReplacedEffectId(effectIdMap);
            SourceOptionProperties?.UpdateValueByReplacedEffectId(effectIdMap);
            foreach (var effect in Effects)
            {
                effect.ReplaceLayerDependPropertiesEffectId(effectIdMap);
            }

            HistoryModel.EndGroup();
        }

        public void ReplaceLayerDependPropertiesMaskId(Dictionary<Guid, Guid> maskIdMap)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByLayerStateChanged));

            LayerOptionProperties?.UpdateValueByReplacedMaskId(maskIdMap);
            SourceOptionProperties?.UpdateValueByReplacedMaskId(maskIdMap);
            foreach (var effect in Effects)
            {
                effect.ReplaceLayerDependPropertiesMaskId(maskIdMap);
            }

            HistoryModel.EndGroup();
        }

        public void ReplaceCompositionDependPropertiesLayerId(Dictionary<Guid, Guid> layerIdMap)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByLayerStateChanged));

            LayerOptionProperties?.UpdateValueByReplacedLayerId(layerIdMap);
            SourceOptionProperties?.UpdateValueByReplacedLayerId(layerIdMap);
            foreach (var effect in Effects)
            {
                effect.ReplaceCompositionDependPropertiesLayerId(layerIdMap);
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
                ((TransformProperties?.IsChangeableByTime() ?? false) ||
                (LayerOptionProperties?.IsChangeableByTime() ?? false) ||
                (TextProperties?.IsChangeableByTime() ?? false) ||
                (ShapeProperties?.IsChangeableByTime() ?? false) ||
                (SourceOptionProperties?.IsChangeableByTime() ?? false) ||
                Effects.Any(e => e.IsRenderEveryFrame || e.PropertyIsChangeableByTime())) ||
                Masks.Any(m => m.PropertyIsChangeableByTime());
        }

        void DeleteEffectInternal(Guid[] effectIds, bool isCut)
        {
            var effects = Effects.Where(l => effectIds.Contains(l.EffectId)).OrderBy(Effects.IndexOf).ToArray();
            var oldIndices = effects.Select(Effects.IndexOf).ToArray();

            foreach (var e in effects)
            {
                Effects.Remove(e);
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(isCut ? LanguageResourceDictionary.History_CutEffects : LanguageResourceDictionary.History_RemoveEffects));

            foreach (var effect in Effects)
            {
                effect.UpdateLayerDependProperties();
            }
            TextProperties?.UpdateValueByLayerStateChanged();
            HistoryModel.Add(new DeleteEffectHistoryCommand(this, effects, oldIndices, isCut));

            HistoryModel.EndGroup();
        }

        void PasteEffectsInternal(CopyData<EffectData> data, Guid[] selectedEffectIds, Guid? insertTargetId, bool isDuplicate)
        {
            var addedEffects = new List<EffectModel>();
            var insertStartIndex = insertTargetId.HasValue ? Effects.FindIndex(e => e.EffectId == insertTargetId) : -1;
            if (insertStartIndex < 0)
            {
                insertStartIndex = Effects.Count;
            }
            else
            {
                insertStartIndex++;
            }

            var index = insertStartIndex;
            var newEffectId = new Dictionary<Guid, Guid>();
            foreach (var effectData in data.Data)
            {
                var newEffect = EffectListModel.CreateEffect(effectData.EffectPluginId, ProjectModel, CompositionModel, this, HistoryModel);
                if (newEffect == null)
                {
                    continue;
                }

                newEffect.LoadData(effectData);
                Effects.Insert(index, newEffect);
                addedEffects.Add(newEffect);
                index++;

                newEffectId.Add(effectData.EffectId, newEffect.EffectId);
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(isDuplicate ? LanguageResourceDictionary.History_DuplicateEffects : LanguageResourceDictionary.History_PasteEffects));

            foreach (var effect in addedEffects)
            {
                effect.ReplaceLayerDependPropertiesEffectId(newEffectId);
                effect.UpdateLayerDependProperties();
            }
            TextProperties?.UpdateValueByLayerStateChanged();
            HistoryModel.Add(new PasteNewEffectsHistoryCommand(this, [..addedEffects], insertStartIndex, isDuplicate));

            HistoryModel.EndGroup();
        }

        void InsertMaskInternal(MaskModel maskModel, int index)
        {
            var count = 1;
            var nameTemplate = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerModel_NewMaskTemplate);
            var name = string.Format(nameTemplate, count);
            while (Masks.Any(m => m.Name == name))
            {
                count++;
                name = string.Format(nameTemplate, count);
            }
            maskModel.Name = name;

            Masks.Insert(index, maskModel);

            HistoryModel.Add(new InsertMaskHistoryCommand(this, maskModel, index));
        }

        void DeleteMaskInternal(Guid[] maskIds, bool isCut)
        {
            var masks = Masks.Where(m => maskIds.Contains(m.MaskId)).OrderBy(Masks.IndexOf).ToArray();
            var oldIndices = masks.Select(Masks.IndexOf).ToArray();

            foreach (var m in masks)
            {
                Masks.Remove(m);
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(isCut ? LanguageResourceDictionary.History_CutMasks : LanguageResourceDictionary.History_RemoveMasks));

            foreach (var effect in Effects)
            {
                effect.UpdateLayerDependProperties();
            }

            TextProperties?.UpdateValueByLayerStateChanged();
            HistoryModel.Add(new DeleteMaskHistoryCommand(this, masks, oldIndices, isCut));

            HistoryModel.EndGroup();
        }

        void PasteMasksInternal(CopyData<MaskData> data, Guid[] selectedMaskIds, Guid? insertTargetId, bool isDuplicate)
        {
            // NOTE: マスクにLayerDependPropertyBaseを追加したら更新用の処理を追加する

            var addedMasks = new List<MaskModel>();
            var insertStartIndex = insertTargetId.HasValue ? Masks.FindIndex(m => m.MaskId == insertTargetId) : -1;
            if (insertStartIndex < 0)
            {
                insertStartIndex = Masks.Count;
            }
            else
            {
                insertStartIndex++;
            }

            var index = insertStartIndex;
            foreach (var maskData in data.Data)
            {
                var newMask = new MaskModel(ProjectModel, CompositionModel, this, AcceleratorModel, HistoryModel, maskData.IsBezierPath, maskData.DefaultShapeType);
                newMask.LoadData(maskData);
                Masks.Insert(index, newMask);
                addedMasks.Add(newMask);
                index++;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(isDuplicate ? LanguageResourceDictionary.History_DuplicateMasks : LanguageResourceDictionary.History_PasteMasks));

            TextProperties?.UpdateValueByLayerStateChanged();
            HistoryModel.Add(new PasteNewMasksHistoryCommand(this, [..addedMasks], insertStartIndex, isDuplicate));

            HistoryModel.EndGroup();
        }

        Time CalcSourceTime(Time layerTime)
        {
            // TODO: タイムリマップ使用時にそっち優先で反映
            if (IsFreezeFrame)
            {
                return FreezeFrameTime;
            }
            else if (PlayRate >= 0.0)
            {
                return layerTime * PlayRate * 0.01;
            }
            else
            {
                return SourceDuration + layerTime * PlayRate * 0.01;
            }
        }

        NImage ApplyMask(NImage image, double downSamplingRateX, double downSamplingRateY, Time layerTime, Time globalTime, bool useGpu)
        {
            var operativeMasks = Masks.Where(m => m.IsEnable && m.IsOperativeMask(layerTime, globalTime)).ToArray();
            if (operativeMasks.Length < 1)
            {
                return image;
            }

            var clearOpaque = operativeMasks[0].IsInverted(layerTime, globalTime);
            if (useGpu)
            {
                var device = AcceleratorModel.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                var gpuMaskImage = new GPURasterizedMaskImage(gpuImage.Width, gpuImage.Height, device, clearOpaque ? 1.0F : 0.0F)
                {
                    Origin = image.Origin
                };

                foreach (var mask in operativeMasks)
                {
                    var newMaskImage = mask.RenderMask(layerTime, globalTime, gpuMaskImage, InterpolationQuality, downSamplingRateX, downSamplingRateY, useGpu);
                    if (gpuMaskImage != newMaskImage)
                    {
                        gpuMaskImage.Dispose();
                        gpuMaskImage = newMaskImage.ToGpu(device);
                    }
                }

                device.For(gpuImage.Width, gpuImage.Height, new MaskImage(gpuImage.Data, gpuImage.Width, gpuMaskImage.Data, gpuMaskImage.Width, 0, 0));

                gpuMaskImage.Dispose();
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                var managedMaskImage = new ManagedRasterizedMaskImage(managedImage.Width, managedImage.Height, clearOpaque ? 1.0F : 0.0F)
                {
                    Origin = image.Origin
                };

                foreach (var mask in operativeMasks)
                {
                    var newMaskImage = mask.RenderMask(layerTime, globalTime, managedMaskImage, InterpolationQuality, downSamplingRateX, downSamplingRateY, useGpu);
                    if (managedMaskImage != newMaskImage)
                    {
                        managedMaskImage.Dispose();
                        managedMaskImage = newMaskImage.ToManaged();
                    }
                }

                var imageData = managedImage.Data;
                var imageWidth = managedImage.Width;
                var maskData = managedMaskImage.Data;
                Parallel.For(0, managedImage.Height, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var maskDataSpan = maskData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = 0; x < imageWidth; x++)
                    {
                        var color = imageDataSpan[x];
                        color.W *= maskDataSpan[x];
                        imageDataSpan[x] = color;
                    }
                });

                managedMaskImage.Dispose();
                return managedImage;
            }
        }

        (ROI, NImage) CalcAndExpandImage(NImage image, double downSamplingRateX, double downSamplingRateY, Time layerTime)
        {
            var newRoi = new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height);
            foreach (var e in Effects.Where(e => e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType.Image | SourceType.Video)))
            {
                newRoi = e.CalcRoi(newRoi, downSamplingRateX, downSamplingRateY, layerTime);
                var (left, right) = newRoi.Left > newRoi.Right ? (newRoi.Right, newRoi.Left) : (newRoi.Left, newRoi.Right);
                var (top, bottom) = newRoi.Top > newRoi.Bottom ? (newRoi.Bottom, newRoi.Top) : (newRoi.Top, newRoi.Bottom);
                newRoi = new ROI(newRoi.OriginalImagePosition, newRoi.OriginalImageSize, left, top, right, bottom);
            }

            if (newRoi.Left < 0 || newRoi.Top < 0 || newRoi.Right > image.Width || newRoi.Bottom > image.Height)
            {
                var originalSize = new Int32Size(image.Width, image.Height);
                var expandLeft = Math.Max(-newRoi.Left, 0);
                var expandTop = Math.Max(-newRoi.Top, 0);
                var newSize = new Int32Size(Math.Max(newRoi.Right, image.Width) + expandLeft, Math.Max(newRoi.Bottom, image.Height) + expandTop);

                if (image is NGPUImage gpuImage)
                {
                    var newGpuImage = new NGPUImage(newSize.Width, newSize.Height, AcceleratorModel.CurrentDevice)
                    {
                        Origin = gpuImage.Origin
                    };
                    using (var context = AcceleratorModel.CurrentDevice.CreateComputeContext())
                    {
                        context.For(gpuImage.Width, gpuImage.Height, new CopyImage(gpuImage.Data, newGpuImage.Data, gpuImage.Width, newSize.Width, newSize.Height, expandLeft, expandTop));
                    }

                    image = newGpuImage;
                }
                else if (image is NManagedImage managedImage)
                {
                    var newManagedImage = new NManagedImage(newSize.Width, newSize.Height)
                    {
                        Origin = managedImage.Origin
                    };
                    Parallel.For(0, managedImage.Height, y =>
                    {
                        managedImage.Data.AsSpan(y * managedImage.Width, managedImage.Width).CopyTo(newManagedImage.Data.AsSpan((y + expandTop) * newSize.Width + expandLeft));
                    });

                    image = newManagedImage;
                }

                var newLeft = Math.Max(newRoi.Left, 0);
                var newTop = Math.Max(newRoi.Top, 0);
                newRoi = new ROI(newRoi.OriginalImagePosition + new Int32Point(expandLeft, expandTop), originalSize, newLeft, newTop, newRoi.Right + expandLeft, newRoi.Bottom + expandTop);
            }

            return (newRoi, image);
        }

        NImage ApplyEffect(NImage image, in ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, bool useGpu)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
            {
                return image;
            }

            var firstImage = image;
            foreach (var e in Effects.Where(e => !e.IsDummyEffect && e.IsEnable && e.SupportedSource.IsSupportedSource(SourceType.Image | SourceType.Video)))
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

        (NImage, SourceFootageRect, ROI) GetFootageImage(Time time, Time frameTime, double downSamplingRate, bool useGpu, bool frameBlend)
        {
            var layerTime = time - SourceStartPoint;
            var sourceTime = CalcSourceTime(layerTime);

            NImage image;
            var originalImageSize = SourceFootageRect.Empty;

            var currentFrameDuration = FootageModel.FrameRate > 0.0 ? new Time(1, Math.Abs(FootageModel.FrameRate * PlayRate * 0.01)) : Time.Zero;
            // TODO: サイズ変更可能なビデオもフレームブレンドできるようにする?
            if (frameTime > 0.0 &&
                (frameTime - currentFrameDuration) != Time.Zero &&
                ((IsComposition && (GetNestedComposition()?.IsRetentionFrameRate ?? false)) || (IsVideo && !IsComposition)) &&
                !IsCustomizableFootageSource &&
                frameBlend && IsEnableFrameBlend)
            {
                var footageFrameDuration = 1.0 / FootageModel.FrameRate;
                if (frameTime < currentFrameDuration)
                {
                    var sourceCurrentFrameTime = sourceTime.FloorToFrameRate(FootageModel.FrameRate);
                    image = FootageModel.ReadImage(sourceCurrentFrameTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, null, InterpolationQuality, useGpu);
                    originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, false, this, null) : new SourceFootageRect(Vector2d.Zero, image.Width, image.Height);

                    var sourceNextFrameTime = (sourceCurrentFrameTime + footageFrameDuration).RoundToFrameRate(FootageModel.FrameRate);
                    var blendRate = (float)((sourceTime - sourceCurrentFrameTime) * FootageModel.FrameRate);
                    var nextFrameImage = FootageModel.ReadImage(sourceNextFrameTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, null, InterpolationQuality, useGpu);

                    var blendedImage = BlendFrame(image, nextFrameImage, blendRate, useGpu);
                    if (image != blendedImage)
                    {
                        image.Dispose();
                        image = blendedImage;
                    }
                    if (image != nextFrameImage)
                    {
                        nextFrameImage.Dispose();
                    }
                }
                else
                {
                    var startSoruceFrameTime = PlayRate > 0.0 ? sourceTime.FloorToFrameRate(FootageModel.FrameRate) : time.CeilingToFrameRate(FootageModel.FrameRate);
                    var resultRate = (float)(frameTime / currentFrameDuration);
                    var frameCount = (int)Math.Ceiling(resultRate);
                    var firstRate = 1.0 - (double)(Time.Abs(sourceTime - startSoruceFrameTime) * currentFrameDuration);
                    frameCount += (resultRate - Math.Truncate(resultRate)) > firstRate ? 1 : 0;

                    using (var firstFrame = FootageModel.ReadImage(startSoruceFrameTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, null, InterpolationQuality, useGpu))
                    {
                        originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, false, this, null) : new SourceFootageRect(Vector2d.Zero, firstFrame.Width, firstFrame.Height);
                        image = useGpu ? new NGPUImage(firstFrame.Width, firstFrame.Height, AcceleratorModel.CurrentDevice, Vector4.Zero) : new NManagedImage(firstFrame.Width, firstFrame.Height);
                        SumBlendFrame(image, firstFrame, (float)firstRate, useGpu);
                    }

                    var sign = Math.Sign(PlayRate);
                    for (int i = 1, limit = frameCount - 1; i < limit; i++)
                    {
                        using var frame = FootageModel.ReadImage(startSoruceFrameTime + footageFrameDuration * i * sign, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, null, InterpolationQuality, useGpu);
                        SumBlendFrame(image, frame, 1.0F, useGpu);
                    }

                    var lastFrameTime = startSoruceFrameTime + footageFrameDuration * frameCount * sign;
                    using var lastFrame = FootageModel.ReadImage(lastFrameTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, null, InterpolationQuality, useGpu);
                    SumBlendFrame(image, lastFrame, (float)(resultRate - firstRate - frameCount + 2), useGpu);

                    if (useGpu)
                    {
                        var gpuImage = (NGPUImage)image;

                        using var context = AcceleratorModel.CurrentDevice.CreateComputeContext();
                        context.For(gpuImage.Width, gpuImage.Height, new SumBlendResult(gpuImage.Data, gpuImage.Width, resultRate));
                    }
                    else
                    {
                        var managedImage = (NManagedImage)image;
                        var imageWidth = managedImage.Width;
                        var imageData = managedImage.Data;

                        Parallel.For(0, managedImage.Height, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = 0; x < imageWidth; x++)
                            {
                                imageDataSpan[x] /= resultRate;
                            }
                        });
                    }
                }
            }
            else
            {
                var sourceOptionProperties = (TextProperties ?? ShapeProperties ?? SourceOptionProperties)?.GetValues(sourceTime, time, true);
                image = FootageModel.ReadImage(sourceTime, downSamplingRate, CompositionModel.Width, CompositionModel.Height, this, sourceOptionProperties, InterpolationQuality, useGpu);
                originalImageSize = downSamplingRate != 1.0 ? FootageModel.CalcSize(time, CompositionModel.Width, CompositionModel.Height, false, this, sourceOptionProperties) : new SourceFootageRect(Vector2d.Zero, image.Width, image.Height);
            }

            return (image, originalImageSize, new ROI(new Int32Point(), new Int32Size(image.Width, image.Height), 0, 0, image.Width, image.Height));
        }

        NImage BlendFrame(NImage baseImage, NImage blendTargetImage, float blendRate, bool useGpu)
        {
            if (blendRate <= 0.0F)
            {
                return baseImage;
            }
            else if (blendRate >= 1.0F)
            {
                return blendTargetImage;
            }

            if (useGpu)
            {
                var device = AcceleratorModel.CurrentDevice;
                var baseGpuImage = baseImage.ToGpu(device);
                var targetGpuImage = blendTargetImage.ToGpu(device);

                using (var context = device.CreateComputeContext())
                {
                    context.For(baseGpuImage.Width, baseGpuImage.Height, new BlendTwoFrame(baseGpuImage.Data, targetGpuImage.Data, baseGpuImage.Width, blendRate));
                }

                if (targetGpuImage != blendTargetImage)
                {
                    targetGpuImage.Dispose();
                }

                return baseGpuImage;
            }
            else
            {
                var baseManagedImage = baseImage.ToManaged();
                var targetManagedImage = blendTargetImage.ToManaged();

                var baseImageData = baseManagedImage.Data;
                var targetImageData = targetManagedImage.Data;
                var width = baseManagedImage.Width;
                var vectoredLength = width - (width % Vector<float>.Count);
                var remainLength = width - vectoredLength;
                var iBlendRate = 1.0F - blendRate;
                Parallel.For(0, baseManagedImage.Height, y =>
                {
                    var baseImageDataSpan = baseImageData.AsSpan(y * width, width);
                    var targetImageDataSpan = targetImageData.AsSpan(y * width, width);

                    var vectorBaseImageDataSpan = MemoryMarshal.Cast<Vector4, Vector<float>>(baseImageDataSpan[..vectoredLength]);
                    var vectorTargetImageDataSpan = MemoryMarshal.Cast<Vector4, Vector<float>>(targetImageDataSpan[..vectoredLength]);
                    for (var i = 0; i < vectorBaseImageDataSpan.Length; i++)
                    {
                        vectorBaseImageDataSpan[i] = vectorBaseImageDataSpan[i] * iBlendRate + vectorTargetImageDataSpan[i] * blendRate;
                    }
                    for (int i = vectoredLength, c = 0; c < remainLength; i++, c++)
                    {
                        baseImageDataSpan[i] = Vector4.Lerp(baseImageDataSpan[i], targetImageDataSpan[i], blendRate);
                    }
                });

                if (targetManagedImage != blendTargetImage)
                {
                    targetManagedImage.Dispose();
                }

                return baseManagedImage;
            }
        }

        void SumBlendFrame(NImage baseImage, NImage addFrame, float rate, bool useGpu)
        {
            if (useGpu)
            {
                var device = AcceleratorModel.CurrentDevice;
                var gpuAddFrame = addFrame.ToGpu(device);
                var gpuImage = (NGPUImage)baseImage;

                using (var context = device.CreateComputeContext())
                {
                    context.For(gpuImage.Width, gpuImage.Height, new SumBlendFrame(gpuImage.Data, gpuAddFrame.Data, gpuImage.Width, rate));
                }

                if (gpuAddFrame != addFrame)
                {
                    gpuAddFrame.Dispose();
                }
            }
            else
            {
                var managedAddFrame = addFrame.ToManaged();
                var addFrameData = managedAddFrame.Data;
                var managedImage = (NManagedImage)baseImage;
                var imageWidth = managedImage.Width;
                var imageData = managedImage.Data;
                
                Parallel.For(0, managedImage.Height, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var addFrameDataSpan = addFrameData.AsSpan(y * imageWidth, imageWidth);
                    for (var x = 0; x < imageWidth; x++)
                    {
                        imageDataSpan[x] += addFrameDataSpan[x] * rate;
                    }
                });

                if (addFrame != managedAddFrame)
                {
                    managedAddFrame.Dispose();
                }
            }
        }

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasEffect = Effects.Count > 0;
            HasNonDummyEffect = Effects.Any(e => !e.IsDummyEffect);
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

        private void Masks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HasMask = Masks.Count > 0;

            foreach (var oldMask in (e.OldItems?.Cast<MaskModel>() ?? []))
            {
                oldMask.MaskUpdated -= Mask_MaskUpdated;
            }
            foreach (var newMask in (e.NewItems?.Cast<MaskModel>() ?? []))
            {
                newMask.MaskUpdated += Mask_MaskUpdated;
            }

            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void FootageModel_FootageUpdated(object? sender, NeedHistoryChangeEventArgs e)
        {
            if (IsComposition)
            {
                SourceDuration = FootageModel.Duration;
                Duration = Time.Abs(SourceDuration / (PlayRate * 0.01));

                if (!IsFreezeFrame && !IsEnableTimeRemap)
                {
                    var oldInPoint = InPoint;
                    var oldOutPoint = OutPoint;
                    var newInPoint = Time.Min(oldInPoint, Duration - CompositionModel.FrameDuration);
                    var newOutPoint = Time.Min(oldOutPoint, Duration);

                    if (e.NeedHistoryChange)
                    {
                        CommitEditDuration(oldInPoint, newInPoint, oldOutPoint, newOutPoint, SourceStartPoint, SourceStartPoint);
                    }
                    else
                    {
                        InPoint = newInPoint;
                        OutPoint = newOutPoint;
                    }
                }
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
            switch (e.PropertyName)
            {
                case nameof(PlayRate):
                case nameof(SourceDuration):
                    Duration = Time.Abs(SourceDuration / (PlayRate * 0.01));
                    break;
                case nameof(IsEnableTimeRemap):
                case nameof(IsFreezeFrame):
                    RaisePropertyChanged(nameof(IsDisableDuration));
                    break;
            }

            var hasUseExpressionProperty = (TransformProperties?.ClearExpressionError() ?? false) ||
                (LayerOptionProperties?.ClearExpressionError() ?? false) ||
                (TextProperties?.ClearExpressionError() ?? false) ||
                (ShapeProperties?.ClearExpressionError() ?? false) ||
                (SourceOptionProperties?.ClearExpressionError() ?? false) ||
                (AudioOptionProperties?.ClearExpressionError() ?? false);
            foreach (var effect in Effects)
            {
                hasUseExpressionProperty |= effect.ClearExpressionError();
            }
            foreach (var mask in Masks)
            {
                hasUseExpressionProperty |= mask.ClearExpressionError();
            }

            if (hasUseExpressionProperty || (e.PropertyName != nameof(IsLock) && e.PropertyName != nameof(IsEnableShy)))
            {
                LayerUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Effect_EffectUpdated(object? sender, EventArgs e)
        {
            LayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Mask_MaskUpdated(object? sender, EventArgs e)
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

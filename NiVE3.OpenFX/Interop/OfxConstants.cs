using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Interop
{
    /// <summary>
    /// OpenFX API のステータスコード
    /// </summary>
    public enum OfxStatus
    {
        OK = 0,
        Failed = 1,
        ErrFatal = 2,
        ErrUnknown = 3,
        ErrMissingHostFeature = 4,
        ErrUnsupported = 5,
        ErrExists = 6,
        ErrFormat = 7,
        ErrMemory = 8,
        ErrBadHandle = 9,
        ErrBadIndex = 10,
        ErrValue = 11,
        ReplyYes = 12,
        ReplyNo = 13,
        ReplyDefault = 14
    }

    /// <summary>
    /// OpenFX API の文字列定数 (ofxCore.h / ofxImageEffect.h / ofxParam.h 相当)
    /// </summary>
    public static class OfxNames
    {
        // API
        public const string ImageEffectPluginApi = "OfxImageEffectPluginAPI";

        // Suites
        public const string PropertySuite = "OfxPropertySuite";
        public const string ImageEffectSuite = "OfxImageEffectSuite";
        public const string ParameterSuite = "OfxParameterSuite";
        public const string MemorySuite = "OfxMemorySuite";
        public const string MultiThreadSuite = "OfxMultiThreadSuite";
        public const string MessageSuite = "OfxMessageSuite";
        public const string InteractSuite = "OfxInteractSuite";
        public const string ProgressSuite = "OfxProgressSuite";
        public const string TimeLineSuite = "OfxTimeLineSuite";
        public const string ParametricParameterSuite = "OfxParametricParameterSuite";
        public const string OpenGLRenderSuite = "OfxImageEffectOpenGLRenderSuite";
        public const string DialogSuite = "OfxDialogSuite";

        // Actions
        public const string ActionLoad = "OfxActionLoad";
        public const string ActionUnload = "OfxActionUnload";
        public const string ActionDescribe = "OfxActionDescribe";
        public const string ActionCreateInstance = "OfxActionCreateInstance";
        public const string ActionDestroyInstance = "OfxActionDestroyInstance";
        public const string ActionInstanceChanged = "OfxActionInstanceChanged";
        public const string ActionBeginInstanceChanged = "OfxActionBeginInstanceChanged";
        public const string ActionEndInstanceChanged = "OfxActionEndInstanceChanged";
        public const string ActionPurgeCaches = "OfxActionPurgeCaches";
        public const string ActionSyncPrivateData = "OfxActionSyncPrivateData";
        public const string ActionBeginInstanceEdit = "OfxActionBeginInstanceEdit";
        public const string ActionEndInstanceEdit = "OfxActionEndInstanceEdit";
        public const string ImageEffectActionDescribeInContext = "OfxImageEffectActionDescribeInContext";
        public const string ImageEffectActionGetRegionOfDefinition = "OfxImageEffectActionGetRegionOfDefinition";
        public const string ImageEffectActionGetRegionsOfInterest = "OfxImageEffectActionGetRegionsOfInterest";
        public const string ImageEffectActionGetTimeDomain = "OfxImageEffectActionGetTimeDomain";
        public const string ImageEffectActionGetFramesNeeded = "OfxImageEffectActionGetFramesNeeded";
        public const string ImageEffectActionGetClipPreferences = "OfxImageEffectActionGetClipPreferences";
        public const string ImageEffectActionIsIdentity = "OfxImageEffectActionIsIdentity";
        public const string ImageEffectActionRender = "OfxImageEffectActionRender";
        public const string ImageEffectActionBeginSequenceRender = "OfxImageEffectActionBeginSequenceRender";
        public const string ImageEffectActionEndSequenceRender = "OfxImageEffectActionEndSequenceRender";

        // Contexts
        public const string ContextGenerator = "OfxImageEffectContextGenerator";
        public const string ContextFilter = "OfxImageEffectContextFilter";
        public const string ContextTransition = "OfxImageEffectContextTransition";
        public const string ContextPaint = "OfxImageEffectContextPaint";
        public const string ContextGeneral = "OfxImageEffectContextGeneral";
        public const string ContextRetimer = "OfxImageEffectContextRetimer";

        // Components / Depths
        public const string ComponentNone = "OfxImageComponentNone";
        public const string ComponentRGBA = "OfxImageComponentRGBA";
        public const string ComponentRGB = "OfxImageComponentRGB";
        public const string ComponentAlpha = "OfxImageComponentAlpha";
        public const string BitDepthNone = "OfxBitDepthNone";
        public const string BitDepthByte = "OfxBitDepthByte";
        public const string BitDepthShort = "OfxBitDepthShort";
        public const string BitDepthFloat = "OfxBitDepthFloat";

        // 汎用プロパティ
        public const string PropName = "OfxPropName";
        public const string PropLabel = "OfxPropLabel";
        public const string PropShortLabel = "OfxPropShortLabel";
        public const string PropLongLabel = "OfxPropLongLabel";
        public const string PropVersion = "OfxPropVersion";
        public const string PropVersionLabel = "OfxPropVersionLabel";
        public const string PropAPIVersion = "OfxPropAPIVersion";
        public const string PropType = "OfxPropType";
        public const string PropInstanceData = "OfxPropInstanceData";
        public const string PropChangeReason = "OfxPropChangeReason";
        public const string PropEffectInstance = "OfxPropEffectInstance";
        public const string PropHostOSHandle = "OfxPropHostOSHandle";
        public const string PropPluginDescription = "OfxPropPluginDescription";
        public const string PropTime = "OfxPropTime";
        public const string PropIsInteractive = "OfxPropIsInteractive";

        // ホストプロパティ (ImageEffect)
        public const string ImageEffectHostPropIsBackground = "OfxImageEffectHostPropIsBackground";
        public const string ImageEffectHostPropNativeOrigin = "OfxImageEffectHostPropNativeOrigin";
        public const string ImageEffectPropSupportsOverlays = "OfxImageEffectPropSupportsOverlays";
        public const string ImageEffectPropSupportsMultiResolution = "OfxImageEffectPropSupportsMultiResolution";
        public const string ImageEffectPropSupportsTiles = "OfxImageEffectPropSupportsTiles";
        public const string ImageEffectPropTemporalClipAccess = "OfxImageEffectPropTemporalClipAccess";
        public const string ImageEffectPropSupportedComponents = "OfxImageEffectPropSupportedComponents";
        public const string ImageEffectPropSupportedContexts = "OfxImageEffectPropSupportedContexts";
        public const string ImageEffectPropSupportedPixelDepths = "OfxImageEffectPropSupportedPixelDepths";
        // 注意: ヘッダの定数名は kOfxImageEffectPropSupportsMultipleClipDepths だが、文字列リテラルには "Supports" が入らない (ofxImageEffect.h の歴史的な非一貫性)
        public const string ImageEffectPropSupportsMultipleClipDepths = "OfxImageEffectPropMultipleClipDepths";
        public const string ImageEffectPropSupportsMultipleClipPARs = "OfxImageEffectPropSupportsMultipleClipPARs";
        public const string ImageEffectPropSetableFrameRate = "OfxImageEffectPropSetableFrameRate";
        public const string ImageEffectPropSetableFielding = "OfxImageEffectPropSetableFielding";
        public const string ImageEffectPropSequentialRenderStatus = "OfxImageEffectPropSequentialRenderStatus";
        public const string ImageEffectInstancePropSequentialRender = "OfxImageEffectInstancePropSequentialRender";
        public const string ImageEffectPropOpenGLRenderSupported = "OfxImageEffectPropOpenGLRenderSupported";
        public const string ImageEffectPropRenderQualityDraft = "OfxImageEffectPropRenderQualityDraft";

        // ホストプロパティ (Parameter)
        public const string ParamHostPropSupportsCustomInteract = "OfxParamHostPropSupportsCustomInteract";
        public const string ParamHostPropSupportsStringAnimation = "OfxParamHostPropSupportsStringAnimation";
        public const string ParamHostPropSupportsChoiceAnimation = "OfxParamHostPropSupportsChoiceAnimation";
        public const string ParamHostPropSupportsBooleanAnimation = "OfxParamHostPropSupportsBooleanAnimation";
        public const string ParamHostPropSupportsCustomAnimation = "OfxParamHostPropSupportsCustomAnimation";
        public const string ParamHostPropSupportsParametricAnimation = "OfxParamHostPropSupportsParametricAnimation";
        public const string ParamHostPropMaxParameters = "OfxParamHostPropMaxParameters";
        public const string ParamHostPropMaxPages = "OfxParamHostPropMaxPages";
        public const string ParamHostPropPageRowColumnCount = "OfxParamHostPropPageRowColumnCount";

        // エフェクトデスクリプタのプロパティ
        public const string PluginPropFilePath = "OfxPluginPropFilePath";
        public const string ImageEffectPluginPropGrouping = "OfxImageEffectPluginPropGrouping";
        public const string ImageEffectPluginPropSingleInstance = "OfxImageEffectPluginPropSingleInstance";
        public const string ImageEffectPluginRenderThreadSafety = "OfxImageEffectPluginRenderThreadSafety";
        public const string ImageEffectPluginPropHostFrameThreading = "OfxImageEffectPluginPropHostFrameThreading";
        public const string ImageEffectPluginPropOverlayInteractV1 = "OfxImageEffectPluginPropOverlayInteractV1";
        public const string ImageEffectPropContext = "OfxImageEffectPropContext";
        public const string ImageEffectPropPluginHandle = "OfxImageEffectPropPluginHandle";

        // クリップのプロパティ
        public const string ImageClipPropOptional = "OfxImageClipPropOptional";
        public const string ImageClipPropIsMask = "OfxImageClipPropIsMask";
        public const string ImageClipPropFieldExtraction = "OfxImageClipPropFieldExtraction";
        public const string ImageEffectPropSupportedComponentsClip = ImageEffectPropSupportedComponents;

        // パラメータ型
        public const string ParamTypeInteger = "OfxParamTypeInteger";
        public const string ParamTypeDouble = "OfxParamTypeDouble";
        public const string ParamTypeBoolean = "OfxParamTypeBoolean";
        public const string ParamTypeChoice = "OfxParamTypeChoice";
        public const string ParamTypeStrChoice = "OfxParamTypeStrChoice";
        public const string ParamTypeRGBA = "OfxParamTypeRGBA";
        public const string ParamTypeRGB = "OfxParamTypeRGB";
        public const string ParamTypeDouble2D = "OfxParamTypeDouble2D";
        public const string ParamTypeInteger2D = "OfxParamTypeInteger2D";
        public const string ParamTypeDouble3D = "OfxParamTypeDouble3D";
        public const string ParamTypeInteger3D = "OfxParamTypeInteger3D";
        public const string ParamTypeString = "OfxParamTypeString";
        public const string ParamTypeCustom = "OfxParamTypeCustom";
        public const string ParamTypeGroup = "OfxParamTypeGroup";
        public const string ParamTypePage = "OfxParamTypePage";
        public const string ParamTypePushButton = "OfxParamTypePushButton";
        public const string ParamTypeParametric = "OfxParamTypeParametric";

        // パラメータのプロパティ
        public const string ParamPropType = "OfxParamPropType";
        public const string ParamPropScriptName = "OfxParamPropScriptName";
        public const string ParamPropHint = "OfxParamPropHint";
        public const string ParamPropParent = "OfxParamPropParent";
        public const string ParamPropSecret = "OfxParamPropSecret";
        public const string ParamPropEnabled = "OfxParamPropEnabled";
        public const string ParamPropAnimates = "OfxParamPropAnimates";
        public const string ParamPropIsAnimating = "OfxParamPropIsAnimating";
        public const string ParamPropIsAutoKeying = "OfxParamPropIsAutoKeying";
        public const string ParamPropPersistant = "OfxParamPropPersistant";
        public const string ParamPropEvaluateOnChange = "OfxParamPropEvaluateOnChange";
        public const string ParamPropCanUndo = "OfxParamPropCanUndo";
        public const string ParamPropPluginMayWrite = "OfxParamPropPluginMayWrite";
        public const string ParamPropDefault = "OfxParamPropDefault";
        public const string ParamPropMin = "OfxParamPropMin";
        public const string ParamPropMax = "OfxParamPropMax";
        public const string ParamPropDisplayMin = "OfxParamPropDisplayMin";
        public const string ParamPropDisplayMax = "OfxParamPropDisplayMax";
        public const string ParamPropIncrement = "OfxParamPropIncrement";
        public const string ParamPropDigits = "OfxParamPropDigits";
        public const string ParamPropDoubleType = "OfxParamPropDoubleType";
        public const string ParamPropDefaultCoordinateSystem = "OfxParamPropDefaultCoordinateSystem";
        public const string ParamPropDimensionLabel = "OfxParamPropDimensionLabel";
        public const string ParamPropChoiceOption = "OfxParamPropChoiceOption";
        // StrChoice の値の一覧と Choice/StrChoice の表示順 (1.5)
        public const string ParamPropChoiceEnum = "OfxParamPropChoiceEnum";
        public const string ParamPropChoiceOrder = "OfxParamPropChoiceOrder";
        public const string ParamPropGroupOpen = "OfxParamPropGroupOpen";
        public const string ParamPropStringMode = "OfxParamPropStringMode";
        public const string ParamPropStringFilePathExists = "OfxParamPropStringFilePathExists";
        public const string ParamPropInteractV1 = "OfxParamPropInteractV1";
        public const string ParamPropShowTimeMarker = "OfxParamPropShowTimeMarker";
        public const string ParamPropPageChild = "OfxParamPropPageChild";

        // パラメータのプロパティ値
        public const string ParamDoubleTypePlain = "OfxParamDoubleTypePlain";
        public const string ParamDoubleTypeScale = "OfxParamDoubleTypeScale";
        public const string ParamStringIsSingleLine = "OfxParamStringIsSingleLine";
        public const string ParamStringIsMultiLine = "OfxParamStringIsMultiLine";
        public const string ParamCoordinatesCanonical = "OfxParamCoordinatesCanonical";

        // ホスト UI の原点 (1.4)。値の文字列リテラルが "k" で始まるのはヘッダ通り
        public const string HostNativeOriginBottomLeft = "kOfxImageEffectHostPropNativeOriginBottomLeft";
        public const string HostNativeOriginTopLeft = "kOfxImageEffectHostPropNativeOriginTopLeft";
        public const string HostNativeOriginCenter = "kOfxImageEffectHostPropNativeOriginCenter";

        // GPU レンダリング関連 (1.5 系。機能検出の読み取りに応答するために定義)
        public const string ImageEffectPropCudaRenderSupported = "OfxImageEffectPropCudaRenderSupported";
        public const string ImageEffectPropCudaStreamSupported = "OfxImageEffectPropCudaStreamSupported";
        public const string ImageEffectPropMetalRenderSupported = "OfxImageEffectPropMetalRenderSupported";
        public const string ImageEffectPropOpenCLRenderSupported = "OfxImageEffectPropOpenCLRenderSupported";
        public const string ImageEffectPropCudaEnabled = "OfxImageEffectPropCudaEnabled";
        public const string ImageEffectPropCudaStream = "OfxImageEffectPropCudaStream";
        public const string ImageEffectPropMetalEnabled = "OfxImageEffectPropMetalEnabled";
        public const string ImageEffectPropMetalCommandQueue = "OfxImageEffectPropMetalCommandQueue";
        public const string ImageEffectPropOpenCLEnabled = "OfxImageEffectPropOpenCLEnabled";
        public const string ImageEffectPropOpenGLEnabled = "OfxImageEffectPropOpenGLEnabled";
        public const string ImageEffectPropOpenGLTextureIndex = "OfxImageEffectPropOpenGLTextureIndex";
        public const string ImageEffectPropOpenGLTextureTarget = "OfxImageEffectPropOpenGLTextureTarget";
        public const string ImageEffectPropOpenCLCommandQueue = "OfxImageEffectPropOpenCLCommandQueue";
        // OpenCL Images 方式 (1.5) の対応宣言と画像ハンドル (Buffers 方式は OpenCLRenderSupported / ImagePropData)
        public const string ImageEffectPropOpenCLSupported = "OfxImageEffectPropOpenCLSupported";
        public const string ImageEffectPropOpenCLImage = "OfxImageEffectPropOpenCLImage";

        // 1.5.1 で追加されたプロパティ
        public const string ImageEffectPropCPURenderSupported = "OfxImageEffectPropCPURenderSupported";
        public const string ImageEffectPropThumbnailRender = "OfxImageEffectPropThumbnailRender";
        public const string ImageEffectPropNoSpatialAwareness = "OfxImageEffectPropNoSpatialAwareness";

        // OpenGL コンテキストのライフサイクル通知アクション (1.5)。
        // Detached の文字列リテラルが "k" で始まるのはヘッダ (ofxGPURender.h) 通り
        public const string ActionOpenGLContextAttached = "OfxActionOpenGLContextAttached";
        public const string ActionOpenGLContextDetached = "kOfxActionOpenGLContextDetached";

        public const string ParamHostPropSupportsStrChoice = "OfxParamHostPropSupportsStrChoice";
        public const string ParamHostPropSupportsStrChoiceAnimation = "OfxParamHostPropSupportsStrChoiceAnimation";

        // インスタンスのプロパティ
        public const string ImageEffectPropProjectSize = "OfxImageEffectPropProjectSize";
        public const string ImageEffectPropProjectOffset = "OfxImageEffectPropProjectOffset";
        public const string ImageEffectPropProjectExtent = "OfxImageEffectPropProjectExtent";
        // 注意: 定数名は kOfxImageEffectPropProjectPixelAspectRatio だが、文字列リテラルに "Project" が入らない (ofxImageEffect.h の非一貫性)
        public const string ImageEffectPropProjectPixelAspectRatio = "OfxImageEffectPropPixelAspectRatio";
        public const string ImageEffectInstancePropEffectDuration = "OfxImageEffectInstancePropEffectDuration";
        public const string ImageEffectPropFrameRate = "OfxImageEffectPropFrameRate";
        public const string ImageEffectPropFrameRange = "OfxImageEffectPropFrameRange";
        public const string ImageEffectPropUnmappedFrameRate = "OfxImageEffectPropUnmappedFrameRate";
        public const string ImageEffectPropUnmappedFrameRange = "OfxImageEffectPropUnmappedFrameRange";
        public const string ImageEffectPropRenderScale = "OfxImageEffectPropRenderScale";
        public const string ImageEffectPropFrameVarying = "OfxImageEffectFrameVarying";

        // クリップインスタンスのプロパティ
        public const string ImageEffectPropComponents = "OfxImageEffectPropComponents";
        public const string ImageEffectPropPixelDepth = "OfxImageEffectPropPixelDepth";
        public const string ImageClipPropUnmappedComponents = "OfxImageClipPropUnmappedComponents";
        public const string ImageClipPropUnmappedPixelDepth = "OfxImageClipPropUnmappedPixelDepth";
        public const string ImageEffectPropPreMultiplication = "OfxImageEffectPropPreMultiplication";
        public const string ImagePropPixelAspectRatio = "OfxImagePropPixelAspectRatio";
        public const string ImageClipPropFieldOrder = "OfxImageClipPropFieldOrder";
        public const string ImageClipPropConnected = "OfxImageClipPropConnected";
        public const string ImageClipPropContinuousSamples = "OfxImageClipPropContinuousSamples";

        // 画像のプロパティ
        public const string ImagePropData = "OfxImagePropData";
        public const string ImagePropBounds = "OfxImagePropBounds";
        public const string ImagePropRegionOfDefinition = "OfxImagePropRegionOfDefinition";
        public const string ImagePropRowBytes = "OfxImagePropRowBytes";
        public const string ImagePropField = "OfxImagePropField";
        public const string ImagePropUniqueIdentifier = "OfxImagePropUniqueIdentifier";

        // レンダリングアクションの引数
        public const string ImageEffectPropRenderWindow = "OfxImageEffectPropRenderWindow";
        public const string ImageEffectPropFieldToRender = "OfxImageEffectPropFieldToRender";
        public const string ImageEffectPropRegionOfDefinition = "OfxImageEffectPropRegionOfDefinition";
        public const string ImageEffectPropRegionOfInterest = "OfxImageEffectPropRegionOfInterest";
        public const string ImageEffectPropFrameStep = "OfxImageEffectPropFrameStep";
        public const string ImageEffectPropInteractiveRenderStatus = "OfxImageEffectPropInteractiveRenderStatus";

        // アルファの事前乗算状態
        public const string ImageOpaque = "OfxImageOpaque";
        public const string ImagePreMultiplied = "OfxImageAlphaPremultiplied";
        public const string ImageUnPreMultiplied = "OfxImageAlphaUnPremultiplied";

        // フィールド
        public const string ImageFieldNone = "OfxFieldNone";

        // メッセージ種別
        public const string MessageFatal = "OfxMessageFatal";
        public const string MessageError = "OfxMessageError";
        public const string MessageWarning = "OfxMessageWarning";
        public const string MessageMessage = "OfxMessageMessage";
        public const string MessageLog = "OfxMessageLog";
        public const string MessageQuestion = "OfxMessageQuestion";

        // 変更理由
        public const string ChangeUserEdited = "OfxChangeUserEdited";
        public const string ChangePluginEdited = "OfxChangePluginEdited";
        public const string ChangeTime = "OfxChangeTime";

        // レンダリングのスレッド安全性
        public const string ImageEffectRenderUnsafe = "OfxImageEffectRenderUnsafe";
        public const string ImageEffectRenderInstanceSafe = "OfxImageEffectRenderInstanceSafe";
        public const string ImageEffectRenderFullySafe = "OfxImageEffectRenderFullySafe";

        // クリップのプロパティ値
        public const string ImageFieldExtractionDoubled = "OfxImageFieldDoubled";

        // タイプ識別 (kOfxTypeXXX)
        public const string TypeImageEffectHost = "OfxTypeImageEffectHost";
        public const string TypeImageEffect = "OfxTypeImageEffect";
        public const string TypeImageEffectInstance = "OfxTypeImageEffectInstance";
        public const string TypeClip = "OfxTypeClip";
        public const string TypeImage = "OfxTypeImage";
        public const string TypeParameter = "OfxTypeParameter";
        public const string TypeParameterInstance = "OfxTypeParameterInstance";
    }
}

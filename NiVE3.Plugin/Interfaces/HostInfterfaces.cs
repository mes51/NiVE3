using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    public interface IAcceleratorObject
    {
        /// <summary>
        /// 現在使用しているGPUのデバイスを取得します
        /// </summary>
        GraphicsDevice CurrentDevice { get; }
    }

    public interface ICompositionObject
    {
        /// <summary>
        /// コンポジションのフレームレートを取得します
        /// </summary>
        double FrameRate { get; }

        /// <summary>
        /// コンポジションの幅を取得します
        /// </summary>
        int Width { get; }

        /// <summary>
        /// コンポジションの高さを取得します
        /// </summary>
        int Height { get; }

        /// <summary>
        /// レイヤーを表す識別子の一覧を取得します
        /// </summary>
        IReadOnlyCollection<LayerInfo> LayerIdentifiers { get; }

        /// <summary>
        /// レイヤーの識別子からレイヤーを取得します
        /// </summary>
        /// <param name="layerIdentifier">レイヤーの識別子</param>
        /// <returns>レイヤーを表すILayerObject。一致するレイヤーがなかった場合はnull</returns>
        ILayerObject? GetLayer(Guid layerIdentifier);

        /// <summary>
        /// アクティブなカメラの設定を取得します
        /// </summary>
        /// <param name="layerTime">カメラ設定を取得する時のコンポジションの時間</param>
        /// <returns>取得したアクティブなカメラの設定</returns>
        CameraSetting GetActiveCameraSetting(Time globalTime);
    }

    public interface ILayerObject
    {
        public const string TransformAnchorPointId = nameof(TransformAnchorPointId);

        public const string TransformPositionId = nameof(TransformPositionId);

        public const string TransformDirectionId = nameof(TransformDirectionId);

        public const string TransformXAngleId = nameof(TransformXAngleId);

        public const string TransformYAngleId = nameof(TransformYAngleId);

        public const string TransformZAngleId = nameof(TransformZAngleId);

        public const string TransformScaleId = nameof(TransformScaleId);

        public const string TransformPropertyOpacityId = nameof(TransformPropertyOpacityId);

        public const string TransformPointOfInterestId = nameof(TransformPointOfInterestId);

        public const string TransformOrientationId = nameof(TransformOrientationId);

        public const string ImageLayerOptionIsCastShadowId = nameof(ImageLayerOptionIsCastShadowId);

        public const string ImageLayerOptionLightTransmissionId = nameof(ImageLayerOptionLightTransmissionId);

        public const string ImageLayerOptionIsAcceptShadowId = nameof(ImageLayerOptionIsAcceptShadowId);

        public const string ImageLayerOptionIsAcceptLightId = nameof(ImageLayerOptionIsAcceptLightId);

        public const string ImageLayerOptionAmbientId = nameof(ImageLayerOptionAmbientId);

        public const string ImageLayerOptionDiffuseId = nameof(ImageLayerOptionDiffuseId);

        public const string ImageLayerOptionSpecularIntensityId = nameof(ImageLayerOptionSpecularIntensityId);

        public const string ImageLayerOptionSpecularShininessId = nameof(ImageLayerOptionSpecularShininessId);

        public const string ImageLayerOptionMetalId = nameof(ImageLayerOptionMetalId);

        public const string CameraLayerOptionZoomId = nameof(CameraLayerOptionZoomId);

        public const string LightLayerOptionLightTypeId = nameof(LightLayerOptionLightTypeId);

        public const string LightLayerOptionColorId = nameof(LightLayerOptionColorId);

        public const string LightLayerOptionIntensityId = nameof(LightLayerOptionIntensityId);

        public const string LightLayerOptionConeAngleId = nameof(LightLayerOptionConeAngleId);

        public const string LightLayerOptionConeAttenuationId = nameof(LightLayerOptionConeAttenuationId);

        public const string LightLayerOptionFalloffTypeId = nameof(LightLayerOptionFalloffTypeId);

        public const string LightLayerOptionFalloffStartId = nameof(LightLayerOptionFalloffStartId);

        public const string LightLayerOptionFalloffLengthId = nameof(LightLayerOptionFalloffLengthId);

        public const string LightLayerOptionEnableShadowId = nameof(LightLayerOptionEnableShadowId);

        public const string LightLayerOptionShadowStrengthId = nameof(LightLayerOptionShadowStrengthId);

        public const string LightLayerOptionShadowScatterSizeId = nameof(LightLayerOptionShadowScatterSizeId);

        public const string AudioLevelId = nameof(AudioLevelId);

        public string Name { get; }

        public Time SourceStartPoint { get; }

        /// <summary>
        /// エフェクトを表す識別子の一覧を取得します
        /// </summary>
        public IReadOnlyCollection<Guid> EffectIdentifiers { get; }

        /// <summary>
        /// マスクを表す識別子の一覧を取得します
        /// </summary>
        public IReadOnlyCollection<Guid> MaskIdentifiers { get; }

        /// <summary>
        /// エフェクトの識別子からエフェクトを取得します
        /// </summary>
        /// <param name="effectIdentifier">エフェクトの識別子</param>
        /// <returns>エフェクトを表すIEffectObject。一致するエフェクトが存在しなかった場合はnull</returns>
        IEffectObject? GetEffect(Guid effectIdentifier);

        /// <summary>
        /// マスクの識別子からマスクを取得します
        /// </summary>
        /// <param name="maskIdentifier">マスクの識別子</param>
        /// <returns>マスクを表すIMaskObject。一致するマスクが存在しなかった場合はnull</returns>
        IMaskObject? GetMask(Guid maskIdentifier);

        /// <summary>
        /// フッテージから取得した画像そのままを取得します
        /// </summary>
        /// <param name="globalTime">画像を取得する時のコンポジションの時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの割合</param>
        /// <param name="useGpu">GPUを使用するかどうか</param>
        /// <returns>取得した画像。指定した時間がソースの長さを超えていた場合はnull</returns>
        NImage? GetRawImage(Time globalTime, double downSamplingRate, bool useGpu);

        /// <summary>
        /// マスク適用済みのレイヤーの画像を取得します
        /// </summary>
        /// <param name="globalTime">画像を取得する時のコンポジションの時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの割合</param>
        /// <param name="useGpu">GPUを使用するかどうか</param>
        /// <returns>取得した画像。指定した時間がソースの長さを超えていた場合はnull</returns>
        NImage? GetMaskedImage(Time globalTime, double downSamplingRate, bool useGpu);

        /// <summary>
        /// マスクとエフェクト適用済みのレイヤーの画像を取得します
        /// </summary>
        /// <param name="globalTime">画像を取得する時のコンポジションの時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの割合</param>
        /// <param name="useGpu">GPUを使用するかどうか</param>
        /// <returns>取得した画像。指定した時間がソースの長さを超えていた場合はnull</returns>
        NImage? GetEffectedImage(Time globalTime, double downSamplingRate, bool useGpu);
    }

    public interface IFootageSourceUsingLayerObject
    {
        public Time SourceStartPoint { get; }

        /// <summary>
        /// マスクを表す識別子の一覧を取得します
        /// </summary>
        public IReadOnlyCollection<Guid> MaskIdentifiers { get; }

        /// <summary>
        /// マスクの識別子からマスクを取得します
        /// </summary>
        /// <param name="maskIdentifier">マスクの識別子</param>
        /// <returns>マスクを表すIMaskObject。一致するマスクが存在しなかった場合はnull</returns>
        IMaskObject? GetMask(Guid maskIdentifier);
    }

    public interface IEffectObject { }

    public interface IMaskObject
    {
        string Name { get; }

        /// <summary>
        /// このマスクのパスを取得します
        /// </summary>
        /// <param name="globalTime">マスクのパスを取得する時のコンポジション時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの割合</param>
        /// <returns>取得したパス</returns>
        BezierPath GetPath(Time globalTime, double downSamplingRate);
    }

    public interface IPropertyObject
    {
        string Id { get; }

        bool IsEnable { get; }

        IReadOnlyCollection<IPropertyObject>? GetChildren();

        /// <summary>
        /// 指定した時間のプロパティの値を取得します
        /// </summary>
        /// <param name="layerTime">プロパティの値を取得する時のレイヤー時間</param>
        /// <param name="withoutDisableProperty">AppendablePropertyの場合、IsEnableがfalseのプロパティを除外するかどうか</param>
        /// <returns>取得したプロパティの値。PropertyGroupの場合はnull</returns>
        public object? GetValue(Time layerTime, bool withoutDisableProperty = false);

        /// <summary>
        /// 指定した時間のプロパティの値のグループを取得します
        /// </summary>
        /// <param name="layerTime">プロパティの値を取得する時のレイヤー時間</param>
        /// <param name="withoutDisableProperty">子のAppendablePropertyで、IsEnableがfalseのプロパティを除外するかどうか</param>
        /// <returns>取得したプロパティの値をまとめたPropertyValueGroup。PropertyGroup以外の場合はnull</returns>
        public PropertyValueGroup? GetValues(Time layerTime, bool withoutDisableProperty = false);

        /// <summary>
        /// このオブジェクトの固有のIDとプロパティ、または子のプロパティのキーフレームを含めた値全体のハッシュを計算します
        /// </summary>
        /// <param name="hash">計算用のハッシュオブジェクト</param>
        public void CalcValuesHash(XxHash3 hash);
    }

    public interface ICompositionViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// レイヤーのViewModelの一覧を取得します。INotifyCollectionChangedでもあります。
        /// </summary>
        IReadOnlyCollection<ILayerViewModel> LayerViewModels { get; }
    }

    public interface ILayerViewModel : INotifyPropertyChanged
    {
        Guid LayerId { get; }

        string Name { get; }

        string SourceName { get; }

        SourceType SourceType { get; }

        bool IsEnable3D { get; }

        /// <summary>
        /// エフェクトのViewModelの一覧を取得します。INotifyCollectionChangedでもあります。
        /// </summary>
        IReadOnlyCollection<IEffectViewModel> EffectViewModels { get; }

        /// <summary>
        /// マスクのViewModelの一覧を取得します。INotifyCollectionChangedでもあります。
        /// </summary>
        IReadOnlyCollection<IMaskViewModel> MaskViewModels { get; }
    }

    public interface IEffectViewModel : INotifyPropertyChanged
    {
        Guid EffectId { get; }

        string Name { get; }
    }

    public interface IMaskViewModel : INotifyPropertyChanged
    {
        Guid MaskId { get; }

        string Name { get; }
    }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? CurrentTimeValue { get; }

        object? CurrentTimeRawValue { get; set; }

        bool IsEnableExpression { get; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}

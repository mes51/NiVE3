using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// レンダラのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// コンポジションのサイズを設定します。コンポジションのサイズが変更される度に呼び出されます
        /// </summary>
        /// <param name="width">コンポジションの幅</param>
        /// <param name="height">コンポジションの高さ</param>
        void SetSize(int width, int height);

        /// <summary>
        /// 3D用のカメラを設定します。レンダリングする度に呼ばれます。
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        void SetCamera(CameraSetting cameraSetting);

        /// <summary>
        /// 3D用のライトを追加します。有効なライトが複数個ある場合はその数だけ呼ばれます。
        /// </summary>
        /// <param name="lightSetting">追加するライトの設定</param>
        void AddLight(LightSetting lightSetting);

        /// <summary>
        /// レンダリングを開始します
        /// </summary>
        /// <param name="downSamplingRate">ダウンサンプリングの倍率</param>
        /// <param name="useGpu">GPUを使用するかどうか</param
        void BeginRendering(double downSamplingRate, bool useGpu);

        /// <summary>
        /// 画像のレンダリングを行います。画像はレイヤーの下位のものから順に渡されます
        /// </summary>
        /// <param name="layers">レンダリング対象の画像の配列</param>
        void Render(RenderableImage[] images);

        /// <summary>
        /// 調整レイヤーをコンポジションの中心に描画します
        /// </summary>
        /// <param name="image">描画する画像</param>
        /// <param name="roi">画像のROI</param>
        /// <param name="downSamplingRate">ダウンサンプリングの比率</param>
        /// <param name="interpolationQuality">画像のレンダリング時の補間方法</param>
        /// <param name="blendMode">画像のブレンドモード</param>
        void RenderAdjustmentLayer(NImage image, ROI roi, double downSamplingRate, ImageInterpolationQuality interpolationQuality, BlendMode blendMode);

        /// <summary>
        /// 調整レイヤーなど、ここまでにレンダリングされた画像を必要とする機能のためにレンダリングの途中結果を取得します。レンダリング処理は継続されます
        /// </summary>
        /// <returns>レンダリングされた画像</returns>
        NImage GetCurrentRenderedImage();

        /// <summary>
        /// レンダリングを完了し、最終的な画像を取得します
        /// </summary>
        /// <returns>レンダリングされた画像</returns>
        NImage FinishRendering();

        /// <summary>
        /// 調整レイヤーのマスクを描画します
        /// </summary>
        /// <param name="image">描画するレイヤー</param>
        /// <returns>描画された調整レイヤーのマスク</returns>
        RasterizedMaskImage RenderAdjustmentMask(RenderableImage image);

        /// <summary>
        /// 2Dレイヤーのバウンディングボックスを取得します
        /// </summary>
        /// <param name="origin">フッテージ画像の位置</param>
        /// <param name="width">レイヤーの幅</param>
        /// <param name="height">レイヤーの高さ</param>
        /// <param name="transform">レイヤーのトランスフォームの値</param>
        /// <param name="parentTransforms">親のレイヤーのトランスフォームの値</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetBoundingBox2D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms);

        /// <summary>
        /// 3Dレイヤーのバウンディングボックスを取得します
        /// </summary>
        /// <param name="origin">フッテージ画像の位置</param>
        /// <param name="width">レイヤーの幅</param>
        /// <param name="height">レイヤーの高さ</param>
        /// <param name="transform">レイヤーのトランスフォームの値</param>
        /// <param name="parentTransforms">親のレイヤーのトランスフォームの値</param>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetBoundingBox3D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms, CameraSetting cameraSetting);

        /// <summary>
        /// カメラのバウンディングボックスを取得します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetCameraBoundingBox(CameraSetting targetCameraSetting, CameraSetting cameraSetting);

        /// <summary>
        /// ライトのバウンディングボックスを取得します
        /// </summary>
        /// <param name="lightSetting">バウンディングボックスを算出するライトの設定</param>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetLightBoundingBox(LightSetting lightSetting, CameraSetting cameraSetting);

        /// <summary>
        /// スクリーン座標からレイヤーを選択します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="layers">現在コンポジションに存在する選択可能なレイヤーの配列</param>
        /// <param name="x">スクリーンのX座標</param>
        /// <param name="y">スクリーンのY座標</param>
        /// <returns>スクリーンの座標最前面に存在するレイヤーのID、存在しない場合はnull</returns>
        Guid? SelectLayer(CameraSetting cameraSetting, LayerSkeleton[] layers, double x, double y);

        /// <summary>
        /// スクリーン座標からコンポジションのワールド座標に変換します。
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="x">スクリーンのX座標</param>
        /// <param name="y">スクリーンのY座標</param>
        /// <returns>コンポジションのワールド座標座標</returns>
        Vector3d ScreenCoordToWorldCoord(CameraSetting cameraSetting, double x, double y);
    }
}

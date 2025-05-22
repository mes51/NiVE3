using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        /// 現在のレンダラの設定を表すデータをシリアル化可能な状態で取得します
        /// </summary>
        /// <returns>レンダラの設定を表すシリアル化可能なobject</returns>
        object? SaveSetting() => null;

        /// <summary>
        /// レンダラの設定を読み込みます
        /// </summary>
        /// <param name="data">読み込むレンダラの設定を表すobject、コンポジション設定画面で最初に設定を行う場合などにnullになる可能性があります</param>
        /// <returns>レンダラの設定が完了した場合はtrue、そうでない場合はfalse</returns>
        bool LoadSetting(object? data) => true;

        /// <summary>
        /// レンダラの設定画面を表示するためのViewを取得します。
        /// RendererMetadataAttribute.HasSettingViewがtrueの時のみ、ユーザーが設定変更をしようとしたときに呼ばれます。
        /// </summary>
        /// <param name="compositionSize">現在のコンポジションのサイズ。コンポジション設定画面上で変更されており、変更適用前の可能性があります</param>
        /// <returns>レンダラの設定画面のView。画面が存在しない場合はnull</returns>
        FrameworkElement? GetRendererSetting(Int32Size compositionSize) => null;

        /// <summary>
        /// レンダラの設定を適用します。
        /// GetOutputSettingで取得したViewを表示後、ユーザーによってOKが選択されたときに呼び出されます。
        /// </summary>
        /// <param name="setting">GetOutputSettingで取得したViewのDataContext</param>
        /// <returns>適用に成功した場合はtrue、そうでない場合はfalse</returns>
        bool ApplySetting(object? setting) => false;

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
        /// エラーが発生したときなどにレンダリングを中止します
        /// </summary>
        void AbortRendering();

        /// <summary>
        /// 調整レイヤーのマスクを描画します
        /// </summary>
        /// <param name="image">描画するレイヤー</param>
        /// <returns>描画された調整レイヤーのマスク</returns>
        RasterizedMaskImage RenderAdjustmentMask(RenderableImage image);
    }
}

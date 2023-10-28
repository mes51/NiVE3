using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;

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
        /// 3D用のカメラを設定します。レンダリングする度に呼ばれますが、カメラがコンポジション内に存在しない場合は呼ばれません
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        void SetCamera(CameraSetting cameraSetting);

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
        /// 調整レイヤーなど、ここまでにレンダリングされた画像を必要とする機能のためにレンダリングの途中結果を取得します。レンダリング処理は継続されます
        /// </summary>
        /// <returns>レンダリングされた画像</returns>
        NImage GetCurrentRenderedImage();

        /// <summary>
        /// レンダリングを完了し、最終的な画像を取得します。
        /// </summary>
        /// <returns>レンダリングされた画像</returns>
        NImage FinishRendering();
    }
}

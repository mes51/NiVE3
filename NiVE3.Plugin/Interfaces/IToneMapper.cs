using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;

namespace NiVE3.Plugin.Interfaces
{
    public interface IToneMapper : IDisposable
    {
        /// <summary>
        /// エフェクトのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// トーンマッピングを実行します
        /// </summary>
        /// <param name="image">トーンマッピングする画像</param>
        /// <param name="useGpu">GPUを使用するかどうか</param
        /// <returns>トーンマッピング後の画像</returns>
        public NImage ToneMapping(NImage image, bool useGpu);
    }
}

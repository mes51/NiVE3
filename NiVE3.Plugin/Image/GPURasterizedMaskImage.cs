using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Image
{
    /// <summary>
    /// GPUから参照可能なマスク画像データを表します
    /// BGRA上のAに相当する値のみを格納します。
    /// </summary>
    public abstract class GPURasterizedMaskImage : RasterizedMaskImage
    {
        internal GPURasterizedMaskImage(int width, int height) : base(width, height) { }

        /// <summary>
        /// マスク画像データをCPU側にコピーしたRasterizedMaskImageを新たに生成します
        /// </summary>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>生成されたManagedRasterizedMaskImage</returns>
        public abstract ManagedRasterizedMaskImage CopyToCpu();
    }
}

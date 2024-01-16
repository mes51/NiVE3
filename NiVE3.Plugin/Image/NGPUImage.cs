using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Image
{
    /// <summary>
    /// GPUから参照可能な画像データを表します
    /// </summary>
    public abstract class NGPUImage : NImage
    {
        internal NGPUImage(int width, int height) : base(width, height) { }

        /// <summary>
        /// 画像データをCPU側にコピーしたNImageを新たに生成します
        /// </summary>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>生成されたNManagedImage</returns>
        public abstract NManagedImage CopyToCpu(bool needClear = false);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Interfaces
{
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// レンダラのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);
    }
}

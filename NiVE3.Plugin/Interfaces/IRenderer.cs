using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;

namespace NiVE3.Plugin.Interfaces
{
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// レンダラのセットアップを行います。Acceleratorが更新される度に呼ばれます。
        /// </summary>
        /// <param name="accelerator">CUDAを実行するデバイスを表すAccelerator。使用できるデバイスがない場合はnull</param>
        public void SetupAccelerator(Accelerator? accelerator);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.Interfaces
{
    public interface IEffect : IDisposable
    {
        /// <summary>
        /// エフェクトのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// このエフェクトを操作するためのプロパティを取得します
        /// </summary>
        /// <returns>PropertyBaseの配列</returns>
        PropertyBase[] GetProperties();
    }
}

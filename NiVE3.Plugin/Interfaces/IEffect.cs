using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

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

        /// <summary>
        /// エフェクトを適用します
        /// </summary>
        /// <param name="image">適用先の画像</param>
        /// <param name="roi">エフェクトの適用範囲</param>
        /// <param name="layerTime">現在のレイヤーの時間</param>
        /// <param name="properties">プロパティ</param>
        /// <returns></returns>
        NImage Process(NImage image, ROI roi, double layerTime, PropertyValueGroup properties);
    }
}

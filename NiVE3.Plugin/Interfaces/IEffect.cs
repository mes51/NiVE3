using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
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
        /// <param name="sourceSize">ソースのサイズ。音声のみの場合、またはフッテージがICustomizableFootageSourceの画像・動画の場合はコンポジションサイズになります</param>
        /// <returns>PropertyBaseの配列</returns>
        PropertyBase[] GetProperties(Int32Size sourceSize);

        /// <summary>
        /// エフェクトの適用範囲を計算します
        /// </summary>
        /// <param name="baseRoi">このエフェクトの前までで計算されたエフェクトの適用範囲</param>
        /// <param name="downSamplingRateX">幅のダウンサンプリングの比率</param>
        /// <param name="downSamplingRateY">高さのダウンサンプリングの比率</param>
        /// <param name="layerTime">現在のレイヤーの時間</param>
        /// <param name="properties">プロパティ</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>計算後のエフェクトの適用範囲</returns>
        ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition) => baseRoi;

        /// <summary>
        /// 画像にエフェクトを適用します
        /// </summary>
        /// <param name="image">適用先の画像</param>
        /// <param name="roi">エフェクトの適用範囲</param>
        /// <param name="downSamplingRateX">幅のダウンサンプリングの比率</param>
        /// <param name="downSamplingRateY">高さのダウンサンプリングの比率</param>
        /// <param name="layerTime">現在のレイヤーの時間</param>
        /// <param name="properties">プロパティ</param>
        /// <param name="composition">コンポジション</param>
        /// <param name="useGpu">GPUを使用するかどうか</param
        /// <returns>エフェクト適用後の画像</returns>
        NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu);

        /// <summary>
        /// 音声にエフェクトを適用します
        /// </summary>
        /// <param name="audio">音声</param>
        /// <param name="startTime">音声の開始時間</param>
        /// <param name="properties">プロパティ</param>
        /// <param name="composition">コンポジション</param>
        /// <returns>エフェクト適用後の音声</returns>
        float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition);
    }
}

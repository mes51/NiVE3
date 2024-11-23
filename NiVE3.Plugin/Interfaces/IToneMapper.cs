using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Image;

namespace NiVE3.Plugin.Interfaces
{
    public interface IToneMapper : IDisposable
    {
        /// <summary>
        /// トーンマッパーのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// トーンマッパープラグインの設定画面を表示するためのウインドウを取得します。
        /// ToneMapperMetadataAttribute.HasSettingViewがtrueの時のみ、Loadメソッド呼び出し後に呼ばれます。
        /// </summary>
        /// <returns>トーンマッパープラグインの設定画面のView。設定画面が存在しない場合はnull</returns>
        FrameworkElement? GetToneMapperSetting() => null;

        /// <summary>
        /// 現在のトーンマッパープラグインの設定をシリアライズ可能な状態で取得します。
        /// </summary>
        /// <returns>トーンマッパープラグインの状態を表すシリアル化可能なobject</returns>
        object? SaveSetting() => null;

        /// <summary>
        /// トーンマッパープラグインの状態を読み込みます。
        /// </summary>
        /// <param name="data">読み込むトーンマッパープラグインの状態を表すobject</param>
        /// <returns>プラグインの初期化が完了した場合はtrue、そうでない場合はfalse</returns>
        bool LoadSetting(object? data) => true;

        /// <summary>
        /// トーンマッパープラグインの設定を適用します。
        /// GetToneMapperSettingで取得したViewを表示後、ユーザーによってOKが選択されたときに呼び出されます。
        /// </summary>
        /// <param name="setting">GetToneMapperSettingで取得したViewのDataContext</param>
        /// <returns>適用に成功した場合はtrue、そうでない場合はfalse</returns>
        bool ApplySetting(object? setting) => true;

        /// <summary>
        /// トーンマッピングを実行します
        /// </summary>
        /// <param name="image">トーンマッピングする画像</param>
        /// <param name="useGpu">GPUを使用するかどうか</param
        /// <returns>トーンマッピング後の画像</returns>
        public NImage ToneMapping(NImage image, bool useGpu);
    }
}

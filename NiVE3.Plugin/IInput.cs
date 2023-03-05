using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Image;

namespace NiVE3.Plugin
{
    /// <summary>
    /// 入力プラグインを表すインターフェース
    /// </summary>
    public interface IInput : IDisposable
    {
        /// <summary>
        /// 読み込んでいるファイルのパス
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// 読み込んだファイルのフレームレート
        /// </summary>
        double FrameRate { get; }

        /// <summary>
        /// 読み込んだファイルの画像の幅
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 読み込んだファイルの画像の高さ
        /// </summary>
        int Height { get; }

        /// <summary>
        /// ファイルを読み込みます
        /// </summary>
        /// <param name="filePath">読み込むファイルのパス</param>
        void Load(string filePath);

        /// <summary>
        /// ファイルから画像を読み込みます
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="toGpu">GPU上に直接読み込む場合はtrue、CPU上に読み込む場合はfalse</param>
        /// <returns>読み込んだ画像を表すNImage</returns>
        NImage Read(double time, bool toGpu);

        /// <summary>
        /// 現在の入力プラグインの状態を表すデータをシリアル化可能な状態で取得します
        /// </summary>
        /// <returns>入力プラグインの状態を表すシリアル化可能なobject</returns>
        object? SaveData() => null;

        /// <summary>
        /// 入力プラグインの状態を読み込みます
        /// </summary>
        /// <param name="data">読み込む入力プラグインの状態を表すobject</param>
        void LoadData(object? data) { }

        /// <summary>
        /// 入力プラグインの設定画面を表示するためのウインドウを取得します
        /// InputMetadataAttribute.HasSettingWindowがtrueの時のみ、Loadメソッド呼び出し後に呼ばれます
        /// </summary>
        /// <returns>入力プラグインの設定画面のWindow</returns>
        Window? ShowLoadSetting() => null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ILGPU.Runtime;
using NiVE3.Plugin.Image;

namespace NiVE3.Plugin.Interfaces
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
        /// 読み込んだファイルの長さ
        /// </summary>
        double Duration { get; }

        /// <summary>
        /// 対応するファイルの拡張子
        /// 書式はOpenFileDialog.Filterに合わせる必要があります
        /// </summary>
        string SupportedFileExtensions { get; }

        /// <summary>
        /// ファイルを読み込みます
        /// </summary>
        /// <param name="filePath">読み込むファイルのパス</param>
        /// <returns>読み込みが成功した場合はtrue(HasSettingViewがtrueの場合は、GetLoadSettingを呼び出しても問題ない場合)、そうでない場合はfalse</returns>
        bool Load(string filePath);

        /// <summary>
        /// ファイルから画像を読み込みます
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="toGpu">GPU上に直接読み込む場合はtrue、CPU上に読み込む場合はfalse</param>
        /// <returns>読み込んだ画像を表すNImage</returns>
        // TODO: Acceleratorをラップしたものを渡す
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
        /// <returns>プラグインの初期化、およびファイルの読み込みが完了した場合はtrue、そうでない場合はfalse</returns>
        bool LoadData(object? data) => true;

        /// <summary>
        /// 入力プラグインの設定画面を表示するためのウインドウを取得します。
        /// InputMetadataAttribute.HasSettingViewがtrueの時のみ、Loadメソッド呼び出し後に呼ばれます。
        /// </summary>
        /// <param name="compositionSize">現在開いているコンポジションのサイズ。開いていない場合はnull。</param>
        /// <returns>入力プラグインの設定画面のView。ファイルによって設定画面が存在しない場合はnull</returns>
        FrameworkElement? GetLoadSetting(Size? compositionSize) => null;

        /// <summary>
        /// 入力プラグインの読み込み時の設定を適用します。
        /// GetLoadSettingで取得したViewを表示後、ユーザーによってOKが選択されたときに呼び出されます。
        /// </summary>
        /// <param name="setting">GetLoadSettingで取得したViewのDataContext</param>
        /// <returns>設定を反映し、ファイルの読み込みが完了した場合はtrue、そうでない場合はfalse</returns>
        bool ApplyLoadSetting(object? setting) => true;
    }
}

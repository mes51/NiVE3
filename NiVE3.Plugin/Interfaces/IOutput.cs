using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    public interface IOutput : IDisposable
    {
        /// <summary>
        /// 出力プラグインのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// 出力プラグインの設定画面を表示するためのウインドウを取得します。
        /// OutputMetadataAttribute.HasSettingViewがtrueの時のみ、Loadメソッド呼び出し後に呼ばれます。
        /// </summary>
        /// <param name="startTime">書き出しを開始するコンポジション内の時間</param>
        /// <param name="duration">書き出す長さ</param>
        /// <param name="frameRate">フレームレート</param>
        /// <param name="size">書き出す画像のサイズ。音声のみの場合はnull</param>
        /// <param name="outputSources">書き出すソースの種類</param>
        /// <returns>出力プラグインの設定画面のView。ファイルによって設定画面が存在しない場合はnull</returns>
        FrameworkElement? GetOutputSetting(double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources) => null;

        /// <summary>
        /// 現在の出力プラグインの状態を表すデータをシリアル化可能な状態で取得します
        /// </summary>
        /// <returns>出力プラグインの状態を表すシリアル化可能なobject</returns>
        object? SaveData() => null;

        /// <summary>
        /// 出力プラグインの状態を読み込みます
        /// </summary>
        /// <param name="data">読み込む出力プラグインの状態を表すobject</param>
        /// <returns>プラグインの初期化、およびファイルの読み込みが完了した場合はtrue、そうでない場合はfalse</returns>
        bool LoadData(object? data) => true;

        /// <summary>
        /// 出力プラグインの読み込み時の設定を適用します。
        /// GetOutputSettingで取得したViewを表示後、ユーザーによってOKが選択されたときに呼び出されます。
        /// </summary>
        /// <param name="setting">GetOutputSettingで取得したViewのDataContext</param>
        bool ApplyOutputSetting(object? setting) => false;

        /// <summary>
        /// 現在のレンダリング設定で出力するファイルのパスを加工します
        /// </summary>
        /// <param name="baseFilePath">元となるファイルパス</param>
        /// <returns>設定に合わせ、拡張子やファイル名が変更されたファイルパス</returns>
        string ProcessOutputFilePath(string baseFilePath);

        /// <summary>
        /// 出力処理を開始します。
        /// </summary>
        /// <param name="filePath">出力先のファイルパス</param>
        /// <param name="startTime">書き出しを開始するコンポジション内の時間</param>
        /// <param name="duration">書き出す長さ</param>
        /// <param name="frameRate">フレームレート</param>
        /// <param name="size">書き出す画像のサイズ。音声のみの場合はnull</param>
        /// <param name="outputSources">書き出すソースの種類</param>
        void BeginOutput(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources);

        /// <summary>
        /// 書き出し処理を終了します。ユーザーが中断した場合など、すべてのフレームが書き出される前に呼び出されることがあります。
        /// </summary>
        void EndOutput();

        /// <summary>
        /// 出力のパスを開始します
        /// </summary>
        /// <param name="pass">何番目のパスか</param>
        void BeginPass(int pass);

        /// <summary>
        /// 現在のパスを終了します。
        /// </summary>
        void EndPass();

        /// <summary>
        /// 出力時、エンコードなどで必要なパスの回数を取得します。BeginOutput呼び出し後に呼び出されます。
        /// </summary>
        /// <returns>出力に必要なパスの回数</returns>
        int GetPassCount();

        /// <summary>
        /// 画像を書き出します。
        /// </summary>
        /// <param name="pass">現在のパス</param>
        /// <param name="time">書き出す画像のコンポジション内の時間</param>
        /// <param name="image">書き出す画像</param>
        /// <param name="useGpu">GPUを使用するかどうか。この値は書き出し前の画像の処理でGPUを使用するかどうかを表します</param>
        void ProcessFrame(int pass, double time, NImage image, bool useGpu);

        /// <summary>
        /// 音声を書き出します。
        /// </summary>
        /// <param name="audio">書き出す音声のデータ</param>
        void ProcessAudio(float[] audio);
    }
}

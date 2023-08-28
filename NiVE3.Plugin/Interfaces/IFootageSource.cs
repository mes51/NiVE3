using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Image;

namespace NiVE3.Plugin.Interfaces
{
    public interface IFootageSource
    {
        /// <summary>
        /// 読み込んだファイル内でのソースを表すID。同一ファイル内での重複不可
        /// </summary>
        public string SourceId { get; }

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
        /// このそーすのメディアの形式
        /// </summary>
        SourceType SourceType { get; }

        /// <summary>
        /// 画像を読み込みます
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="toGpu">GPU上に直接読み込む場合はtrue、CPU上に読み込む場合はfalse</param>
        /// <returns>読み込んだ画像を表すNImage</returns>
        // TODO: Acceleratorをラップしたものを渡す
        NImage Read(double time, bool toGpu);
    }

    /// <summary>
    /// フッテージのメディアの形式を表します。
    /// </summary>
    [Flags]
    public enum SourceType
    {
        /// <summary>
        /// なし
        /// </summary>
        None = 0b000,
        /// <summary>
        /// 画像
        /// </summary>
        Image = 0b001,
        /// <summary>
        /// 音声
        /// </summary>
        Audio = 0b010,
        /// <summary>
        /// ビデオ(音声なし)
        /// </summary>
        Video = 0b100,
        /// <summary>
        /// ビデオ+音声
        /// </summary>
        VideoAndAudio = Video | Audio
    }
}

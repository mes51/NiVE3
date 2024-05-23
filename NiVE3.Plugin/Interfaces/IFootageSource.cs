using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    public interface IFootageSource
    {
        /// <summary>
        /// NiVEがサポートする音声のサンプリングレート
        /// </summary>
        public const int SupportAudioSamplingRate = 48000;

        /// <summary>
        /// NiVEがサポートする音声のチャンネル数
        /// </summary>
        public const int SupportChannelCount = 2;

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
        /// このソースのメディアの形式
        /// </summary>
        SourceType SourceType { get; }

        /// <summary>
        /// 画像を読み込みます
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの比率</param>
        /// <param name="toGpu">GPU上に直接読み込む場合はtrue、CPU上に読み込む場合はfalse</param>
        /// <returns>読み込んだ画像を表すNImage</returns>
        // TODO: Acceleratorをラップしたものを渡す
        NImage ReadFrame(double time, double downSamplingRate, bool toGpu);

        /// <summary>
        /// 音声を読み込みます。読み込む音声は48kHz 2ch 32bit浮動小数点wavファイルと同じフォーマットである必要があります。
        /// </summary>
        /// <param name="time">読み込み開始する時間</param>
        /// <param name="length">読み込む長さ</param>
        /// <returns></returns>
        float[] ReadAudio(double time, double length);
    }

    public interface ICustomizableFootageSource : IFootageSource
    {
        /// <summary>
        /// ソースの読み込み時に使用するオプションを表すプロパティを取得します
        /// </summary>
        /// <returns>オプションを表すプロパティ</returns>
        PropertyBase[] GetOptionProperties();

        /// <summary>
        /// 読み込む画像のサイズと位置を計算します
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="compositionWidth">コンポジションの幅</param>
        /// <param name="compositionHeight">コンポジションの高さ</param>
        /// <param name="properties">オプションの値</param>
        /// <returns>読み込む画像の四角形を表すSourceFootageRect</returns>
        SourceFootageRect CalcSize(double time, int compositionWidth, int compositionHeight, PropertyValueGroup properties);

        /// <summary>
        /// 画像を読み込みます
        /// </summary>
        /// <param name="time">読み込むタイミングの時間</param>
        /// <param name="downSamplingRate">ダウンサンプリングの比率</param>
        /// <param name="compositionWidth">コンポジションの幅</param>
        /// <param name="compositionHeight">コンポジションの高さ</param>
        /// <param name="properties">オプションの値</param>
        /// <param name="imageInterpolationQuality">画像のレンダリング時の補間方法</param>
        /// <param name="toGpu">GPU上に直接読み込む場合はtrue、CPU上に読み込む場合はfalse</param>
        /// <returns>読み込んだ画像を表すNImage</returns>
        // TODO: Acceleratorをラップしたものを渡す
        NImage ReadFrame(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu);
    }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.Image
{
    /// <summary>
    /// GPUから参照可能なマスク画像データを表します
    /// BGRA上のAに相当する値のみを格納します。
    /// </summary>
    public class GPURasterizedMaskImage : RasterizedMaskImage
    {
        /// <summary>
        /// マスク画像データ
        /// </summary>
        public ReadWriteBuffer<float> Data { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        public GPURasterizedMaskImage(int width, int height, GraphicsDevice device) : base(width, height)
        {
            Data = device.AllocateReadWriteBuffer<float>(width * height);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        /// <param name="data">元となるマスク画像のデータ</param>
        /// <exception cref="ArgumentOutOfRangeException">データのサイズが幅*高さ未満です</exception>
        public GPURasterizedMaskImage(int width, int height, GraphicsDevice device, ReadOnlySpan<float> data) : base(width, height)
        {
            if (data.Length < width * height)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            Data = device.AllocateReadWriteBuffer(data[..(width * height)]);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        /// <param name="alpha">最初のアルファ値</param>
        public GPURasterizedMaskImage(int width, int height, GraphicsDevice device, float alpha) : base(width, height)
        {
            var length = width * height;
            var data = ArrayPool<float>.Shared.Rent(length);
            data.AsSpan(0, length).Fill(alpha);
            Data = device.AllocateReadWriteBuffer(data[..length]);
            ArrayPool<float>.Shared.Return(data);
        }

        /// <summary>
        /// マスク画像データを取得します
        /// </summary>
        /// <returns>取得したマスク画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。</returns>
        public override float[] GetData()
        {
            var data = ArrayPool<float>.Shared.Rent(Data.Length);
            Data.CopyTo(data[..Data.Length]);
            return data;
        }

        /// <summary>
        /// マスク画像をCPUにコピーして複製します
        /// </summary>
        /// <returns>複製されたマスク画像</returns>
        public override RasterizedMaskImage Copy()
        {
            return CopyToCpu();
        }

        /// <summary>
        /// マスク画像データをCPU側にコピーしたRasterizedMaskImageを新たに生成します
        /// </summary>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>生成されたManagedRasterizedMaskImage</returns>
        public ManagedRasterizedMaskImage CopyToCpu()
        {
            var result = new ManagedRasterizedMaskImage(Width, Height, false);
            Data.CopyTo(result.Data[..Data.Length]);
            return result;
        }

        /// <summary>
        /// GPURasterizedMaskImage間でマスク画像をコピーします
        /// </summary>
        /// <param name="image">コピー先のGPURasterizedMaskImage</param>
        /// <exception cref="ArgumentOutOfRangeException">画像サイズが異なります</exception>
        public void CopyTo(GPURasterizedMaskImage image)
        {
            if (Width != image.Width || Height != image.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(image));
            }

            Data.CopyTo(image.Data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Data.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

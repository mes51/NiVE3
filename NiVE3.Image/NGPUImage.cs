using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.Image
{
    /// <summary>
    /// GPUから参照可能な画像データを表します
    /// </summary>
    public class NGPUImage : NImage
    {
        public ReadWriteBuffer<Vector4> Data { get; }

        public NGPUImage(int width, int height, GraphicsDevice device) : base(width, height)
        {
            Data = device.AllocateReadWriteBuffer<Vector4>(width * height);
        }

        public NGPUImage(int width, int height, GraphicsDevice device, ReadOnlySpan<Vector4> cpuData) : base(width, height)
        {
            Data = device.AllocateReadWriteBuffer(cpuData[..(width * height)]);
        }

        /// <summary>
        /// 画像をCPUにコピーして複製します
        /// </summary>
        /// <returns>複製された画像</returns>
        public override NImage Copy()
        {
            return CopyToCpu();
        }

        /// <summary>
        /// 画像データを取得します
        /// </summary>
        /// <returns>取得した画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。</returns>
        public override Vector4[] GetData()
        {
            var result = ArrayPool<Vector4>.Shared.Rent(DataLength);
            Data.CopyTo(result.AsSpan(0, DataLength));

            return result;
        }

        /// <summary>
        /// 画像データをCPU側にコピーしたNImageを新たに生成します
        /// </summary>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>生成されたNManagedImage</returns>
        public NManagedImage CopyToCpu(bool needClear = false)
        {
            var result = new NManagedImage(Width, Height, needClear);
            Data.CopyTo(result.GetDataSpan());

            return result;
        }

        /// <summary>
        /// NGPUImage間で画像をコピーします
        /// </summary>
        /// <param name="image">コピー先のNGPUImage</param>
        /// <exception cref="ArgumentOutOfRangeException">画像サイズが異なります</exception>
        public void CopyTo(NGPUImage image)
        {
            if (Width != image.Width || Height != image.Height)
            {
                throw new ArgumentOutOfRangeException("different target image size");
            }

            Data.CopyTo(image.Data);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Data.Dispose();
        }
    }
}

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
    /// CPUから参照可能な画像データを表します
    /// </summary>
    public sealed class NManagedImage : NImage
    {
        /// <summary>
        /// 画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。
        /// </summary>
        public Vector4[] Data { get; }

        /// <summary>
        /// NManagedImageの新しいインスタンスを生成します。配列はArrayPoolから取得します
        /// </summary>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        public NManagedImage(int width, int height, bool needClear = true) : base(width, height)
        {
            var length = width * height;
            Data = ArrayPool<Vector4>.Shared.Rent(length);
            if (needClear)
            {
                Data.AsSpan(0, length).Clear();
            }
        }

        /// <summary>
        /// NManagedImageの新しいインスタンスを生成します。配列はArrayPoolから取得します
        /// </summary>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="color">初期の色</param>
        public NManagedImage(int width, int height, Vector4 color) : base(width, height)
        {
            var length = width * height;
            Data = ArrayPool<Vector4>.Shared.Rent(length);
            Data.AsSpan(0, length).Fill(color);
        }

        /// <summary>
        /// 画像データを取得します
        /// </summary>
        /// <returns>取得した画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。</returns>
        public override Vector4[] GetData()
        {
            return Data;
        }

        /// <summary>
        /// 画像のデータを取得します。長さはDataLengthでスライスされます
        /// </summary>
        /// <returns>取得した画像データ</returns>
        public Span<Vector4> GetDataSpan()
        {
            return Data.AsSpan(0, DataLength);
        }

        /// <summary>
        /// 画像を複製します
        /// </summary>
        /// <returns>複製された画像</returns>
        public override NImage Copy()
        {
            var result = new NManagedImage(Width, Height, false)
            {
                Origin = Origin
            };
            Data.AsSpan(0, DataLength).CopyTo(result.Data);
            return result;
        }

        /// <summary>
        /// 画像データをGPU側にコピーしたNImageを新たに生成します
        /// </summary>
        /// <param name="device">GPUのデバイス</param>
        /// <returns>生成されたNGPUImage</returns>
        public NGPUImage CopyToGpu(GraphicsDevice device)
        {
            return new NGPUImage(Width, Height, device, GetDataSpan())
            {
                Origin = Origin
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ArrayPool<Vector4>.Shared.Return(Data);
            }
            base.Dispose(disposing);
        }
    }
}

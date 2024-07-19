using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
        /// <summary>
        /// 画像データ
        /// </summary>
        public ReadWriteBuffer<Float4> Data { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        public NGPUImage(int width, int height, GraphicsDevice device) : base(width, height)
        {
            Data = device.AllocateReadWriteBuffer<Float4>(width * height);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        /// <param name="data">元となる画像データ</param>
        /// <exception cref="ArgumentOutOfRangeException">データのサイズが幅*高さ未満です</exception>
        public NGPUImage(int width, int height, GraphicsDevice device, ReadOnlySpan<Vector4> data) : base(width, height)
        {
            if (data.Length < width * height)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            var float4Data = MemoryMarshal.Cast<Vector4, Float4>(data);
            Data = device.AllocateReadWriteBuffer(float4Data[..(width * height)]);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="device">GPUのデバイス</param>
        /// <param name="color">初期の各ピクセルの色</param>
        public NGPUImage(int width, int height, GraphicsDevice device, Vector4 color) : this(width, height, device)
        {
            Data = device.AllocateReadWriteBuffer<Float4>(width * height);
            using var context = device.CreateComputeContext();
            context.For(width, height, new ClearImage(Data, width, color));
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
            var float4Data = MemoryMarshal.Cast<Vector4, Float4>(result);
            Data.CopyTo(float4Data[..DataLength]);

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
            var float4Data = MemoryMarshal.Cast<Vector4, Float4>(result.GetDataSpan());
            Data.CopyTo(float4Data);

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
                throw new ArgumentOutOfRangeException(nameof(image));
            }

            Data.CopyTo(image.Data);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                Data.Dispose();
            }
            catch { } // NOTE: 生成に失敗した場合 null になることがある
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ClearImage(ReadWriteBuffer<Float4> image, int width, Float4 color) : IComputeShader
    {
        public void Execute()
        {
            image[ThreadIds.Y * width + ThreadIds.X] = color;
        }
    }
}

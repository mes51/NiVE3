using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;

namespace NiVE3.Plugin.Image
{
    /// <summary>
    /// Cudaから参照可能な画像データを表します
    /// </summary>
    public sealed class NCudaImage : NImage
    {
        /// <summary>
        /// GPU上の画像データ
        /// </summary>
        public MemoryBuffer1D<float, Stride1D.Dense> Data { get; }

        Accelerator Accelerator { get; }

        /// <summary>
        /// NCudaImageの新しいインスタンスを生成します
        /// </summary>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="accelerator">バッファを確保する先のAccelerator</param>
        public NCudaImage(int width, int height, Accelerator accelerator) : base(width, height)
        {
            Accelerator = accelerator;
            Data = accelerator.Allocate1D<float>(width * height * 4);
        }

        /// <summary>
        /// CPU側にコピーした画像データを取得します
        /// </summary>
        /// <returns>取得した画像データ</returns>
        public override float[] GetData()
        {
            return GetData(false);
        }

        /// <summary>
        /// 画像を複製します
        /// </summary>
        /// <returns>複製した画像</returns>
        public override NImage Copy()
        {
            var result = new NCudaImage(Width, Height, Accelerator);
            Data.CopyTo(result.Data);
            return result;
        }

        /// <summary>
        /// CPU側にコピーした画像データを取得します
        /// </summary>
        /// <param name="rentFromArrayPool">配列をArrayPoolから取得するかどうか</param>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>取得した画像データ</returns>
        public float[] GetData(bool rentFromArrayPool, bool needClear = false)
        {
            var result = rentFromArrayPool ? ArrayPool<float>.Shared.Rent(DataLength) : new float[DataLength];
            if (rentFromArrayPool && needClear)
            {
                result.AsSpan().Fill(0.0F);
            }
            var handle = GCHandle.Alloc(result, GCHandleType.Pinned);

            using (var cpuBuffer = CPUMemoryBuffer.Create(Accelerator, handle.AddrOfPinnedObject(), DataLength, sizeof(float)))
            {
                Data.View.CopyTo(cpuBuffer.AsArrayView<float>(0, DataLength));
                Accelerator.Synchronize();
            }

            handle.Free();
            return result;
        }

        /// <summary>
        /// 画像データをCPU側にコピーしたNImageを新たに生成します
        /// </summary>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        /// <returns>生成されたNImage</returns>
        public NImage CopyToCpu(bool needClear = false)
        {
            var result = new NManagedImage(Width, Height, needClear);
            var handle = GCHandle.Alloc(result.Data, GCHandleType.Pinned);

            using (var cpuBuffer = CPUMemoryBuffer.Create(Accelerator, handle.AddrOfPinnedObject(), result.DataLength, sizeof(float)))
            {
                Data.View.CopyTo(cpuBuffer.AsArrayView<float>(0, result.DataLength));
                Accelerator.Synchronize();
            }

            handle.Free();
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            Data.Dispose();
            base.Dispose(disposing);
        }
    }
}

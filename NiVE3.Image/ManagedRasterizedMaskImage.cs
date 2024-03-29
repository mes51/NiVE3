using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Image
{
    /// <summary>
    /// CPUから参照可能なマスク画像データを表します
    /// BGRA上のAに相当する値のみを格納します。
    /// </summary>
    public class ManagedRasterizedMaskImage : RasterizedMaskImage
    {
        /// <summary>
        /// マスク画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。
        /// </summary>
        public float[] Data { get; }

        public ManagedRasterizedMaskImage(int width, int height, bool needClear = true) : base(width, height)
        {
            var length = width * height;
            Data = ArrayPool<float>.Shared.Rent(length);
            if (needClear)
            {
                Data.AsSpan().Clear();
            }
        }

        /// <summary>
        /// マスク画像データを取得します
        /// </summary>
        /// <returns>取得したマスク画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。</returns>
        public override float[] GetData()
        {
            return Data;
        }

        /// <summary>
        /// マスク画像のデータを取得します。長さはDataLengthでスライスされます
        /// </summary>
        /// <returns>取得したマスク画像データ</returns>
        public Span<float> GetDataSpan()
        {
            return Data.AsSpan(0, DataLength);
        }

        /// <summary>
        /// マスク画像を複製します
        /// </summary>
        /// <returns>複製されたマスク画像</returns>
        public override RasterizedMaskImage Copy()
        {
            var result = new ManagedRasterizedMaskImage(Width, Height, false);
            Data.AsSpan(0, DataLength).CopyTo(result.GetDataSpan()[..DataLength]);
            result.Origin = Origin;
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ArrayPool<float>.Shared.Return(Data);
            }
            base.Dispose(disposing);
        }
    }
}

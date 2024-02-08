using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public float[] Data { get; }

        /// <summary>
        /// NManagedImageの新しいインスタンスを生成します。配列はArrayPoolから取得します
        /// </summary>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="needClear">ArrayPoolから取得した配列の0クリアが必要かどうか</param>
        public NManagedImage(int width, int height, bool needClear = true) : base(width, height)
        {
            var length = width * height * 4;
            Data = ArrayPool<float>.Shared.Rent(length);
            if (needClear)
            {
                Data.AsSpan().Fill(0.0F);
            }
        }

        /// <summary>
        /// 画像データを取得します
        /// </summary>
        /// <returns>取得した画像データ。ArrayPoolから取得した配列のため、画像サイズ以上の長さの可能性があります。</returns>
        public override float[] GetData()
        {
            return Data;
        }

        /// <summary>
        /// 画像のデータを取得します。長さはDataLengthでスライスされます
        /// </summary>
        /// <returns>取得した画像データ</returns>
        public Span<float> GetDataSpan()
        {
            return Data.AsSpan(0, DataLength);
        }

        /// <summary>
        /// 画像を複製します
        /// </summary>
        /// <returns>複製された画像</returns>
        public override NImage Copy()
        {
            var result = new NManagedImage(Width, Height, false);
            Data.AsSpan(0, DataLength).CopyTo(result.Data);
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

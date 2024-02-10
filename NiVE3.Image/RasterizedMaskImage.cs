using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Image
{
    /// <summary>
    /// ラスタライズ済みのマスク画像を表します。
    /// BGRA上のAに相当する値のみを格納します。
    /// </summary>
    public abstract class RasterizedMaskImage : IDisposable
    {
        /// <summary>
        /// NiVEで扱うことが可能な最大ピクセル数
        /// NImage.MaxPixelCountと同じピクセル数で制限されています
        /// </summary>
        public const int MaxPixelCount = NImage.MaxPixelCount;

        /// <summary>
        /// マスク画像の幅
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// マスク画像の高さ
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// マスク画像データの最小サイズ。GetDataで取得するデータの長さはこれ以上であることが保証されます
        /// </summary>
        public virtual int DataLength => Width * Height;

        /// <summary>
        /// 画像の(0, 0)となる位置を表す値
        /// </summary>
        public Vector2d Origin { get; set; }

        /// <summary>
        /// このインスタンスが破棄済みかどうか
        /// </summary>
        protected bool Disposed { get; private set; }

        internal RasterizedMaskImage(int width, int height)
        {
            if (width * height > MaxPixelCount)
            {
                throw new ArgumentException("画像のピクセル数がMaxPixelCountを超えています");
            }

            Width = width;
            Height = height;
        }

        /// <summary>
        /// マスク画像のデータを取得します。必要な場合は配列への変換処理が実行されます
        /// </summary>
        /// <returns>取得したマスク画像データ</returns>
        public abstract float[] GetData();

        /// <summary>
        /// マスク画像を複製します
        /// </summary>
        /// <returns>複製されたマスク画像</returns>
        public abstract RasterizedMaskImage Copy();

        /// <summary>
        /// アンマネージ リソースの解放またはリセットに関連付けられているアプリケーション定義のタスクを実行します
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Dispose(true);
                Disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// RasterizedMaskImageによって使用されているアンマネージド リソースを解放し、オプションでマネージド リソースも解放します
        /// </summary>
        /// <param name="disposing">マネージド リソースとアンマネージド リソースの両方を解放する場合は true。アンマネージド リソースだけを解放する場合は false</param>
        protected virtual void Dispose(bool disposing) { }

        ~RasterizedMaskImage()
        {
            Dispose();
        }
    }
}

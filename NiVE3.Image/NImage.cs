using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Image
{
    /// <summary>
    /// NiVE上での画像データを表します
    /// </summary>
    public abstract class NImage : IDisposable
    {
        /// <summary>
        /// NiVEで扱うことが可能な最大ピクセル数
        /// Array.MaxLength によって制限されています
        /// </summary>
        public const int MaxPixelCount = 46340 * 46340; // ≈ Math.Sqrt(Array.MaxLength)

        /// <summary>
        /// 画像の幅
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 画像の高さ
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 画像の(0, 0)となる位置を表す値
        /// </summary>
        public Vector2d Origin { get; set; }

        /// <summary>
        /// 画像データの最小サイズ。GetDataで取得するデータの長さはこれ以上であることが保証されます
        /// </summary>
        public virtual int DataLength => Width * Height;

        /// <summary>
        /// このインスタンスが破棄済みかどうか
        /// </summary>
        protected bool Disposed { get; private set; }

        internal NImage(int width, int height)
        {
            if (width * height > MaxPixelCount)
            {
                throw new ArgumentException("画像のピクセル数がMaxPixelCountを超えています");
            }

            Width = width;
            Height = height;
        }

        /// <summary>
        /// 画像のデータを取得します。必要な場合は配列への変換処理が実行されます
        /// </summary>
        /// <returns>取得した画像データ</returns>
        public abstract Vector4[] GetData();

        /// <summary>
        /// 画像を複製します
        /// </summary>
        /// <returns>複製された画像</returns>
        public abstract NImage Copy();

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
        /// NImageによって使用されているアンマネージド リソースを解放し、オプションでマネージド リソースも解放します
        /// </summary>
        /// <param name="disposing">マネージド リソースとアンマネージド リソースの両方を解放する場合は true。アンマネージド リソースだけを解放する場合は false</param>
        protected virtual void Dispose(bool disposing) { }

        ~NImage()
        {
            Dispose(false);
        }
    }
}

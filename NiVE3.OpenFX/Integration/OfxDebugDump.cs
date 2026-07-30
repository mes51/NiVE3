using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Integration
{
#if NIVE3_OFX_DIAGNOSTICS
    /// <summary>
    /// レンダリング内容の診断用ダンプ (DebugDiagnostics 構成でのみコンパイルされます)
    /// 環境変数 NIVE3_OFX_DEBUG_DIR にディレクトリを設定すると、レンダリングごとの入出力画像を BMP で保存します
    /// </summary>
    static class OfxDebugDump
    {
        static long Sequence;

        public static string? Directory { get; } = InitializeDirectory();

        static string? InitializeDirectory()
        {
            var directory = Environment.GetEnvironmentVariable("NIVE3_OFX_DEBUG_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }
            try
            {
                System.IO.Directory.CreateDirectory(directory);
                return directory;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 画像を BMP として保存します (診断用)
        /// </summary>
        /// <param name="tag">ファイル名に含めるタグ</param>
        /// <param name="pixels">BGRA (上から下) の画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <returns>連番 (入出力の対応付け用)</returns>
        public static long Save(string tag, Vector4[] pixels, int width, int height, long? sequence = null)
        {
            var directory = Directory;
            var number = sequence ?? Interlocked.Increment(ref Sequence);
            if (directory == null)
            {
                return number;
            }

            try
            {
                var path = Path.Combine(directory, $"{number:D5}_{tag}_{width}x{height}.bmp");
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(stream);

                var dataSize = width * height * 4;
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write(54 + dataSize);
                writer.Write(0);
                writer.Write(54);
                writer.Write(40);
                writer.Write(width);
                writer.Write(-height);
                writer.Write((short)1);
                writer.Write((short)32);
                writer.Write(0);
                writer.Write(dataSize);
                writer.Write(2835);
                writer.Write(2835);
                writer.Write(0);
                writer.Write(0);

                for (var i = 0; i < width * height; i++)
                {
                    var pixel = pixels[i];
                    writer.Write((byte)Math.Clamp(pixel.X * 255.0F + 0.5F, 0.0F, 255.0F));
                    writer.Write((byte)Math.Clamp(pixel.Y * 255.0F + 0.5F, 0.0F, 255.0F));
                    writer.Write((byte)Math.Clamp(pixel.Z * 255.0F + 0.5F, 0.0F, 255.0F));
                    writer.Write((byte)Math.Clamp(pixel.W * 255.0F + 0.5F, 0.0F, 255.0F));
                }
            }
            catch
            {
                // 診断用のため失敗は無視する
            }
            return number;
        }
    }
#endif
}

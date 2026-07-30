using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Discovery
{
    /// <summary>
    /// OFX プラグインバンドルの探索
    /// </summary>
    public static class OfxDiscovery
    {
        /// <summary>
        /// OFX 標準のプラグイン配置ディレクトリの一覧を取得します
        /// </summary>
        public static IReadOnlyList<string> GetStandardPluginDirectories()
        {
            var directories = new List<string>();

            // Windows の標準パス: C:\Program Files\Common Files\OFX\Plugins
            var commonFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            if (!string.IsNullOrEmpty(commonFiles))
            {
                directories.Add(Path.Combine(commonFiles, "OFX", "Plugins"));
            }

            // 環境変数 OFX_PLUGIN_PATH (セミコロン区切り)
            var envPath = Environment.GetEnvironmentVariable("OFX_PLUGIN_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                directories.AddRange(envPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            return directories;
        }

        /// <summary>
        /// 指定したディレクトリ以下から .ofx バイナリのパスを列挙します
        /// バンドル形式 (*.ofx.bundle/Contents/Win64/*.ofx) と直接置かれた .ofx の両方に対応します
        /// </summary>
        /// <param name="directory">探索するディレクトリ</param>
        /// <returns>見つかった .ofx ファイルのパスの一覧</returns>
        public static IReadOnlyList<string> FindOfxBinaries(string directory)
        {
            var result = new List<string>();
            if (!Directory.Exists(directory))
            {
                return result;
            }

            foreach (var bundle in Directory.EnumerateDirectories(directory, "*.ofx.bundle", SearchOption.AllDirectories))
            {
                var win64 = Path.Combine(bundle, "Contents", "Win64");
                if (Directory.Exists(win64))
                {
                    result.AddRange(Directory.EnumerateFiles(win64, "*.ofx", SearchOption.TopDirectoryOnly));
                }
            }

            // バンドル形式でない .ofx (規格外だが検証用に受け付ける)
            foreach (var file in Directory.EnumerateFiles(directory, "*.ofx", SearchOption.TopDirectoryOnly))
            {
                result.Add(file);
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}

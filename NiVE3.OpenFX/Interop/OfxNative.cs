using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Interop
{
    /// <summary>
    /// ofxCore.h の OfxPlugin 構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OfxPluginNative
    {
        public byte* PluginApi;
        public int ApiVersion;
        public byte* PluginIdentifier;
        public uint PluginVersionMajor;
        public uint PluginVersionMinor;
        public delegate* unmanaged[Cdecl]<OfxHostNative*, void> SetHost;
        public delegate* unmanaged[Cdecl]<byte*, void*, nint, nint, OfxStatus> MainEntry;
    }

    /// <summary>
    /// ofxCore.h の OfxHost 構造体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OfxHostNative
    {
        public nint HostProps;
        public delegate* unmanaged[Cdecl]<nint, byte*, int, void*> FetchSuite;
    }

    /// <summary>
    /// ロード済みの .ofx バイナリを表します
    /// </summary>
    public sealed unsafe class OfxBinary : IDisposable
    {
        public string FilePath { get; }

        public IReadOnlyList<OfxPluginInfo> Plugins { get; }

        nint Library { get; set; }

        OfxBinary(string filePath, nint library, IReadOnlyList<OfxPluginInfo> plugins)
        {
            FilePath = filePath;
            Library = library;
            Plugins = plugins;
        }

        /// <summary>
        /// .ofx バイナリをロードし、含まれるプラグインを列挙します
        /// </summary>
        /// <param name="filePath">.ofx ファイルのパス</param>
        /// <returns>ロードされたバイナリ</returns>
        public static OfxBinary Load(string filePath)
        {
            var library = NativeLibrary.Load(filePath);
            try
            {
                if (!NativeLibrary.TryGetExport(library, "OfxGetNumberOfPlugins", out var getNumberOfPlugins) ||
                    !NativeLibrary.TryGetExport(library, "OfxGetPlugin", out var getPlugin))
                {
                    throw new InvalidOperationException($"OfxGetNumberOfPlugins/OfxGetPlugin がエクスポートされていません: {filePath}");
                }

                var count = ((delegate* unmanaged[Cdecl]<int>)getNumberOfPlugins)();
                var plugins = new List<OfxPluginInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    var plugin = ((delegate* unmanaged[Cdecl]<int, OfxPluginNative*>)getPlugin)(i);
                    if (plugin != null)
                    {
                        plugins.Add(new OfxPluginInfo(plugin, i, filePath));
                    }
                }

                return new OfxBinary(filePath, library, plugins);
            }
            catch
            {
                NativeLibrary.Free(library);
                throw;
            }
        }

        public void Dispose()
        {
            if (Library != 0)
            {
                NativeLibrary.Free(Library);
                Library = 0;
            }
        }
    }

    /// <summary>
    /// .ofx バイナリ内の 1 プラグインを表します
    /// </summary>
    public sealed unsafe class OfxPluginInfo
    {
        public int Index { get; }

        public string PluginApi { get; }

        public int ApiVersion { get; }

        public string Identifier { get; }

        public uint VersionMajor { get; }

        public uint VersionMinor { get; }

        public bool IsImageEffect => PluginApi == OfxNames.ImageEffectPluginApi;

        /// <summary>
        /// ロード元の .ofx ファイルのパス
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// kOfxPluginPropFilePath としてプラグインへ公開するパス。
        /// バンドル構成 (*.ofx.bundle/Contents/&lt;arch&gt;/*.ofx) の場合はバンドルディレクトリ
        /// (プラグインはこれに "/Contents/Resources" 等を付加してリソースを参照する)、
        /// それ以外は .ofx のあるディレクトリ
        /// </summary>
        public string BundlePath { get; }

        OfxPluginNative* Plugin { get; }

        internal OfxPluginInfo(OfxPluginNative* plugin, int index, string filePath)
        {
            Plugin = plugin;
            Index = index;
            PluginApi = Marshal.PtrToStringUTF8((nint)plugin->PluginApi) ?? "";
            ApiVersion = plugin->ApiVersion;
            Identifier = Marshal.PtrToStringUTF8((nint)plugin->PluginIdentifier) ?? "";
            VersionMajor = plugin->PluginVersionMajor;
            VersionMinor = plugin->PluginVersionMinor;
            FilePath = filePath;
            BundlePath = GetBundlePath(filePath);
        }

        static string GetBundlePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var contents = directory != null ? Path.GetDirectoryName(directory) : null;
            var bundle = contents != null ? Path.GetDirectoryName(contents) : null;
            if (contents != null && bundle != null &&
                string.Equals(Path.GetFileName(contents), "Contents", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(bundle).EndsWith(".ofx.bundle", StringComparison.OrdinalIgnoreCase))
            {
                return bundle;
            }
            return directory ?? "";
        }

        /// <summary>
        /// プラグインにホスト構造体を渡します
        /// </summary>
        /// <param name="host">ホスト構造体へのポインタ</param>
        public void SetHost(OfxHostNative* host)
        {
            if (Plugin->SetHost != null)
            {
                Plugin->SetHost(host);
            }
        }

        /// <summary>
        /// プラグインのメインエントリポイントを呼び出します
        /// </summary>
        /// <param name="action">アクション名</param>
        /// <param name="handle">アクションの対象のハンドル</param>
        /// <param name="inArgs">入力引数のプロパティセットのハンドル</param>
        /// <param name="outArgs">出力引数のプロパティセットのハンドル</param>
        /// <returns>プラグインが返したステータス</returns>
        public OfxStatus CallAction(string action, nint handle, nint inArgs, nint outArgs)
        {
            if (Plugin->MainEntry == null)
            {
                return OfxStatus.ErrFatal;
            }

            var actionBytes = Encoding.UTF8.GetBytes(action + "\0");
            fixed (byte* actionPtr = actionBytes)
            {
#if NIVE3_OFX_DIAGNOSTICS
                Host.OfxLog.Trace($">> CallAction {action} handle=0x{handle:X}");
                var status = Plugin->MainEntry(actionPtr, (void*)handle, inArgs, outArgs);
                Host.OfxLog.Trace($"<< CallAction {action} -> {status}");
                return status;
#else
                return Plugin->MainEntry(actionPtr, (void*)handle, inArgs, outArgs);
#endif
            }
        }
    }
}

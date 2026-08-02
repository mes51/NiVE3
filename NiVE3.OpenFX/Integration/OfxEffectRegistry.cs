using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Discovery;
using NiVE3.OpenFX.Host;
using NiVE3.OpenFX.Host.GL;
using NiVE3.OpenFX.Interop;
using NiVE3.Plugin.Attributes;

namespace NiVE3.OpenFX.Integration
{
    /// <summary>
    /// 使用可能な OpenFX エフェクトの一覧を管理します
    /// 起動時にプラグインを検出・Describe し、NiVE3 のエフェクトとして公開するための情報を保持します
    /// </summary>
    public sealed class OfxEffectRegistry : IDisposable
    {
        /// <summary>
        /// OFX ホストのランタイム
        /// </summary>
        public OfxHostRuntime Runtime { get; }

        /// <summary>
        /// 使用可能なエフェクトの定義一覧
        /// </summary>
        public IReadOnlyList<OfxEffectDefinition> Definitions => DefinitionList;

        List<OfxEffectDefinition> DefinitionList { get; } = new List<OfxEffectDefinition>();

        Dictionary<Guid, OfxEffectDefinition> DefinitionsByGuid { get; } = new Dictionary<Guid, OfxEffectDefinition>();

        List<OfxBinary> Binaries { get; } = new List<OfxBinary>();

        List<OfxPluginInfo> LoadedPlugins { get; } = new List<OfxPluginInfo>();

        OfxEffectRegistry()
        {
            Runtime = new OfxHostRuntime();
        }

        /// <summary>
        /// 指定したディレクトリ群から OFX プラグインを検出してレジストリを構築します
        /// </summary>
        /// <param name="directories">検索するディレクトリの一覧</param>
        /// <returns>構築されたレジストリ</returns>
        public static OfxEffectRegistry Load(IEnumerable<string> directories)
        {
            var registry = new OfxEffectRegistry();
            foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (var path in OfxDiscovery.FindOfxBinaries(directory))
                {
                    registry.LoadBinary(path);
                }
            }
            return registry;
        }

        void LoadBinary(string path)
        {
            try
            {
                var binary = OfxBinary.Load(path);
                Binaries.Add(binary);

                // 同一識別子のプラグインは最も新しいバージョンのみを使用する
                var plugins = binary.Plugins
                    .Where(p => p.IsImageEffect)
                    .GroupBy(p => p.Identifier)
                    .Select(g => g.OrderByDescending(p => (p.VersionMajor, p.VersionMinor)).First());

                foreach (var plugin in plugins)
                {
                    try
                    {
                        LoadPlugin(binary, plugin);
                    }
                    catch (Exception ex)
                    {
                        OfxLog.Warn($"OFX プラグインの初期化に失敗しました: {plugin.Identifier}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OfxLog.Warn($"OFX バイナリのロードに失敗しました: {path}: {ex.Message}");
            }
        }

        void LoadPlugin(OfxBinary binary, OfxPluginInfo plugin)
        {
            var loadStatus = Runtime.LoadPlugin(plugin);
            if (loadStatus is not (OfxStatus.OK or OfxStatus.ReplyDefault))
            {
                OfxLog.Warn($"OFX プラグインの Load に失敗しました: {plugin.Identifier}: {loadStatus}");
                return;
            }
            LoadedPlugins.Add(plugin);

            var (descriptor, describeStatus) = Runtime.Describe(plugin);
            if (describeStatus is not (OfxStatus.OK or OfxStatus.ReplyDefault))
            {
                OfxLog.Warn($"OFX プラグインの Describe に失敗しました: {plugin.Identifier}: {describeStatus}");
                descriptor.Dispose();
                return;
            }

            // Filter (または Filter 相当として扱える General) と float 深度への対応が必須
            var contexts = ReadStrings(descriptor.Properties, OfxNames.ImageEffectPropSupportedContexts);
            var context = contexts.Contains(OfxNames.ContextFilter) ? OfxNames.ContextFilter
                : contexts.Contains(OfxNames.ContextGeneral) ? OfxNames.ContextGeneral
                : null;
            var depths = ReadStrings(descriptor.Properties, OfxNames.ImageEffectPropSupportedPixelDepths);
            if (context == null || !depths.Contains(OfxNames.BitDepthFloat))
            {
                OfxLog.Info($"OFX プラグインは非対応のためスキップします: {plugin.Identifier} (contexts: {string.Join(",", contexts)}, depths: {string.Join(",", depths)})");
                descriptor.Dispose();
                return;
            }

            var glSupport = descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPropOpenGLRenderSupported, 0) as string;
            var clSupport = descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPropOpenCLRenderSupported, 0) as string;
            var cudaSupport = descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPropCudaRenderSupported, 0) as string;
            var cudaStreamSupport = descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPropCudaStreamSupported, 0) as string;
            var supportsGl = glSupport is "true" or "needed" && GlContextManager.Shared != null;
            var supportsCl = clSupport is "true" && Host.CL.ClContextManager.Shared != null;
            var supportsCuda = cudaSupport is "true" && Host.Cuda.CudaContextManager.Shared != null;

            // CPU レンダリング不可 (1.5.1) を宣言していて、使用可能な GPU API もないプラグインは除外する
            var cpuSupport = descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPropCPURenderSupported, 0) as string;
            if (cpuSupport == "false" && !supportsGl && !supportsCl && !supportsCuda)
            {
                OfxLog.Info($"CPU レンダリング不可を宣言しており、使用可能な GPU レンダリング API もないためスキップします: {plugin.Identifier}");
                descriptor.Dispose();
                return;
            }

            var metadata = new OfxEffectMetadata(
                name: descriptor.Properties.GetOrDefault(OfxNames.PropLabel, 0) as string is { Length: > 0 } label ? label : plugin.Identifier,
                author: plugin.Identifier,
                category: descriptor.Properties.GetOrDefault(OfxNames.ImageEffectPluginPropGrouping, 0) as string is { Length: > 0 } grouping ? grouping : "OpenFX",
                description: descriptor.Properties.GetOrDefault(OfxNames.PropPluginDescription, 0) as string ?? "",
                effectUuid: CreateDeterministicGuid(plugin).ToString(),
                isSupportOpenGLRender: supportsGl,
                isSupportOpenCLRender: supportsCl,
                isSupportCudaRender: supportsCuda,
                isSupportCudaStream: supportsCuda && cudaStreamSupport is "true");

            var definition = new OfxEffectDefinition(this, plugin, descriptor, context, contexts, metadata);
            DefinitionList.Add(definition);
            DefinitionsByGuid[Guid.Parse(metadata.EffectUuid)] = definition;
        }

        /// <summary>
        /// Guid からエフェクト定義を取得します
        /// </summary>
        /// <param name="effectUuid">エフェクトの Guid</param>
        /// <returns>エフェクト定義。存在しない場合は null</returns>
        public OfxEffectDefinition? Find(Guid effectUuid)
        {
            DefinitionsByGuid.TryGetValue(effectUuid, out var definition);
            return definition;
        }

        /// <summary>
        /// OFX プラグインの識別子とメジャーバージョンから決定的な Guid を生成します
        /// (同じプラグインは全ての環境・起動で同じ Guid になり、プロジェクトファイルの保存・読込に使用できます)
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <returns>生成された Guid</returns>
        public static Guid CreateDeterministicGuid(OfxPluginInfo plugin)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes($"nive3-ofx:{plugin.Identifier}:{plugin.VersionMajor}"));
            return new Guid(hash);
        }

        static string[] ReadStrings(PropertySet props, string key)
        {
            return Enumerable.Range(0, props.GetDimension(key))
                .Select(i => props.GetOrDefault(key, i) as string)
                .OfType<string>()
                .ToArray();
        }

        public void Dispose()
        {
            foreach (var definition in DefinitionList)
            {
                definition.Dispose();
            }
            DefinitionList.Clear();
            DefinitionsByGuid.Clear();

            // DLL を解放する前に Unload アクションでプラグインを終了させる
            // (Unload なしで解放すると、プラグインのワーカースレッドが残りクラッシュすることがある)
            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    Runtime.UnloadPlugin(plugin);
                }
                catch (Exception ex)
                {
                    OfxLog.Warn($"Unload に失敗しました: {plugin.Identifier}: {ex.Message}");
                }
            }
            LoadedPlugins.Clear();

            foreach (var binary in Binaries)
            {
                binary.Dispose();
            }
            Binaries.Clear();
            Runtime.Dispose();
        }
    }

    /// <summary>
    /// 1 つの OFX エフェクトの定義 (プラグイン + Describe 結果 + メタデータ)
    /// </summary>
    public sealed class OfxEffectDefinition : IDisposable
    {
        public OfxPluginInfo Plugin { get; }

        public EffectDescriptor MainDescriptor { get; }

        /// <summary>
        /// 使用するコンテキスト (Filter または General)。
        /// General の方が入力クリップが多い場合 (Transition 系の From/To 等) は、
        /// 初回の DescribeInContext 時に General へ切り替わります
        /// </summary>
        public string Context { get; private set; }

        /// <summary>
        /// プラグインが宣言している対応コンテキストの一覧
        /// </summary>
        public IReadOnlyList<string> SupportedContexts { get; }

        public OfxEffectMetadata Metadata { get; }

        internal OfxEffectRegistry Registry { get; }

        EffectDescriptor? ContextDescriptorCache { get; set; }

        object Lock { get; } = new object();

        internal OfxEffectDefinition(OfxEffectRegistry registry, OfxPluginInfo plugin, EffectDescriptor mainDescriptor, string context, IReadOnlyList<string> supportedContexts, OfxEffectMetadata metadata)
        {
            Registry = registry;
            Plugin = plugin;
            MainDescriptor = mainDescriptor;
            Context = context;
            SupportedContexts = supportedContexts;
            Metadata = metadata;
        }

        /// <summary>
        /// このエフェクトのアダプタ (インスタンス) を生成します
        /// </summary>
        /// <returns>生成されたアダプタ</returns>
        public OfxEffectAdapter CreateAdapter()
        {
            return new OfxEffectAdapter(this);
        }

        /// <summary>
        /// DescribeInContext 済みのデスクリプタを取得します (初回アクセス時に実行)
        /// </summary>
        /// <returns>コンテキストのデスクリプタ。失敗した場合は null</returns>
        public EffectDescriptor? GetContextDescriptor()
        {
            lock (Lock)
            {
                if (ContextDescriptorCache == null)
                {
                    var (descriptor, status) = Registry.Runtime.DescribeInContext(Plugin, Context);
                    if (status is not (OfxStatus.OK or OfxStatus.ReplyDefault))
                    {
                        OfxLog.Warn($"DescribeInContext に失敗しました: {Plugin.Identifier}: {status}");
                        descriptor.Dispose();
                        return null;
                    }

                    // General の方が入力クリップが多い場合 (Sapphire Transition の From/To 等) は General を採用する
                    if (Context != OfxNames.ContextGeneral && SupportedContexts.Contains(OfxNames.ContextGeneral))
                    {
                        var (generalDescriptor, generalStatus) = Registry.Runtime.DescribeInContext(Plugin, OfxNames.ContextGeneral);
                        if (generalStatus is OfxStatus.OK or OfxStatus.ReplyDefault &&
                            CountInputClips(generalDescriptor) > CountInputClips(descriptor))
                        {
                            OfxLog.Info($"入力クリップが多いため General コンテキストを使用します: {Plugin.Identifier} " +
                                $"({Context}: {CountInputClips(descriptor)}, General: {CountInputClips(generalDescriptor)})");
                            descriptor.Dispose();
                            descriptor = generalDescriptor;
                            Context = OfxNames.ContextGeneral;
                        }
                        else
                        {
                            generalDescriptor.Dispose();
                        }
                    }

                    ContextDescriptorCache = descriptor;
                }
                return ContextDescriptorCache;
            }
        }

        static int CountInputClips(EffectDescriptor descriptor)
        {
            return descriptor.Clips.Keys.Count(name => name != "Output");
        }

        public void Dispose()
        {
            ContextDescriptorCache?.Dispose();
            ContextDescriptorCache = null;
            MainDescriptor.Dispose();
        }
    }

    /// <summary>
    /// OFX エフェクトの NiVE3 向けメタデータ
    /// </summary>
    public sealed class OfxEffectMetadata : IEffectMetadata
    {
        public string Name { get; }

        public string Author { get; }

        public string Category { get; }

        public string Description { get; }

        public string EffectUuid { get; }

        public bool IsDummyEffect => false;

        /// <summary>
        /// OpenGL レンダリングに対応しているかどうか (プラグインの宣言 + GL コンテキストの有無)
        /// </summary>
        public bool IsSupportOpenGLRender { get; }

        /// <summary>
        /// OpenCL (Buffers) レンダリングに対応しているかどうか (プラグインの宣言 + CL デバイスの有無)
        /// </summary>
        public bool IsSupportOpenCLRender { get; }

        /// <summary>
        /// CUDA レンダリングに対応しているかどうか (プラグインの宣言 + CUDA デバイスの有無)
        /// </summary>
        public bool IsSupportCudaRender { get; }

        /// <summary>
        /// CUDA ストリームに対応しているかどうか (仕様上、両者が対応する場合のみ CudaStream を渡す)
        /// </summary>
        public bool IsSupportCudaStream { get; }

        public bool IsSupportGpu => IsSupportOpenGLRender || IsSupportOpenCLRender || IsSupportCudaRender;

        public bool UseCompositionCamera => false;

        public EffectSupportedSource SupportedSource => EffectSupportedSource.Image;

        internal OfxEffectMetadata(string name, string author, string category, string description, string effectUuid, bool isSupportOpenGLRender, bool isSupportOpenCLRender, bool isSupportCudaRender, bool isSupportCudaStream)
        {
            Name = name;
            Author = author;
            Category = category;
            Description = description;
            EffectUuid = effectUuid;
            IsSupportOpenGLRender = isSupportOpenGLRender;
            IsSupportOpenCLRender = isSupportOpenCLRender;
            IsSupportCudaRender = isSupportCudaRender;
            IsSupportCudaStream = isSupportCudaStream;
        }
    }
}

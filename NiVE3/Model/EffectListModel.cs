using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Discovery;
using NiVE3.OpenFX.Integration;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class EffectListModel : BindableBase
    {
        public IReadOnlyList<IEffectMetadata> EffectMetadatas { get; }

        [ImportMany]
        List<ExportFactory<IEffect, IEffectMetadata>>? Effects { get; set; }

        OfxEffectRegistry? OfxEffects { get; }

        Dictionary<Guid, IEffectMetadata> EffectMetadataDictionary { get; }

        AcceleratorModel AcceleratorModel { get; set; }

        public EffectListModel(AcceleratorModel acceleratorModel)
        {
            var catalog = new DirectoryCatalog(Paths.PluginDirectory);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            AcceleratorModel = acceleratorModel;

            OfxEffects = LoadOfxEffects();

            var metadatas = new List<IEffectMetadata>();
            if (Effects != null)
            {
                metadatas.AddRange(Effects.Select(e => e.Metadata));
            }
            if (OfxEffects != null)
            {
                metadatas.AddRange(OfxEffects.Definitions.Select(d => d.Metadata));
            }

            EffectMetadatas = metadatas;
            EffectMetadataDictionary = new Dictionary<Guid, IEffectMetadata>();
            foreach (var metadata in metadatas)
            {
                // 万一 Guid が衝突した場合は先勝ち (MEF エフェクト優先)
                EffectMetadataDictionary.TryAdd(Guid.Parse(metadata.EffectUuid), metadata);
            }
        }

        static OfxEffectRegistry? LoadOfxEffects()
        {
            try
            {
#if NIVE3_OFX_DIAGNOSTICS
                // OFX ホストの警告 (未対応機能の要求など) をデバッガから確認できるようにする
                NiVE3.OpenFX.Host.OfxLog.Sink ??= message => System.Diagnostics.Trace.WriteLine($"[OFX] {message}");
#endif
                NiVE3.OpenFX.Host.OfxHostCallbacks.MessageHandler ??= ShowOfxMessage;

                var setting = Config.ApplicationSetting.Setting;

                // 依存 DLL の検索ディレクトリを PATH に追加する (Natron 付属プラグインなど向け)
                foreach (var directory in setting.OfxDllDirectories.Where(Directory.Exists))
                {
                    var path = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!path.Split(';').Contains(directory, StringComparer.OrdinalIgnoreCase))
                    {
                        Environment.SetEnvironmentVariable("PATH", $"{directory};{path}");
                    }
                }

                var directories = OfxDiscovery.GetStandardPluginDirectories()
                    .Append(Path.Combine(Paths.PluginDirectory, "OFX"))
                    .Concat(setting.OfxPluginDirectories);
                return OfxEffectRegistry.Load(directories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenFX プラグインの読み込みに失敗しました: {ex}");
                return null;
            }
        }

        static NiVE3.OpenFX.Interop.OfxStatus ShowOfxMessage(string messageType, string message)
        {
            if (messageType == NiVE3.OpenFX.Interop.OfxNames.MessageLog || string.IsNullOrWhiteSpace(message))
            {
                return NiVE3.OpenFX.Interop.OfxStatus.OK;
            }

            var application = System.Windows.Application.Current;
            if (application == null)
            {
                return NiVE3.OpenFX.Interop.OfxStatus.OK;
            }

            // 質問は応答が必要なため同期表示する (レンダリングスレッドから呼ばれることがあるため UI スレッドへ切り替える)
            if (messageType == NiVE3.OpenFX.Interop.OfxNames.MessageQuestion)
            {
                return application.Dispatcher.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show(message, "OpenFX", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    return result == System.Windows.MessageBoxResult.Yes ? NiVE3.OpenFX.Interop.OfxStatus.ReplyYes : NiVE3.OpenFX.Interop.OfxStatus.ReplyNo;
                });
            }

            // 応答不要のメッセージは非同期に表示する
            // (プラグインのアクション実行中にモーダル表示するとメッセージポンプ経由の再入を招くため、
            //  アクション完了後に表示されるようキューへ積んで即座に制御を返す)
            var image = messageType switch
            {
                NiVE3.OpenFX.Interop.OfxNames.MessageFatal or NiVE3.OpenFX.Interop.OfxNames.MessageError => System.Windows.MessageBoxImage.Error,
                NiVE3.OpenFX.Interop.OfxNames.MessageWarning => System.Windows.MessageBoxImage.Warning,
                _ => System.Windows.MessageBoxImage.Information
            };
            application.Dispatcher.BeginInvoke(() => System.Windows.MessageBox.Show(message, "OpenFX", System.Windows.MessageBoxButton.OK, image));
            return NiVE3.OpenFX.Interop.OfxStatus.OK;
        }

        public EffectModel? CreateEffect(Guid effectUuid, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? effectId = null)
        {
            var factory = Effects?.FirstOrDefault(f => Guid.Parse(f.Metadata.EffectUuid) == effectUuid);
            if (factory != null)
            {
                var effect = factory.CreateExport();
                if (factory.Metadata.IsSupportGpu)
                {
                    effect.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
                }
                return new EffectModel(new MefEffectHandle(effect), factory.Metadata, projectModel, compositionModel, layerModel, historyModel, effectId);
            }

            var ofxDefinition = OfxEffects?.Find(effectUuid);
            if (ofxDefinition != null)
            {
                var adapter = ofxDefinition.CreateAdapter();
                return new EffectModel(new DirectEffectHandle(adapter), ofxDefinition.Metadata, projectModel, compositionModel, layerModel, historyModel, effectId);
            }

            return null;
        }

        public IEffectMetadata? GetMetadata(Guid effectUuid)
        {
            EffectMetadataDictionary.TryGetValue(effectUuid, out var metadata);
            return metadata;
        }
    }
}

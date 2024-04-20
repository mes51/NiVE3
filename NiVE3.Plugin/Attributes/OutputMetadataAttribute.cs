using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Attributes
{
    public interface IOutputMetadata
    {
        /// <summary>
        /// 出力プラグインの型
        /// </summary>
        Type PluginType { get; }

        /// <summary>
        /// 出力プラグインの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 出力プラグインの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 出力プラグインの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 出力プラグインの識別のためのGuid
        /// この値は全ての出力プラグインの中で一意で或必要があります
        /// </summary>
        string OutputUuid { get; }

        /// <summary>
        /// 対応するファイルの拡張子。複数ある場合はカンマ(,)で区切ります
        /// </summary>
        string SupportedFileType { get; }

        /// <summary>
        /// 書き出しに対応するデータの型
        /// </summary>
        SourceType SupportedSourceType { get; }

        /// <summary>
        /// 書き出しの設定画面が存在するかどうか
        /// </summary>
        bool HasSettingView { get; }

        /// <summary>
        /// 出力直前の画像処理がGPUでの処理に対応しているかどうかを表します
        /// </summary>
        bool IsSupportGpu { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class OutputMetadataAttribute : Attribute, IOutputMetadata
    {
        public Type PluginType { get; }

        public string Name => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(NameKey) ?? NameKey;

        public string Description => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(DescriptionKey) ?? DescriptionKey;

        public string Author { get; }

        public string OutputUuid { get; }

        public string SupportedFileType { get; }

        public SourceType SupportedSourceType { get; }

        public bool HasSettingView { get; }

        /// <summary>
        /// 出力直前の画像処理がGPUでの処理に対応しているかどうか
        /// </summary>
        public bool IsSupportGpu { get; set; }

        /// <summary>
        /// 多言語化用のResourceDictionaryの型
        /// </summary>
        public Type? LanguageResourceDictionaryType { get; set; }

        string NameKey { get; }

        string DescriptionKey { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginType">出力プラグインの型</param>
        /// <param name="name">出力プラグインの表示名、またはResourceDictionaryのキー</param>
        /// <param name="author">出力プラグインの作成者</param>
        /// <param name="description">出力プラグインの概要、またはResourceDictionaryのキー</param>
        /// <param name="outputUuid">出力プラグインの識別のためのGuid</param>
        /// <param name="supportedFileType">対応するファイルの拡張子</param>
        /// <param name="supportedSourceType">書き出しに対応するデータの型</param>
        /// <param name="hasSettingView">書き出しの設定画面が存在するかどうか</param>
        public OutputMetadataAttribute(Type pluginType, string name, string author, string description, string outputUuid, string supportedFileType, SourceType supportedSourceType, bool hasSettingView)
        {
            PluginType = pluginType;
            Author = author;
            OutputUuid = outputUuid;
            SupportedFileType = supportedFileType;
            SupportedSourceType = supportedSourceType;
            HasSettingView = hasSettingView;
            NameKey = name;
            DescriptionKey = description;
        }
    }
}

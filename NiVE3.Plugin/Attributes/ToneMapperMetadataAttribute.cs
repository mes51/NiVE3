using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Attributes
{
    public interface IToneMapperMetadata
    {
        /// <summary>
        /// プラグインの型
        /// </summary>
        Type PluginType { get; }

        /// <summary>
        /// トーンマッパーの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// トーンマッパーの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// トーンマッパーの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// トーンマッパーの識別のためのGuid
        /// この値はすべてのトーンマッパーの中で一意である必要があります
        /// </summary>
        string ToneMapperUuid { get; }

        /// <summary>
        /// GPUによるアクセラレーションに対応しているかどうかを表します
        /// </summary>
        bool IsSupportGpu { get; }
    }

    /// <summary>
    /// NiVEに対し公開するトーンマッパーの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ToneMapperMetadataAttribute : Attribute, IToneMapperMetadata
    {
        public string Name => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(NameKey) ?? NameKey;

        public string Description => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(DescriptionKey) ?? DescriptionKey;

        public Type PluginType { get; }

        public string Author { get; }

        public string ToneMapperUuid { get; }

        /// <summary>
        /// GPUによるアクセラレーションに対応しているかどうか
        /// </summary>
        public bool IsSupportGpu { get; set; }

        /// <summary>
        /// 多言語化用のResourceDictionaryの型
        /// </summary>
        public Type? LanguageResourceDictionaryType { get; set; }

        string NameKey { get; }

        string DescriptionKey { get; }

        public ToneMapperMetadataAttribute(Type pluginType, string name, string author, string description, string toneMapperUuid)
        {
            NameKey = name;
            DescriptionKey = description;
            PluginType = pluginType;
            Author = author;
            ToneMapperUuid = toneMapperUuid;
        }
    }
}

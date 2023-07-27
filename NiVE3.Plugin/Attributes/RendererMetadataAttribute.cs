using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Attributes
{
    public interface IRendererMetadata
    {
        /// <summary>
        /// プラグインの型
        /// </summary>
        Type PluginType { get; }

        /// <summary>
        /// レンダラの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// レンダラの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// レンダラの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// レンダラの識別のためのGuid
        /// この値はすべてのレンダラの中で一意である必要があります
        /// </summary>
        string RendererUuid { get; }
    }

    /// <summary>
    /// NiVEに対し公開するレンダラの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RendererMetadataAttribute : Attribute, IRendererMetadata
    {
        public string Name => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(NameKey) ?? NameKey;

        public string Description => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(DescriptionKey) ?? DescriptionKey;

        public Type PluginType { get; }

        public string Author { get; }

        public string RendererUuid { get; }

        /// <summary>
        /// 多言語化用のResourceDictionaryの型
        /// </summary>
        public Type? LanguageResourceDictionaryType { get; set; }

        string NameKey { get; }

        string DescriptionKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">レンダラの表示名、またはResourceDictionaryのキー</param>
        /// <param name="author">レンダラの作成者</param>
        /// <param name="description">レンダラの概要、またはResourceDictionaryのキー</param>
        /// <param name="rendererUuid">レンダラの識別のためのGuid</param>
        public RendererMetadataAttribute(Type pluginType, string name, string author, string description, string rendererUuid)
        {
            NameKey = name;
            DescriptionKey = description;
            PluginType = pluginType;
            Author = author;
            RendererUuid = rendererUuid;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Attributes
{
    public interface IInputMetadata
    {
        /// <summary>
        /// 入力プラグインの型
        /// </summary>
        Type PluginType { get; }

        /// <summary>
        /// 入力プラグインの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 入力プラグインの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 入力プラグインの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 入力プラグインの識別のためのGuid
        /// この値は全ての入力プラグインの中で一意で或必要があります
        /// </summary>
        string InputUuid { get; }

        /// <summary>
        /// 対応するファイルの拡張子。複数ある場合はカンマ(,)で区切ります。
        /// </summary>
        string SupportedFileType { get; }

        /// <summary>
        /// 読み込み時の設定画面が存在するかどうか
        /// </summary>
        bool HasSettingView { get; }
    }

    /// <summary>
    /// NiVEに対し公開する入力プラグインの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class InputMetadataAttribute : Attribute, IInputMetadata
    {
        public Type PluginType { get; }

        public string Name => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(NameKey) ?? NameKey;

        public string Description => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(DescriptionKey) ?? DescriptionKey;

        public string Author { get; }

        public string InputUuid { get; }

        public string SupportedFileType { get; }

        public bool HasSettingView { get; }

        /// <summary>
        /// 多言語化用のResourceDictionaryの型
        /// </summary>
        public Type? LanguageResourceDictionaryType { get; set; }

        string NameKey { get; }

        string DescriptionKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pluginType">入力プラグインの型</param>
        /// <param name="name">入力プラグインの名前、またはResourceDictionaryのキー</param>
        /// <param name="author">入力プラグインの作成者</param>
        /// <param name="description">入力プラグインの概要、またはResourceDictionaryのキー</param>
        /// <param name="inputUuid">入力プラグインの識別のためのGuid</param>
        /// <param name="supportedFileType">対応するファイルの拡張子</param>
        /// <param name="hasSettingView">読み込み時の設定画面が存在するかどうか</param>
        public InputMetadataAttribute(Type pluginType, string name, string author, string description, string inputUuid, string supportedFileType, bool hasSettingView = false)
        {
            PluginType = pluginType;
            NameKey = name;
            DescriptionKey = description;
            Author = author;
            InputUuid = inputUuid;
            SupportedFileType = supportedFileType;
            HasSettingView = hasSettingView;
        }
    }
}

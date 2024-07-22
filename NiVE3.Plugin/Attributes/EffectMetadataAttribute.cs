using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;

namespace NiVE3.Plugin.Attributes
{
    /// <summary>
    /// エフェクトが処理できるソースを表します
    /// </summary>
    [Flags]
    public enum EffectSupportedSource
    {
        /// <summary>
        /// なし
        /// </summary>
        None = 0,
        /// <summary>
        /// 画像とビデオ
        /// </summary>
        Image = 0x1,
        /// <summary>
        /// 音声
        /// </summary>
        Audio = 0x2
    }

    /// <summary>
    /// エフェクトの概要
    /// </summary>
    public interface IEffectMetadata
    {
        /// <summary>
        /// エフェクトの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// エフェクトの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// エフェクトのカテゴリ
        /// </summary>
        string Category { get; }

        /// <summary>
        /// エフェクトの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// エフェクトの識別のためのGuid
        /// この値は全てのエフェクトの中で一意である必要があります
        /// </summary>
        string EffectUuid { get; }

        /// <summary>
        /// 時間経過により適用結果が変わるエフェクトであるかどうかを表します
        /// </summary>
        bool IsRenderEveryFrame { get; }

        /// <summary>
        /// 何もしないエフェクトであることを表します
        /// </summary>
        bool IsDummyEffect { get; }

        /// <summary>
        /// GPUによるアクセラレーションに対応しているかどうかを表します
        /// </summary>
        bool IsSupportGpu { get; }

        /// <summary>
        /// エフェクトが処理できるソースを表します
        /// </summary>
        EffectSupportedSource SupportedSource { get; }
    }

    /// <summary>
    /// NiVEに対し公開するエフェクトの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EffectMetadataAttribute : Attribute, IEffectMetadata
    {
        public string Name => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(NameKey) ?? NameKey;

        public string Description => LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(DescriptionKey) ?? DescriptionKey;

        public string Author { get; }

        public string Category { get; }

        public string EffectUuid { get; }

        /// <summary>
        /// 時間経過により適用結果が変わるエフェクトであるかどうか
        /// </summary>
        public bool IsRenderEveryFrame { get; set; }

        /// <summary>
        /// 何もしないエフェクトかどうか
        /// </summary>
        public bool IsDummyEffect { get; set; } = false;

        /// <summary>
        /// GPUによるアクセラレーションに対応しているかどうか
        /// </summary>
        public bool IsSupportGpu { get; set; }

        /// <summary>
        /// エフェクトが処理できるソース
        /// </summary>
        public EffectSupportedSource SupportedSource { get; set; } = EffectSupportedSource.Image;

        /// <summary>
        /// 多言語化用のResourceDictionaryの型
        /// </summary>
        public Type? LanguageResourceDictionaryType { get; set; }

        string NameKey { get; }

        string DescriptionKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">エフェクトの表示名、またはResourceDictionaryのキー</param>
        /// <param name="author">エフェクトの制作者、またはResourceDictionaryのキー</param>
        /// <param name="description">エフェクトの概要</param>
        /// <param name="effectUuid">エフェクトの識別のためのGuid</param>
        public EffectMetadataAttribute(string name, string author, string category, string description, string effectUuid)
        {
            Author = author;
            Category = category;
            EffectUuid = effectUuid;
            NameKey = name;
            DescriptionKey = description;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Resource
{
    /// <summary>
    /// LanguageResourceDictionaryからローカライズ済みのテキストを取得するためのキー
    /// </summary>
    public class LanguageResourceKey : IEquatable<LanguageResourceKey>
    {
        /// <summary>
        /// 使用するLanguageResourceDictionaryBaseの型
        /// </summary>
        public Type LanguageResourceDictionaryType { get; }

        /// <summary>
        /// ローカライズ済みのテキストのキー
        /// </summary>
        public string ResourceKey { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="languageResourceDictionaryType">使用するLanguageResourceDictionaryBaseの型</param>
        /// <param name="resourceKey">ローカライズ済みのテキストのキー</param>
        public LanguageResourceKey(Type languageResourceDictionaryType, string resourceKey)
        {
            LanguageResourceDictionaryType = languageResourceDictionaryType;
            ResourceKey = resourceKey;
        }

        /// <summary>
        /// ローカライズ済みのテキストを取得します
        /// </summary>
        /// <returns></returns>
        public string? GetText()
        {
            return LanguageResourceDictionaryBase.GetLanguageResourceDictionary(LanguageResourceDictionaryType)?.GetText(ResourceKey);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LanguageResourceKey);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(LanguageResourceDictionaryType);
            hashCode.Add(ResourceKey);
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return GetText() ?? "";
        }

        public bool Equals(LanguageResourceKey? other)
        {
            return LanguageResourceDictionaryType == other?.LanguageResourceDictionaryType && ResourceKey == other?.ResourceKey;
        }
    }
}

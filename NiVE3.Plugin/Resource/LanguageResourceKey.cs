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
    public class LanguageResourceKey
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
    }
}

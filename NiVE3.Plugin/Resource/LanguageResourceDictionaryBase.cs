using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.Plugin.Resource
{
    public abstract class LanguageResourceDictionaryBase : ResourceDictionary
    {
        private static string selectedLanguage = "ja-jp";
        public static string SelectedLanguage
        {
            get { return selectedLanguage; }
            internal set
            {
                selectedLanguage = value;
                foreach (var l in Cache.Values)
                {
                    l.Reload();
                }
            }
        }

        static Dictionary<Type, LanguageResourceDictionaryBase> Cache { get; } = [];

        protected abstract void Reload();

        public string GetText(string key)
        {
            return (this[key] as string) ?? "";
        }

        internal static LanguageResourceDictionaryBase? GetLanguageResourceDictionary(Type? type)
        {
            if (type == null)
            {
                return null;
            }

            if (!Cache.ContainsKey(type))
            {
                if (Activator.CreateInstance(type) is LanguageResourceDictionaryBase dictionary)
                {
                    Cache[type] = dictionary;
                }
                else
                {
                    return null;
                }
            }

            return Cache[type];
        }
    }
}

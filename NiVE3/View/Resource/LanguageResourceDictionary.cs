using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary]
    class LanguageResourceDictionary : ResourceDictionary
    {
        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        [ShowInMarkup, DefaultValue("NicoVisualEffects 3")]
        public static readonly string MainWindow_Title = nameof(MainWindow_Title);

        [ShowInMarkup, DefaultValue("ファイル(_F)")]
        public static readonly string MainWindow_Menu_File = nameof(MainWindow_Menu_File);

        [ShowInMarkup, DefaultValue("プロジェクトを開く(_O)")]
        public static readonly string MainWindow_Menu_OpenProject = nameof(MainWindow_Menu_OpenProject);

        [ShowInMarkup, DefaultValue("終了(_X)")]
        public static readonly string MainWindow_Menu_Exit = nameof(MainWindow_Menu_Exit);

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => (f.Name, f.GetCustomAttribute<DefaultValueAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Name, t => Tuple.Create(t.Item2!.DefaultValue, Version.Parse(t.Item2!.FromVersion)));
        }

        string selectedLanguage = "";
        public string SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                if (selectedLanguage!= value)
                {
                    selectedLanguage = value;
                    Reload();
                }
            }
        }

        public LanguageResourceDictionary()
        {
            SelectedLanguage = "ja-jp";
        }

        public void Reload()
        {
            // TODO: 言語情報読み込み&バージョン比較後適用
            foreach (var (key, (defaultValue, version)) in LanguageKeys)
            {
                this[key] = defaultValue;
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class DefaultValueAttribute : Attribute
        {
            public DefaultValueAttribute(string defaultValue)
            {
                DefaultValue = defaultValue;
            }

            public string DefaultValue { get; }

            public string FromVersion { get; set; } = "0.0.0.0";
        }
    }
}

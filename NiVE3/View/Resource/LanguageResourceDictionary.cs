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

        [ShowInMarkup, DefaultValue("名前")]
        public static readonly string FootageListView_FootageName = nameof(FootageListView_FootageName);

        [ShowInMarkup, DefaultValue("サイズ")]
        public static readonly string FootageListView_FootageSize = nameof(FootageListView_FootageSize);

        [ShowInMarkup, DefaultValue("フレームレート")]
        public static readonly string FootageListView_FootageFrameRate = nameof(FootageListView_FootageFrameRate);

        [ShowInMarkup, DefaultValue("デュレーション")]
        public static readonly string FootageListView_FootageDuration = nameof(FootageListView_FootageDuration);

        [ShowInMarkup, DefaultValue("ファイルパス")]
        public static readonly string FootageListView_FootageFilePath = nameof(FootageListView_FootageFilePath);

        [ShowInMarkup, DefaultValue("拡張子")]
        public static readonly string FootageListView_FootageFileExtension = nameof(FootageListView_FootageFileExtension);

        [ShowInMarkup, DefaultValue("コメント")]
        public static readonly string FootageListView_FootageComment = nameof(FootageListView_FootageComment);

        [ShowInMarkup, DefaultValue("名前:")]
        public static readonly string SolidInputSettingView_FootageName = nameof(SolidInputSettingView_FootageName);

        [ShowInMarkup, DefaultValue("サイズ")]
        public static readonly string SolidInputSettingView_Group_Size = nameof(SolidInputSettingView_Group_Size);

        [ShowInMarkup, DefaultValue("カラー")]
        public static readonly string SolidInputSettingView_Group_Color = nameof(SolidInputSettingView_Group_Color);

        [ShowInMarkup, DefaultValue("幅:")]
        public static readonly string SolidInputSettingView_Width = nameof(SolidInputSettingView_Width);

        [ShowInMarkup, DefaultValue("高さ:")]
        public static readonly string SolidInputSettingView_Height = nameof(SolidInputSettingView_Height);

        [ShowInMarkup, DefaultValue("縦横比を固定する")]
        public static readonly string SolidInputSettingView_IsFixRatio = nameof(SolidInputSettingView_IsFixRatio);

        [ShowInMarkup, DefaultValue("コンポジションサイズに合わせる")]
        public static readonly string SolidInputSettingView_FitCompositionSize = nameof(SolidInputSettingView_FitCompositionSize);

        [ShowInMarkup, DefaultValue("OK")]
        public static readonly string Dialog_OK = nameof(Dialog_OK);

        [ShowInMarkup, DefaultValue("キャンセル")]
        public static readonly string Dialog_Cancel = nameof(Dialog_Cancel);

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

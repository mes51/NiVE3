using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Attributes
{
    public interface IInputMetadata
    {
        /// <summary>
        /// 入力プラグインの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 入力プラグインの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 入力プラグインの識別のためのGuid
        /// この値は全ての入力プラグインの中で一意で或必要があります
        /// </summary>
        string InputUuid { get; }

        /// <summary>
        /// 対応するファイルの拡張子。フォーマットはOpenFileDialogのFilterの構文に準じます
        /// </summary>
        string SupportedFileType { get; }

        /// <summary>
        /// 読み込み時の設定画面が存在するかどうか
        /// </summary>
        bool HasSettingWindow { get; }
    }

    /// <summary>
    /// NiVEに対し公開する入力プラグインの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class InputMetadataAttribute : Attribute, IInputMetadata
    {
        public string Name { get; }

        public string Author { get; }

        public string InputUuid { get; }

        public string SupportedFileType { get; }

        public bool HasSettingWindow { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">入力プラグインの名前</param>
        /// <param name="author">入力プラグインの作成者</param>
        /// <param name="inputUuid">入力プラグインの識別のためのGuid</param>
        /// <param name="supportedFileType">対応するファイルの拡張子</param>
        /// <param name="hasSettingWindow">読み込み時の設定画面が存在するかどうか</param>
        public InputMetadataAttribute(string name, string author, string inputUuid, string supportedFileType, bool hasSettingWindow = false)
        {
            Name = name;
            Author = author;
            InputUuid = inputUuid;
            SupportedFileType = supportedFileType;
            HasSettingWindow = hasSettingWindow;
        }
    }
}

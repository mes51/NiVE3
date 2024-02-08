using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Image;

namespace NiVE3.Plugin.Interfaces
{
    /// <summary>
    /// 入力プラグインを表すインターフェース
    /// </summary>
    public interface IInput : IDisposable
    {
        /// <summary>
        /// 読み込んでいるファイルのパス
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// 入力プラグインのセットアップを行います。使用するGPUや設定が変更される度に呼び出されます。
        /// </summary>
        /// <param name="accelerator">実行するデバイスを表すオブジェクトを含むIAcceleratorObject。</param>
        public void SetupAccelerator(IAcceleratorObject accelerator);

        /// <summary>
        /// ファイルを読み込みます
        /// </summary>
        /// <param name="filePath">読み込むファイルのパス。ファイルはすでに存在チェック済みになります。</param>
        /// <returns>読み込みが成功した場合はtrue(HasSettingViewがtrueの場合は、GetLoadSettingを呼び出しても問題ない場合)、そうでない場合はfalse</returns>
        bool Load(string filePath);

        /// <summary>
        /// 現在の入力プラグインの状態を表すデータをシリアル化可能な状態で取得します
        /// </summary>
        /// <returns>入力プラグインの状態を表すシリアル化可能なobject</returns>
        object? SaveData() => null;

        /// <summary>
        /// 入力プラグインの状態を読み込みます
        /// </summary>
        /// <param name="data">読み込む入力プラグインの状態を表すobject</param>
        /// <returns>プラグインの初期化、およびファイルの読み込みが完了した場合はtrue、そうでない場合はfalse</returns>
        bool LoadData(object? data) => true;

        /// <summary>
        /// 入力プラグインの設定画面を表示するためのウインドウを取得します。
        /// InputMetadataAttribute.HasSettingViewがtrueの時のみ、Loadメソッド呼び出し後に呼ばれます。
        /// </summary>
        /// <param name="compositionSize">現在開いているコンポジションのサイズ。開いていない場合はnull。</param>
        /// <returns>入力プラグインの設定画面のView。ファイルによって設定画面が存在しない場合はnull</returns>
        FrameworkElement? GetLoadSetting(Size? compositionSize) => null;

        /// <summary>
        /// 入力プラグインの読み込み時の設定を適用します。
        /// GetLoadSettingで取得したViewを表示後、ユーザーによってOKが選択されたときに呼び出されます。
        /// </summary>
        /// <param name="setting">GetLoadSettingで取得したViewのDataContext</param>
        /// <returns>設定を反映し、ファイルの読み込みが完了した場合はtrue、そうでない場合はfalse</returns>
        bool ApplyLoadSetting(object? setting) => true;

        /// <summary>
        /// 読み込んだファイルから、ソースを取得します
        /// </summary>
        /// <returns>読み込んだファイルのソースを表すFootageSourceGroup</returns>
        FootageSourceGroup GetGroup();
    }

    /// <summary>
    /// 読み込んだソースのまとまりを表すクラス
    /// </summary>
    public class FootageSourceGroup
    {
        /// <summary>
        /// グループの名前。最上位のグループ以外はこの名前でフッテージパネルにフォルダが作成されます。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 子のグループ。
        /// </summary>
        public FootageSourceGroup[] ChildrenGroup { get; }

        /// <summary>
        /// このグループに属するソース。
        /// </summary>
        public IFootageSource[] Sources { get; }

        /// <summary>
        /// コンストラクタ。最上位のグループ、かつ子グループ、が存在しない場合に使用します。
        /// </summary>
        /// <param name="sources">このグループに属するソース</param>
        public FootageSourceGroup(IFootageSource[] sources) : this("Root", Array.Empty<FootageSourceGroup>(), sources) { }

        /// <summary>
        /// コンストラクタ。子グループ、が存在しない場合に使用します。
        /// </summary>
        /// <param name="name">グループの名前</param>
        /// <param name="sources">このグループに属するソース</param>
        public FootageSourceGroup(string name, IFootageSource[] sources) : this(name, Array.Empty<FootageSourceGroup>(), sources) { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">このグループの名前</param>
        /// <param name="childrenGroup">子グループ</param>
        /// <param name="sources">このグループに属するソース</param>
        public FootageSourceGroup(string name, FootageSourceGroup[] childrenGroup, IFootageSource[] sources)
        {
            Name = name;
            ChildrenGroup = childrenGroup;
            Sources = sources;
        }
    }
}
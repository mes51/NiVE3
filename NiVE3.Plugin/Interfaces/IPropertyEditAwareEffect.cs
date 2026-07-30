using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Interfaces
{
    /// <summary>
    /// プロパティの値の編集通知と、エフェクト側からの値の書き戻しに対応するエフェクトを表します
    /// IEffect と併せて実装することで、値の編集時 (キーフレーム操作や Undo を含む) に通知を受け取れます
    /// </summary>
    public interface IPropertyEditAwareEffect
    {
        /// <summary>
        /// プロパティの値が編集された際に呼び出されます
        /// </summary>
        /// <param name="properties">エフェクトのプロパティ一覧</param>
        void OnPropertyValuesEdited(IPropertyObject[] properties);

        /// <summary>
        /// Undo/Redo によってプロパティの値が復元された際に呼び出されます
        /// エフェクトは内部状態 (表示/有効状態など) を復元後の値に合わせて更新できますが、
        /// この呼び出し中の値の書き戻しは無視されます (復元された値を壊さないため)
        /// </summary>
        /// <param name="properties">エフェクトのプロパティ一覧</param>
        void OnPropertyValuesRestored(IPropertyObject[] properties);

        /// <summary>
        /// エフェクト側からプロパティの値を書き換える際に発生します
        /// (OpenFX プラグインがパラメータ連動で値を変更した場合など)
        /// </summary>
        event EventHandler<PropertyValuesWritebackEventArgs>? PropertyValuesWriteback;
    }

    /// <summary>
    /// エフェクト側からのプロパティ値の書き戻しを表します
    /// </summary>
    public class PropertyValuesWritebackEventArgs : EventArgs
    {
        /// <summary>
        /// 書き戻す値の一覧 (プロパティの ID と新しい値)
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, object?>> Values { get; }

        /// <summary>
        /// ユーザー操作 (値の編集やボタンクリック) に起因する変更かどうか
        /// true の場合はアンドゥ履歴へ記録することが期待されます
        /// </summary>
        public bool IsUserAction { get; }

        public PropertyValuesWritebackEventArgs(IReadOnlyList<KeyValuePair<string, object?>> values, bool isUserAction)
        {
            Values = values;
            IsUserAction = isUserAction;
        }
    }
}

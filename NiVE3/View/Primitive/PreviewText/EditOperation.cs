using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// 編集操作の種類。連続入力の Undo まとめ (coalescing) の判定に使う。
    /// </summary>
    enum EditKind
    {
        Other,
        Typing,
        Backspace,
        DeleteForward,
    }

    /// <summary>
    /// 1 回の編集 (置換) を表す。Undo は Inserted を Removed に戻し、Redo はその逆を行う。
    /// 編集前後の選択状態も保持し、Undo/Redo 後にキャレット位置を復元する。
    /// </summary>
    sealed class EditOperation
    {
        public int Offset { get; set; }

        public string Removed { get; set; } = "";

        public string Inserted { get; set; } = "";

        public int AnchorBefore { get; set; }

        public int CaretBefore { get; set; }

        public int AnchorAfter { get; set; }

        public int CaretAfter { get; set; }

        public EditKind Kind { get; set; }
    }
}
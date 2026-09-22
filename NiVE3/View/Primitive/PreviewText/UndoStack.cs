using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// Undo/Redo スタック。連続したタイピング・Backspace・Delete は
    /// TextBox と同様に 1 つの操作にまとめる。
    /// </summary>
    sealed class UndoStack
    {
        List<EditOperation> UndoOperations { get; } = [];

        List<EditOperation> RedoOperations { get; } = [];

        public bool CanUndo => UndoOperations.Count > 0;

        public bool CanRedo => RedoOperations.Count > 0;

        public void Clear()
        {
            UndoOperations.Clear();
            RedoOperations.Clear();
        }

        public void Push(EditOperation operation)
        {
            RedoOperations.Clear();
            if (UndoOperations.Count > 0 && TryMerge(UndoOperations[^1], operation))
            {
                return;
            }
            UndoOperations.Add(operation);
        }

        public EditOperation? Undo()
        {
            if (UndoOperations.Count == 0)
            {
                return null;
            }
            var operation = UndoOperations[^1];
            UndoOperations.RemoveAt(UndoOperations.Count - 1);
            RedoOperations.Add(operation);
            return operation;
        }

        public EditOperation? Redo()
        {
            if (RedoOperations.Count == 0)
            {
                return null;
            }
            var operation = RedoOperations[^1];
            RedoOperations.RemoveAt(RedoOperations.Count - 1);
            UndoOperations.Add(operation);
            return operation;
        }

        static bool TryMerge(EditOperation last, EditOperation operation)
        {
            // 連続タイピング: 直前の挿入の直後への挿入 (改行を除く)
            if (operation.Kind == EditKind.Typing && last.Kind == EditKind.Typing &&
                operation.Removed.Length == 0 && last.Removed.Length == 0 &&
                operation.Offset == last.Offset + last.Inserted.Length &&
                !TextNewLine.Contains(operation.Inserted))
            {
                last.Inserted += operation.Inserted;
                last.AnchorAfter = operation.AnchorAfter;
                last.CaretAfter = operation.CaretAfter;
                return true;
            }

            // 連続 Backspace: 直前の削除位置の直前を削除
            if (operation.Kind == EditKind.Backspace && last.Kind == EditKind.Backspace &&
                operation.Inserted.Length == 0 && last.Inserted.Length == 0 &&
                operation.Offset + operation.Removed.Length == last.Offset)
            {
                last.Offset = operation.Offset;
                last.Removed = operation.Removed + last.Removed;
                last.AnchorAfter = operation.AnchorAfter;
                last.CaretAfter = operation.CaretAfter;
                return true;
            }

            // 連続 Delete: 同一位置での前方削除
            if (operation.Kind == EditKind.DeleteForward && last.Kind == EditKind.DeleteForward &&
                operation.Inserted.Length == 0 && last.Inserted.Length == 0 &&
                operation.Offset == last.Offset)
            {
                last.Removed += operation.Removed;
                last.AnchorAfter = operation.AnchorAfter;
                last.CaretAfter = operation.CaretAfter;
                return true;
            }

            return false;
        }
    }
}
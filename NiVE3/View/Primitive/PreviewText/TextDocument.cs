using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// エディタが編集対象とするテキストのモデル。
    /// 改行は正規化せずそのまま保持する。オフセットや編集通知 (TextEditedEventArgs) をホスト側の本文と
    /// 一致させるためで、"\r\n"・"\n"・"\r" のいずれも 1 つの改行として扱う (TextNewLine)。
    /// 行頭オフセットのインデックスを保持し、オフセット⇔行番号の変換を提供する。
    /// </summary>
    sealed class TextDocument
    {
        List<int> LineStarts { get; } = [0];

        public string Text { get; private set; } = "";

        public int Length => Text.Length;

        public int LineCount => LineStarts.Count;

        /// <summary>
        /// 本文で使われている改行コード。改行の挿入 (Enter・貼り付け) はこれに揃える
        /// </summary>
        public string NewLine => TextNewLine.Detect(Text);

        /// <summary>
        /// テキストが変更されるたびに発生する
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// テキスト全体を置き換える (プロパティ経由の外部設定用)
        /// </summary>
        public void SetText(string? text)
        {
            Text = text ?? "";
            RebuildLineIndex();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public string GetText(int offset, int length)
        {
            return Text.Substring(offset, length);
        }

        /// <summary>
        /// [offset, offset+length) を inserted で置き換える。すべての編集操作はここを通る。
        /// </summary>
        public void Replace(int offset, int length, string inserted)
        {
            if (offset < 0 || length < 0 || offset + length > Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            Text = Text.Remove(offset, length).Insert(offset, inserted);
            RebuildLineIndex();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// オフセットが属する行番号を返す。行末 (改行の直前) はその行に属する。
        /// </summary>
        public int GetLineIndexFromOffset(int offset)
        {
            var low = 0;
            var high = LineStarts.Count - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (LineStarts[middle] <= offset)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            return low;
        }

        public int GetLineStart(int line)
        {
            return LineStarts[line];
        }

        /// <summary>
        /// 行の長さ (末尾の改行を含まない)
        /// </summary>
        public int GetLineLength(int line)
        {
            var start = LineStarts[line];
            var end = line + 1 < LineStarts.Count
                ? LineStarts[line + 1] - TextNewLine.GetLengthBefore(Text, LineStarts[line + 1])
                : Text.Length;
            return end - start;
        }

        /// <summary>
        /// 行末の改行の長さ (最終行なら 0)
        /// </summary>
        public int GetLineNewLineLength(int line)
        {
            return line + 1 < LineStarts.Count ? TextNewLine.GetLengthBefore(Text, LineStarts[line + 1]) : 0;
        }

        void RebuildLineIndex()
        {
            LineStarts.Clear();
            LineStarts.Add(0);
            var index = 0;
            while (index < Text.Length)
            {
                var newLineLength = TextNewLine.GetLengthAt(Text, index);
                if (newLineLength > 0)
                {
                    index += newLineLength;
                    LineStarts.Add(index);
                }
                else
                {
                    index++;
                }
            }
        }
    }
}
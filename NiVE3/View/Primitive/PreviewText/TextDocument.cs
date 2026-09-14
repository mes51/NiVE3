using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// エディタが編集対象とするテキストのモデル。
    /// 改行は内部的に '\n' へ正規化して保持する (クリップボード入出力時に "\r\n" と相互変換する)。
    /// 行頭オフセットのインデックスを保持し、オフセット⇔行番号の変換を提供する。
    /// </summary>
    sealed class TextDocument
    {
        List<int> LineStarts { get; } = [0];

        public string Text { get; private set; } = "";

        public int Length => Text.Length;

        public int LineCount => LineStarts.Count;

        /// <summary>
        /// テキストが変更されるたびに発生する
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// 改行コードを '\n' に正規化する
        /// </summary>
        public static string Normalize(string? text)
        {
            return (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        }

        /// <summary>
        /// テキスト全体を置き換える (プロパティ経由の外部設定用)
        /// </summary>
        public void SetText(string? text)
        {
            Text = Normalize(text);
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
        /// オフセットが属する行番号を返す。行末 ('\n' の直前) はその行に属する。
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
        /// 行の長さ (末尾の '\n' を含まない)
        /// </summary>
        public int GetLineLength(int line)
        {
            var start = LineStarts[line];
            var end = line + 1 < LineStarts.Count ? LineStarts[line + 1] - 1 : Text.Length;
            return end - start;
        }

        void RebuildLineIndex()
        {
            LineStarts.Clear();
            LineStarts.Add(0);
            for (var i = 0; i < Text.Length; i++)
            {
                if (Text[i] == '\n')
                {
                    LineStarts.Add(i + 1);
                }
            }
        }
    }
}
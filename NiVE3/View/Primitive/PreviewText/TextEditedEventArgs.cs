using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// 1 回のテキスト編集の詳細。[Offset, Offset + RemovedText.Length) が InsertedText に
    /// 置き換えられたことを表す。ホスト側でスタイル範囲などの付随情報を編集に追従させるために使う。
    /// </summary>
    sealed class TextEditedEventArgs : EventArgs
    {
        public int Offset { get; }

        public string RemovedText { get; }

        public string InsertedText { get; }

        public int RemovedLength => RemovedText.Length;

        public int InsertedLength => InsertedText.Length;

        public TextEditedEventArgs(int offset, string removedText, string insertedText)
        {
            Offset = offset;
            RemovedText = removedText;
            InsertedText = insertedText;
        }
    }
}
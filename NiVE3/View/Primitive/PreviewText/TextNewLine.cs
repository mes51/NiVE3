using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// 改行コードの扱いを集約するヘルパー。
    /// エディタは本文の改行を正規化せずに保持し (ホスト側のオフセットと一致させるため)、
    /// "\r\n"・"\n"・"\r" のいずれも 1 つの改行として扱う。
    /// </summary>
    static class TextNewLine
    {
        /// <summary>
        /// 本文に改行が無いときに使う既定の改行コード
        /// </summary>
        public const string Default = "\r\n";

        static char[] NewLineChars { get; } = ['\r', '\n'];

        public static bool IsNewLineChar(char character)
        {
            return character == '\r' || character == '\n';
        }

        public static bool Contains(string text)
        {
            return text.IndexOfAny(NewLineChars) >= 0;
        }

        /// <summary>
        /// index から始まる改行の長さ ("\r\n" なら 2、"\n" / "\r" なら 1、改行でなければ 0)
        /// </summary>
        public static int GetLengthAt(string text, int index)
        {
            if (index < 0 || index >= text.Length || !IsNewLineChar(text[index]))
            {
                return 0;
            }
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                return 2;
            }
            return 1;
        }

        /// <summary>
        /// offset の直前で終わる改行の長さ ("\r\n" なら 2、"\n" / "\r" なら 1、改行でなければ 0)
        /// </summary>
        public static int GetLengthBefore(string text, int offset)
        {
            if (offset <= 0 || offset > text.Length || !IsNewLineChar(text[offset - 1]))
            {
                return 0;
            }
            if (text[offset - 1] == '\n' && offset >= 2 && text[offset - 2] == '\r')
            {
                return 2;
            }
            return 1;
        }

        /// <summary>
        /// start 以降で最初に現れる改行の位置。無ければ -1
        /// </summary>
        public static int IndexOf(string text, int start)
        {
            return start >= text.Length ? -1 : text.IndexOfAny(NewLineChars, start);
        }

        /// <summary>
        /// offset を含む行の先頭 (直前の改行の直後)
        /// </summary>
        public static int GetLineStart(string text, int offset)
        {
            if (offset <= 0)
            {
                return 0;
            }
            return text.LastIndexOfAny(NewLineChars, Math.Min(offset, text.Length) - 1) + 1;
        }

        /// <summary>
        /// 本文で使われている改行コードを判定する。改行が無ければ Default
        /// </summary>
        public static string Detect(string text)
        {
            if (text.Contains("\r\n"))
            {
                return "\r\n";
            }
            if (text.Contains('\n'))
            {
                return "\n";
            }
            if (text.Contains('\r'))
            {
                return "\r";
            }
            return Default;
        }

        /// <summary>
        /// text 中の改行 ("\r\n" / "\n" / "\r") をすべて newLine に揃える
        /// </summary>
        public static string ConvertTo(string text, string newLine)
        {
            if (!Contains(text))
            {
                return text;
            }
            var builder = new StringBuilder(text.Length);
            var index = 0;
            while (index < text.Length)
            {
                var length = GetLengthAt(text, index);
                if (length > 0)
                {
                    builder.Append(newLine);
                    index += length;
                }
                else
                {
                    builder.Append(text[index]);
                    index++;
                }
            }
            return builder.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// キャレットの移動先 (文字素・単語境界) を計算するヘルパー。
    /// サロゲートペアや結合絵文字は StringInfo のテキスト要素単位で 1 文字として扱う。
    /// </summary>
    static class TextNavigation
    {
        /// <summary>
        /// 次のキャレット停止位置 (1 テキスト要素分進む)
        /// </summary>
        public static int NextElement(string text, int offset)
        {
            if (offset >= text.Length)
            {
                return text.Length;
            }
            if (text[offset] == '\n')
            {
                return offset + 1;
            }
            var length = StringInfo.GetNextTextElementLength(text, offset);
            return offset + Math.Max(1, length);
        }

        /// <summary>
        /// 前のキャレット停止位置 (1 テキスト要素分戻る)
        /// </summary>
        public static int PrevElement(string text, int offset)
        {
            if (offset <= 0)
            {
                return 0;
            }
            if (text[offset - 1] == '\n')
            {
                return offset - 1;
            }

            // テキスト要素境界は前方からしか列挙できないため、行頭から走査する
            var lineStart = text.LastIndexOf('\n', offset - 1) + 1;
            var position = lineStart;
            var previous = lineStart;
            while (position < offset)
            {
                previous = position;
                position += Math.Max(1, StringInfo.GetNextTextElementLength(text, position));
            }
            return previous;
        }

        /// <summary>
        /// 文字種クラス。日本語はひらがな・カタカナ・漢字を別クラスにして、
        /// Ctrl+矢印やダブルクリックの単語境界が自然になるようにする。
        /// </summary>
        static int CharClass(char character)
        {
            if (character == '\n')
            {
                return -1;
            }
            if (char.IsWhiteSpace(character))
            {
                return 0;
            }
            if (character is >= '぀' and <= 'ゟ')
            {
                // ひらがな
                return 2;
            }
            if (character is (>= '゠' and <= 'ヿ') or (>= 'ｦ' and <= 'ﾝ'))
            {
                // カタカナ
                return 3;
            }
            if (character is (>= '一' and <= '鿿') or (>= '㐀' and <= '䶿') or (>= '豈' and <= '﫿'))
            {
                // 漢字
                return 4;
            }
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                // 英数字
                return 1;
            }
            // 記号など
            return 5;
        }

        /// <summary>
        /// 次の単語の先頭へ移動 (Ctrl+Right / Ctrl+Delete)
        /// </summary>
        public static int NextWord(string text, int offset)
        {
            var length = text.Length;
            var index = offset;
            if (index < length)
            {
                var charClass = CharClass(text[index]);
                if (charClass > 0)
                {
                    while (index < length && CharClass(text[index]) == charClass)
                    {
                        index++;
                    }
                }
            }
            // 空白と改行をスキップ
            while (index < length && CharClass(text[index]) <= 0)
            {
                index++;
            }
            return index;
        }

        /// <summary>
        /// 前の単語の先頭へ移動 (Ctrl+Left / Ctrl+Backspace)
        /// </summary>
        public static int PrevWord(string text, int offset)
        {
            var index = offset;
            // 空白と改行をスキップ
            while (index > 0 && CharClass(text[index - 1]) <= 0)
            {
                index--;
            }
            if (index > 0)
            {
                var charClass = CharClass(text[index - 1]);
                while (index > 0 && CharClass(text[index - 1]) == charClass)
                {
                    index--;
                }
            }
            return index;
        }

        /// <summary>
        /// ダブルクリック時の単語範囲 (同一文字種の連続) を返す
        /// </summary>
        public static (int Start, int End) WordRange(string text, int offset)
        {
            if (text.Length == 0)
            {
                return (0, 0);
            }
            if (offset >= text.Length || text[offset] == '\n')
            {
                if (offset == 0 || text[offset - 1] == '\n')
                {
                    return (offset, offset);
                }
                offset--;
            }

            var charClass = CharClass(text[offset]);
            var start = offset;
            while (start > 0 && text[start - 1] != '\n' && CharClass(text[start - 1]) == charClass)
            {
                start--;
            }
            var end = offset;
            while (end < text.Length && text[end] != '\n' && CharClass(text[end]) == charClass)
            {
                end++;
            }
            return (start, end);
        }
    }
}
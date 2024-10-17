using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace NiVE3.View.Primitive.Editor
{
    static class FoldingStrategy
    {
        const char OpenBrace = '{';

        const char CloseBrace = '}';

        public static IEnumerable<NewFolding> CreateNewFolding(ITextSource textSource)
        {
            var newFoldings = new List<NewFolding>();

            var lastNewLineOffset = 0;
            var startOffsets = new Stack<int>();
            var length = textSource.TextLength;
            for (var i = 0; i < length; i++)
            {
                var c = textSource.GetCharAt(i);
                if (c == OpenBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == CloseBrace && startOffsets.Count > 0)
                {
                    var startOffset = startOffsets.Pop();
                    if (startOffset < lastNewLineOffset)
                    {
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }
            }

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

            return newFoldings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Expression.Utility
{
    static class ExpressionStringUtil
    {
        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        [ExpressionPublicMember]
        public static int countGrapheme(string? text)
        {
            if (text == null)
            {
                return 0;
            }
            else
            {
                return new StringInfo(text).LengthInTextElements;
            }
        }

        [ExpressionPublicMember]
        public static string[] segmentByGrapheme(string? text)
        {
            if (text == null)
            {
                return [];
            }

            var result = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.GetTextElement());
            }
            return [..result];
        }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}

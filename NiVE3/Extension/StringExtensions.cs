using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NiVE3.Extension
{
    static class StringExtensions
    {
        public static int GetGraphemeCount(this string str)
        {
            var count = 0;
            var index = 0;
            while (index < str.Length)
            {
                index += StringInfo.GetNextTextElementLength(str, index);
                count++;
            }

            return count;
        }
    }
}

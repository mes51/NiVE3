using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Expression
{
    static class ExpressionInternalUtil
    {
        public static bool TryConvertToIndex(object value, out int index)
        {
            switch (value)
            {
                case double v:
                    index = (int)v - 1;
                    return true;
                case float v:
                    index = (int)v - 1;
                    return true;
                case long v:
                    index = (int)v - 1;
                    return true;
                case ulong v:
                    index = (int)v - 1;
                    return true;
                case int v:
                    index = v - 1;
                    return true;
                case uint v:
                    index = (int)v - 1;
                    return true;
                case short v:
                    index = v - 1;
                    return true;
                case ushort v:
                    index = v - 1;
                    return true;
                case byte v:
                    index = v - 1;
                    return true;
            }

            index = -1;
            return false;
        }
    }
}

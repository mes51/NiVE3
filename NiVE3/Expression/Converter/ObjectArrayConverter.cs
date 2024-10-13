using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint.Native;
using Jint.Runtime.Interop;
using Jint;

namespace NiVE3.Expression.Converter
{
    internal class ObjectArrayConverter : IObjectConverter
    {
        public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
        {
            if (value is object[] array)
            {
                var resultArray = new JsArray(engine);

                for (var i = 0; i < array.Length; i++)
                {
                    resultArray.Set(new JsNumber(i), JsValue.FromObject(engine, array[i]));
                }

                result = resultArray;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}

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
    class ObjectDictionaryConverter : IObjectConverter
    {
        public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                var resultObject = new JsObject(engine);

                foreach (var (k, v) in dictionary)
                {
                    resultObject.Set(new JsString(k), JsValue.FromObject(engine, v));
                }

                result = resultObject;
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

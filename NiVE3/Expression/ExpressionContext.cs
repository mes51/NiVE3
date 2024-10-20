using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.ShadowRealm;
using NiVE3.Config;
using NiVE3.Expression.Converter;

namespace NiVE3.Expression
{
    class ExpressionContext : IDisposable
    {
        Engine Engine { get; }

        bool Disposed { get; set; }

        public ExpressionContext(double time)
        {
            Engine = new Engine(options =>
            {
                options.Strict = true;
                options.AddObjectConverter<ObjectDictionaryConverter>();
                options.AddObjectConverter<ObjectArrayConverter>();
                options.Interop.CreateClrObject = io => new Dictionary<string, object?>();
                options.TimeoutInterval(TimeSpan.FromSeconds(ApplicationSetting.Setting.ExpressionTimeout));
            });
            Engine.SetValue("time", time);
        }

        public object? Evaluate(ExpressionScript script, object? value)
        {
            Engine.SetValue("thisProperty", value ?? JsValue.Null);
            return ToExpressionValue(Engine.Evaluate(script.Script).ToObject());
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Engine.Dispose();
                Disposed = true;
            }
        }

        /// <summary>
        /// 配列をobject[]に変換、認識できない値をnullにする
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static object? ToExpressionValue(object? value)
        {
            switch (value)
            {
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                case long:
                case ulong:
                case float:
                case double:
                case decimal:
                case string:
                case DateTime:
                    return value;
                case object?[] array:
                    for (var i = 0; i < array.Length; i++)
                    {
                        array[i] = ToExpressionValue(array[i]);
                    }
                    return array;
                case Array typedArray:
                    {
                        var objectArray = new object[typedArray.Length];
                        Array.Copy(typedArray, objectArray, objectArray.Length);
                        return objectArray;
                    }
                case IDictionary<string, object?> dictionary:
                    foreach (var (k, v) in dictionary)
                    {
                        dictionary[k] = ToExpressionValue(dictionary[k]);
                    }
                    return dictionary;
                default:
                    return null;
            }
        }
    }
}

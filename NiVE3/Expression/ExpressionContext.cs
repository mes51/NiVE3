using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.ShadowRealm;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using NiVE3.Config;
using NiVE3.Expression.Converter;
using NiVE3.Expression.Utility;
using NiVE3.Expression.Wrapper;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;

namespace NiVE3.Expression
{
    class ExpressionContext : IDisposable
    {
        Engine Engine { get; }

        bool Disposed { get; set; }

        ProjectModel ProjectModel { get; }

        LayerModel LayerModel { get; }

        EffectModel? EffectModel { get; }

        PropertyModel PropertyModel { get; }

        public ExpressionContext(double globalTime, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, PropertyModel propertyModel)
        {
            ProjectModel = projectModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            PropertyModel = propertyModel;

            Engine = new Engine(options =>
            {
                options.Strict = true;
                options.AddObjectConverter<VectorConverter>();
                options.AddObjectConverter<ObjectDictionaryConverter>();
                options.AddObjectConverter<ObjectArrayConverter>();
                options.Interop.CreateClrObject = static io => new Dictionary<string, object?>();
                options.TimeoutInterval(TimeSpan.FromSeconds(ApplicationSetting.Setting.ExpressionTimeout));
                options.SetTypeResolver(new TypeResolver
                {
                    MemberFilter = static member => Attribute.IsDefined(member, typeof(ExpressionPublicMemberAttribute))
                });
            });
            Engine.SetValue("time", globalTime);
            Engine.SetValue("comp", (Func<string, CompositionWrapper?>)(key => FindComposition(this, key, globalTime)));
            Engine.SetValue("thisComp", new CompositionWrapper(compositionModel, globalTime));
            Engine.SetValue("thisLayer", new LayerWrapper(layerModel, globalTime));
            if (effectModel != null)
            {
                Engine.SetValue("thisEffect", new EffectWrapper(effectModel, globalTime));
            }
            Engine.SetValue("thisProperty", new PropertyWrapper(propertyModel, globalTime));
            Engine.SetValue("Random", new ExpressionRandom(globalTime, propertyModel.ObjectId));
        }

        public object? Evaluate(ExpressionScript script, object? value)
        {
            Engine.SetValue("thisPropertyValue", value ?? JsValue.Null);
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

        static CompositionWrapper? FindComposition(ExpressionContext context, string name, double time)
        {
            foreach (var comp in context.ProjectModel.CompositionModels)
            {
                if (comp.Name == name)
                {
                    return new CompositionWrapper(comp, time);
                }
            }

            return null;
        }
    }
}
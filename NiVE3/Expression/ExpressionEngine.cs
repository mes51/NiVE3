using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;
using NiVE3.Expression.Converter;
using NiVE3.Model;

namespace NiVE3.Expression
{
    class ExpressionEngine
    {
        static Engine Engine { get; }

        static ExpressionEngine()
        {
            Engine = new Engine(options =>
            {
                options.Strict = true;
                options.AddObjectConverter<ObjectDictionaryConverter>();
                options.AddObjectConverter<ObjectArrayConverter>();
                options.Interop.CreateClrObject = io => new Dictionary<string, object?>();
            });
        }

        public static ExpressionContext CreateContext(double globalTime, CompositionModel compositionModel, LayerModel? layerModel, EffectModel? effectModel, PropertyModel propertyModel)
        {
            var realm = Engine.Intrinsics.ShadowRealm.Construct();
            realm.SetValue("time", globalTime);

            return new ExpressionContext(Engine, realm);
        }

        public static ExpressionScript Compile(string code)
        {
            var script = Engine.PrepareScript(code);
            return new ExpressionScript(script);
        }
    }
}

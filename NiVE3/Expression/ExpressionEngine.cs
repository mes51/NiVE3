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
            });
        }

        public static ExpressionContext CreateContext(double globalTime, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, PropertyModel propertyModel)
        {
            return new ExpressionContext(globalTime, projectModel, compositionModel, layerModel, effectModel, propertyModel);
        }

        public static ExpressionScript Compile(string code)
        {
            var script = Engine.PrepareScript(code);
            return new ExpressionScript(script);
        }
    }
}

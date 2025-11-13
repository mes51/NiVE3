using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ReactivePropertyGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class ReactivePropertyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
            {
                ReactivePropertyEmitter.RegisterAttributes(context);
            });

            ReactivePropertyEmitter.RegisterEmit(context);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

// SEE: https://neue.cc/2022/12/16_IncrementalSourceGenerator.html

namespace NiVE3.SourceGenerator.LanguageResourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class LanguageResourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
            {
                LanguageResourceKeyEmitter.RegisterAttributes(context);
            });

            LanguageResourceKeyEmitter.RegisterEmit(context);
        }
    }
}

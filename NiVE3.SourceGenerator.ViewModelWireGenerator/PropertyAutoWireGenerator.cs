using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// SEE: https://neue.cc/2022/12/16_IncrementalSourceGenerator.html

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    [Generator(LanguageNames.CSharp)]
    public partial class PropertyAutoWireGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
            {
                ViewModelWireableEmitter.RegisterAttributes(context);
                ManualViewModelWireableEmitter.RegisterAttributes(context);
            });

            ViewModelWireableEmitter.RegisterEmit(context);
            ManualViewModelWireableEmitter.RegisterEmit(context);
        }
    }
}

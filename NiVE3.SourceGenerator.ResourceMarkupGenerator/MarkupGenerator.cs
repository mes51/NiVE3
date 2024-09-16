using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

// SEE: https://neue.cc/2022/12/16_IncrementalSourceGenerator.html

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    [Generator(LanguageNames.CSharp)]
    public partial class MarkupGenerator : IIncrementalGenerator
    {
        static readonly string Namespace = typeof(MarkupGenerator).Namespace;

        static readonly string MarkupableResourceDictionaryFullName = $"{Namespace}.MarkupableResourceDictionaryAttribute";

        static readonly string ShowInMarkupFullName = $"{Namespace}.ShowInMarkupAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var resourceDictionaryEmitter = new MarkupableResourceDictionaryEmitter();
            var inputBindingEmitter = new MarkupableInputBindingEmitter();

            context.RegisterPostInitializationOutput(context =>
            {
                ShowInMarkupAttributeEmitter.RegisterAttributes(context);
                resourceDictionaryEmitter.RegisterAttributes(context);
                inputBindingEmitter.RegisterAttributes(context);
            });

            resourceDictionaryEmitter.RegisterEmit(context);
            inputBindingEmitter.RegisterEmit(context);
        }
    }
}
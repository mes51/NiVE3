using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class ShowInMarkupAttributeEmitter
    {
        static readonly string Namespace = typeof(ShowInMarkupAttributeEmitter).Namespace;

        public static readonly string FullName = $"{Namespace}.ShowInMarkupAttribute";

        public static void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("ShowInMarkupAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
sealed class ShowInMarkupAttribute : Attribute { }
""");
        }
    }
}

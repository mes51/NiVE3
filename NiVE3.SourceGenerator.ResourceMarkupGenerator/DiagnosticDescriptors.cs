using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor TypeIsNotResourceDictionary = new DiagnosticDescriptor(
            id: "MRG0001",
            title: "Type is not derived ResourceDictionary",
            messageFormat: "{0} is not derived ResourceDictionary",
            category: "MarkupableResourceDictionaryGenerate",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

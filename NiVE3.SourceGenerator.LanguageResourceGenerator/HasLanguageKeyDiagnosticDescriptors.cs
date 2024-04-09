using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.LanguageResourceGenerator
{
    static class HasLanguageKeyDiagnosticDescriptors
    {
        const string Category = "LanguageResourceKeyGenerate";

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "LRKG0001",
            title: "ResourceDictionary must be partial",
            messageFormat: "The ResourceDictionary '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TypeIsNotResourceDictionary = new DiagnosticDescriptor(
            id: "LRKG0002",
            title: "Type is not derived ResourceDictionary",
            messageFormat: "{0} is not derived ResourceDictionary",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

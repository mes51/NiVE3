using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ReactivePropertyGenerator
{
    static class ReactivePropertyDiagnosticDescriptors
    {
        const string Category = "ReactivePropertyGenerator";

        public static readonly DiagnosticDescriptor SetPropertyMethodIsNotDefined = new DiagnosticDescriptor(
            id: "RPG0001",
            title: "SetProperty is not defined or invalid signature",
            messageFormat: "SetProperty is not defined or invalid signature in {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor PropertyIsNotPartial = new DiagnosticDescriptor(
            id: "RPG0002",
            title: "Property is not partial",
            messageFormat: "{0} is not partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor PropertyNotDefinedGetterAndSetter = new DiagnosticDescriptor(
            id: "RPG0003",
            title: "Property is not define getter and setter",
            messageFormat: "{0} is not define getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

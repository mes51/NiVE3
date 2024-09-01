using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor ResourceTypeIsNotResourceDictionary = new DiagnosticDescriptor(
            id: "MRG1001",
            title: "Type is not derived ResourceDictionary",
            messageFormat: "{0} is not derived ResourceDictionary",
            category: "MarkupableResourceDictionaryEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor ResourceShowInMarkupableIsMustBeField = new DiagnosticDescriptor(
            id: "MRG1002",
            title: "Markupable resource is must field",
            messageFormat: "'{0}' type markupable '{1}' is not field. ResourceDictionary markupable is must be field.",
            category: "MarkupableResourceDictionaryEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor GestureDefinitionTypeIsNotDependencyObject = new DiagnosticDescriptor(
            id: "MRG2001",
            title: "Type is not derived DependencyObject",
            messageFormat: "{0} is not derived DependencyObject",
            category: "MarkupableInputBindingGestureEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor GestureDefinitionTypeIsNotDefinedInstanceProperty = new DiagnosticDescriptor(
            id: "MRG2001",
            title: "Type is not defined instance property",
            messageFormat: "{0} is not defined instance property",
            category: "MarkupableInputBindingGestureEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor GestureShowInMarkupableIsMustBeProperty = new DiagnosticDescriptor(
            id: "MRG2003",
            title: "Markupable gesture is must property",
            messageFormat: "'{0}' type markupable '{1}' is not property. Gesture markupable is must be property.",
            category: "MarkupableInputBindingGestureEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor GesturePropertyIsNotInputGesture = new DiagnosticDescriptor(
            id: "MRG2004",
            title: "Property is not InputGesture",
            messageFormat: "'{0}' type '{1}' property is not InputGesture",
            category: "MarkupableInputBindingGestureEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor GesturePropertyMustDefinredGetter = new DiagnosticDescriptor(
            id: "MRG2005",
            title: "Gesture property must define getter",
            messageFormat: "'{0}' type '{1}' property must define getter",
            category: "MarkupableInputBindingGestureEmitter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

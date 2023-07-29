using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    class DiagnosticDescriptors
    {
        const string Category = "ViewModelWireGenerate";

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "VMWG0001",
            title: "Auto wireable ViewModel must be partial",
            messageFormat: "The auto wireable ViewModel '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TypeIsNotINotifyPropertyChanged = new DiagnosticDescriptor(
            id: "VMWG0002",
            title: "Type is not derived INotifyPropertyChanged",
            messageFormat: "'{0}' is not derived INotifyPropertyChanged",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor WiringMethodIsNotDefined = new DiagnosticDescriptor(
            id: "VMWG0003",
            title: "Wiring method is not defined",
            messageFormat: "Wiring method '{0}' is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor WiringMethodMustBePartial = new DiagnosticDescriptor(
            id: "VMWG0004",
            title: "Wiring method must be partial",
            messageFormat: "Wiring method '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor MustDefinredGetterAndSetter = new DiagnosticDescriptor(
            id: "VMWG0005",
            title: "Property must define getter and setter",
            messageFormat: "'{0}' property must define getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourcePropertyIsNotDefined = new DiagnosticDescriptor(
            id: "VMWG0006",
            title: "Source property is not defined",
            messageFormat: "Source '{0}' property is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourcePropertyITypeIsNotINotifyPropertyChanged = new DiagnosticDescriptor(
            id: "VMWG0007",
            title: "Source property type is not derived INotifyPropertyChanged",
            messageFormat: "Source '{0}' type is not derived INotifyPropertyChanged",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourceTypePropertyIsNotDefined = new DiagnosticDescriptor(
            id: "VMWG0008",
            title: "Source type property is not defined",
            messageFormat: "Source '{0}' type '{1}' property is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor OneWaySourceTypePropertyMustDefinredGetter = new DiagnosticDescriptor(
            id: "VMWG0009",
            title: "One way source type property must define getter",
            messageFormat: "One way source '{0}' type '{1}' property must define getter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourceTypePropertyMustDefinredGetterAndSetter = new DiagnosticDescriptor(
            id: "VMWG0010",
            title: "Source type property must define getter and setter",
            messageFormat: "Source '{0}' type '{1}' property must define getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EventNameIsEmpty = new DiagnosticDescriptor(
            id: "VMWG0011",
            title: "bind target event name is empty",
            messageFormat: "bind target event name is empty",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EventIsNotDefinedInSourceType = new DiagnosticDescriptor(
            id: "VMWG0012",
            title: "Event is not defined in source type",
            messageFormat: "'{0}' event is not defined in source '{1}' type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

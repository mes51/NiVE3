using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    class ViewModelWireableDiagnosticDescriptors
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

        public static readonly DiagnosticDescriptor SourcePropertyTypeIsNotIINotifyPropertyChanged = new DiagnosticDescriptor(
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

    class ManualViewModelWireableDiagnosticDescriptors
    {
        const string Category = "ManualViewModelWireGenerate";

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "MVMWG0001",
            title: "Manual wireable ViewModel must be partial",
            messageFormat: "The manual wireable ViewModel '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TypeIsNotINotifyPropertyChanged = new DiagnosticDescriptor(
            id: "MVMWG0002",
            title: "Type is not derived INotifyPropertyChanged",
            messageFormat: "'{0}' is not derived INotifyPropertyChanged",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TargetSourcePropertyIsNotDefined = new DiagnosticDescriptor(
            id: "MVMWG0003",
            title: "Target source property is not defined",
            messageFormat: "Target source '{0}' property is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TargetSourceIsAlreadyUsed = new DiagnosticDescriptor(
            id: "MVMWG0004",
            title: "Target source is already used",
            messageFormat: "Target source '{0}' is already used",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor TargetSourceTypeIsNotINotifyPropertyChanged = new DiagnosticDescriptor(
            id: "MVMWG0005",
            title: "Target Source type is not derived INotifyPropertyChanged",
            messageFormat: "Target source type '{0}' is not derived INotifyPropertyChanged",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor BindMethodIsNotDefined = new DiagnosticDescriptor(
            id: "MVMWG0006",
            title: "Bind method is not defined",
            messageFormat: "Bind method '{0}' is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor BindMethodMustBePartial = new DiagnosticDescriptor(
            id: "MVMWG0007",
            title: "Bind method must be partial",
            messageFormat: "Bind method '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor BindMethodIsAlreadyUsed = new DiagnosticDescriptor(
            id: "MVMWG0008",
            title: "Already used bind method",
            messageFormat: "Bind method '{0}' is already used",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor UnbindMethodIsNotDefined = new DiagnosticDescriptor(
            id: "MVMWG0009",
            title: "Unbind method is not defined",
            messageFormat: "Unbind method '{0}' is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor UnbindMethodMustBePartial = new DiagnosticDescriptor(
            id: "MVMWG0010",
            title: "Unbind method must be partial",
            messageFormat: "Unbind method '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor UnbindMethodIsAlreadyUsed = new DiagnosticDescriptor(
            id: "MVMWG0011",
            title: "Already used unbind method",
            messageFormat: "Unbind method '{0}' is already used",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor MustDefinredGetterAndSetter = new DiagnosticDescriptor(
            id: "MVMWG0012",
            title: "Property must define getter and setter",
            messageFormat: "'{0}' property must define getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor DetectUntargetedSource = new DiagnosticDescriptor(
            id: "MVMWG0013",
            title: "Detect use untargeted source",
            messageFormat: "'{0}' is untargeted source",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourceTypePropertyIsNotDefined = new DiagnosticDescriptor(
            id: "MVMWG0014",
            title: "Source type property is not defined",
            messageFormat: "Source '{0}' type '{1}' property is not defined",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor OneWaySourceTypePropertyMustDefinredGetter = new DiagnosticDescriptor(
            id: "MVMWG0015",
            title: "One way source type property must define getter",
            messageFormat: "One way source '{0}' type '{1}' property must define getter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor SourceTypePropertyMustDefinredGetterAndSetter = new DiagnosticDescriptor(
            id: "MVMWG0016",
            title: "Source type property must define getter and setter",
            messageFormat: "Source '{0}' type '{1}' property must define getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EventNameIsEmpty = new DiagnosticDescriptor(
            id: "MVMWG0017",
            title: "bind target event name is empty",
            messageFormat: "bind target event name is empty",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor EventIsNotDefinedInSourceType = new DiagnosticDescriptor(
            id: "MVMWG0018",
            title: "Event is not defined in source type",
            messageFormat: "'{0}' event is not defined in source '{1}' type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    }
}

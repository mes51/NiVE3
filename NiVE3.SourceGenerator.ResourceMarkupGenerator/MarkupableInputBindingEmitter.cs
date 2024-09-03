using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class MarkupableInputBindingEmitter : MarkupEmitterBase
    {
        protected override string AttributeName => "MarkupableBindingAttribute";

        public override void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource(AttributeName, $"namespace {Namespace};" + $$"""

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class {{AttributeName}} : Attribute
{
    public bool IsPublic { get; set; }

    public string? OverrideExtensionName { get; set; }

    public string? InstancePropertyName { get; set; }

    public Type? RoutingCommandOwnerType { get; set; }

    public string? RoutingCommandName { get; set; }
}
""");
        }

        protected override string EmitProvideValueBody(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData)
        {
            var instancePropertyName = (string?)markupableAttributeData.NamedArguments.FirstOrDefault(a => a.Key == "InstancePropertyName").Value.Value;
            var routingCommandOwnerType = (ITypeSymbol?)markupableAttributeData.NamedArguments.FirstOrDefault(a => a.Key == "RoutingCommandOwnerType").Value.Value;
            var routingCommandName = (string?)markupableAttributeData.NamedArguments.FirstOrDefault(a => a.Key == "RoutingCommandName").Value.Value ?? "";

            if (instancePropertyName != null && routingCommandOwnerType != null)
            {
                return $$"""
        var key = ResourceKey.ToString();
        var instance = {{typeSymbol.ToDisplayString()}}.{{instancePropertyName}};
        var gesture = (System.Windows.Input.InputGesture?)typeof({{typeSymbol.ToDisplayString()}}).GetProperty(key)?.GetValue(instance);
        if (gesture == null)
        {
            return null;
        }
        var command = new System.Windows.Input.RoutedCommand("{{routingCommandName}}", typeof({{routingCommandOwnerType.ToDisplayString()}}));
        var dp = (DependencyProperty?)typeof({{typeSymbol.ToDisplayString()}}).GetField(key + "Property", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);

        System.Windows.Input.InputBinding inputBinding;
        if (dp != null)
        {
            var bindable = new GestureBindableInputBinding(command, gesture)
            {
                CommandParameter = key
            };
            var gestureBinding = new Binding(key) { Source = instance };
            BindingOperations.SetBinding(bindable, GestureBindableInputBinding.BindableGestureProperty, gestureBinding);
            inputBinding = bindable;
        }
        else
        {
            inputBinding = new System.Windows.Input.InputBinding(command, gesture);
        }

        if (ReturnDynamicResource)
        {
            return new Binding
            {
                Source = inputBinding
            };
        }
        else
        {
            return inputBinding;
        }
""";
            }
            else
            {
                return "return null;";
            }
        }

        protected override string GetMarkupExtensionName(string baseTypeName)
        {
            return $"{baseTypeName}Extension";
        }

        protected override string GetMarkupResourceTypeName(string baseTypeName)
        {
            return $"{baseTypeName}ResourceType";
        }

        protected override ISymbol[] GetTargetSymbols(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup)
        {
            return typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { DeclaredAccessibility: Accessibility.Public, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .Where(p => p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, showInMarkup)))
                .ToArray();
        }

        protected override Diagnostic? Validate(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup, AttributeData markupableAttributeData)
        {
            if (!Util.IsInherit(typeSymbol, "System.Windows.DependencyObject"))
            {
                return Diagnostic.Create(DiagnosticDescriptors.GestureDefinitionTypeIsNotDependencyObject, syntax.Identifier.GetLocation(), typeSymbol.Name);
            }

            var instancePropertyName = (string?)markupableAttributeData.NamedArguments.FirstOrDefault(a => a.Key == "InstancePropertyName").Value.Value ?? "";
            if (typeSymbol.GetMembers().OfType<IPropertySymbol>().All(p => p.Name != instancePropertyName))
            {
                return Diagnostic.Create(DiagnosticDescriptors.GestureDefinitionTypeIsNotDefinedInstanceProperty, syntax.Identifier.GetLocation(), typeSymbol.Name);
            }

            foreach (var member in typeSymbol.GetMembers().Where(m => m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, showInMarkup))))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IPropertySymbol property)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.GestureShowInMarkupableIsMustBeProperty, member.Locations.First(), typeSymbol.Name, member.Name);
                }

                if (!Util.IsInherit(property.Type, "System.Windows.Input.InputGesture"))
                {
                    return Diagnostic.Create(DiagnosticDescriptors.GesturePropertyIsNotInputGesture, property.Locations.First(), typeSymbol.Name, property.Name);
                }
                if (property.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.GesturePropertyMustDefinredGetter, property.Locations.First(), typeSymbol.Name, property.Name);
                }
            }

            return null;
        }

        protected override string DefineExtra(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData)
        {
            return $$"""
file class GestureBindableInputBinding : System.Windows.Input.InputBinding
{
    public static readonly DependencyProperty BindableGestureProperty = DependencyProperty.Register(
        nameof(BindableGesture),
        typeof(System.Windows.Input.InputGesture),
        typeof(GestureBindableInputBinding),
        new PropertyMetadata(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.None, System.Windows.Input.ModifierKeys.None), GestureChanged)
    );

    public System.Windows.Input.InputGesture BindableGesture
    {
        get { return (System.Windows.Input.InputGesture)GetValue(BindableGestureProperty); }
        set { SetValue(BindableGestureProperty, value); }
    }

    public override System.Windows.Input.InputGesture Gesture
    {
        get
        {
            return base.Gesture;
        }
        set
        {
            base.Gesture = value;
            SetCurrentValue(BindableGestureProperty, value);
        }
    }

    public GestureBindableInputBinding() : base() { }

    public GestureBindableInputBinding(System.Windows.Input.ICommand command, System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers) : this(command, new System.Windows.Input.KeyGesture(key, modifiers)) { }

    public GestureBindableInputBinding(System.Windows.Input.ICommand command, System.Windows.Input.InputGesture gesture) : base(command, gesture)
    {
        BindableGesture = gesture;
    }

    static void GestureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is GestureBindableInputBinding inputBinding && e.NewValue is System.Windows.Input.InputGesture newInput)
        {
            inputBinding.Gesture = newInput;
        }
    }

    protected override Freezable CreateInstanceCore()
    {
        return new GestureBindableInputBinding();
    }
}
""";
        }
    }
}

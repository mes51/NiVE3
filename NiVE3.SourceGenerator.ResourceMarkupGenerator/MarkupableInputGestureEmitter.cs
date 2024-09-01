using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class MarkupableInputGestureEmitter : MarkupEmitterBase
    {
        protected override string AttributeName => "MarkupableGestureAttribute";

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
}
""");
        }

        protected override string EmitProvideValueBody(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData)
        {
            var instancePropertyName = (string?)markupableAttributeData.NamedArguments.FirstOrDefault(a => a.Key == "InstancePropertyName").Value.Value;

            if (instancePropertyName != null)
            {
                return $$"""
        var key = ResourceKey.ToString();
        var instance = {{typeSymbol.ToDisplayString()}}.{{instancePropertyName}};

        if (ReturnDynamicResource)
        {
            return new Binding
            {
                Path = new PropertyPath(key),
                Source = instance,
                Mode = BindingMode.OneWay
            };
        }
        else
        {
            return typeof({{typeSymbol.ToDisplayString()}}).GetProperty(key)?.GetValue(instance);
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
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    class MarkupableResourceDictionaryEmitter : MarkupEmitterBase
    {
        protected override string AttributeName => "MarkupableResourceDictionaryAttribute";

        protected override string EmitProvideValueBody(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData)
        {
            return $$"""
        var key = typeof({{typeSymbol.ToDisplayString()}}).GetField(ResourceKey.ToString())?.GetValue(null) as string;
        if (key != null)
        {
            var dr = new DynamicResourceExtension(key);
            if (ReturnDynamicResource)
            {
                return dr;
            }
            else
            {
                return dr.ProvideValue(serviceProvider);
            }
        }
        else
        {
            return null;
        }
""";
        }

        protected override string GetMarkupExtensionName(string baseTypeName)
        {
            return $"{baseTypeName.Replace("ResourceDictionary", "")}ResourceExtension";
        }

        protected override string GetMarkupResourceTypeName(string baseTypeName)
        {
            return $"{baseTypeName.Replace("ResourceDictionary", "")}ResourceType";
        }

        protected override ISymbol[] GetTargetSymbols(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup)
        {
            return typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f is { IsStatic: true, DeclaredAccessibility: Accessibility.Public, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .Where(f => f.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, showInMarkup)))
                .ToArray();
        }

        protected override Diagnostic? Validate(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup, AttributeData markupableAttributeData)
        {
            if (!Util.IsInherit(typeSymbol, "System.Windows.ResourceDictionary"))
            {
                return Diagnostic.Create(DiagnosticDescriptors.ResourceTypeIsNotResourceDictionary, syntax.Identifier.GetLocation(), typeSymbol.Name);
            }

            foreach (var member in typeSymbol.GetMembers().Where(m => m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, showInMarkup))))
            {
                if (member is not IFieldSymbol)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.ResourceShowInMarkupableIsMustBeField, syntax.Identifier.GetLocation(), typeSymbol.Name, member.Name);
                }
            }

            return null;
        }
    }
}

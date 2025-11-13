using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ReactivePropertyGenerator
{
    static class ReactivePropertyEmitter
    {
        static readonly string Namespace = typeof(ReactivePropertyGenerator).Namespace;

        static readonly string UseReactivePropertyFullName = $"{Namespace}.UseReactivePropertyAttribute";

        static readonly string ReactivePropertyFullName = $"{Namespace}.ReactivePropertyAttribute";

        public static void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("UseReactivePropertyAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class UseReactivePropertyAttribute : Attribute { }
""");

            context.AddSource("ReactivePropertyAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
sealed class ReactivePropertyAttribute : Attribute { }
""");
        }

        public static void RegisterEmit(IncrementalGeneratorInitializationContext context)
        {
            var typeDefinitions = context.SyntaxProvider.ForAttributeWithMetadataName(
                UseReactivePropertyFullName,
                static (node, token) => node is ClassDeclarationSyntax,
                static (context, token) => (TypeDeclarationSyntax)context.TargetNode
            );

            var source = typeDefinitions.Combine(context.CompilationProvider).WithComparer(Comparer.Instance);

            context.RegisterSourceOutput(source, (context, source) =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Emit(context, source.Item1, source.Item2);
            });
        }

        static void Emit(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var useReactivePropertySymbol = EmitterUtil.GetTypeSymbol(compilation, UseReactivePropertyFullName);
            var ReactivePropertySymbol = EmitterUtil.GetTypeSymbol(compilation, ReactivePropertyFullName);

            context.CancellationToken.ThrowIfCancellationRequested();

            if (!IsDefinedSetProperty(compilation, typeSymbol))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ReactivePropertyDiagnosticDescriptors.SetPropertyMethodIsNotDefined, syntax.Identifier.GetLocation(), typeSymbol.Name)
                );
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            // クラスに適用されているかのチェックのみ実行
            EmitterUtil.GetAttributeData(typeSymbol, useReactivePropertySymbol);

            context.CancellationToken.ThrowIfCancellationRequested();

            var properties = new List<(IPropertySymbol, Accessibility, Accessibility)>();
            foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ReactivePropertySymbol))))
            {
                if (!property.IsPartialDefinition)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ReactivePropertyDiagnosticDescriptors.PropertyIsNotPartial, property.Locations.First(), property.Name)
                    );
                    return;
                }
                if (property.GetMethod == null || property.SetMethod == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ReactivePropertyDiagnosticDescriptors.PropertyNotDefinedGetterAndSetter, property.Locations.First(), property.Name)
                    );
                    return;
                }
                properties.Add((property, property.GetMethod.DeclaredAccessibility, property.SetMethod.DeclaredAccessibility));
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var fileName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "")
                .Replace("<", "_")
                .Replace(">", "_") + ".ReactivePropertyGenerator.g.cs";

            var code = new StringBuilder($$"""
// <auto-generated/>
#nullable enable
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

using System;
using System.Windows;
using NiVE3.Plugin.Resource;

namespace {{typeSymbol.ContainingNamespace}};

partial class {{typeSymbol.Name}}
{

""");

            foreach (var (property, getterAccessibility, setterAccessibility) in properties)
            {
                code.AppendLine($$"""
                    {{GetAccessibilityKeyword(property.DeclaredAccessibility)}} partial {{property.Type}} {{property.Name}}
                    {
                        {{(getterAccessibility != property.DeclaredAccessibility ? $"{GetAccessibilityKeyword(getterAccessibility)} " : "")}}get;
                        {{(setterAccessibility != property.DeclaredAccessibility ? $"{GetAccessibilityKeyword(setterAccessibility)} " : "")}}set { SetProperty(ref field, value); }
                    }
                """);
            }

            code.AppendLine("""
}
""");

            context.AddSource(fileName, code.ToString());
        }

        static string GetAccessibilityKeyword(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedAndInternal => "protected internal",
                _ => "private"
            };
        }

        static bool IsDefinedSetProperty(Compilation compilation, ITypeSymbol? type)
        {
            while (type != null)
            {
                foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method.Name != "SetProperty" ||
                        method.TypeParameters.Length != 1 ||
                        method.Parameters.Length < 2 ||
                        method.Parameters[0].Type is not ITypeParameterSymbol ||
                        method.Parameters[0].RefKind != RefKind.Ref ||
                        method.Parameters[1].Type is not ITypeParameterSymbol ||
                        method.Parameters[1].RefKind != RefKind.None)
                    {
                        continue;
                    }

                    if (method.Parameters.Length > 2 && !method.Parameters.Skip(2).All(p => p.HasExplicitDefaultValue))
                    {
                        continue;
                    }

                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
{
    abstract class MarkupEmitterBase
    {
        static protected readonly string Namespace = typeof(MarkupableResourceDictionaryEmitter).Namespace;

        protected abstract string AttributeName { get; }

        protected string AttributeFullName => $"{Namespace}.{AttributeName}";

        public virtual void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource(AttributeName, $"namespace {Namespace};" + $$"""

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class {{AttributeName}} : Attribute
{
    public bool IsPublic { get; set; }

    public string? OverrideExtensionName { get; set; }
}
""");
        }

        public void RegisterEmit(IncrementalGeneratorInitializationContext context)
        {
            var typeDefinitions = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeFullName,
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

        protected abstract Diagnostic? Validate(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup, AttributeData markupableAttributeData);

        protected abstract string GetMarkupExtensionName(string baseTypeName);

        protected abstract string GetMarkupResourceTypeName(string baseTypeName);

        protected abstract string EmitProvideValueBody(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData);

        protected abstract ISymbol[] GetTargetSymbols(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol showInMarkup);

        protected virtual string DefineExtra(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData markupableAttributeData)
        {
            return "";
        }

        void Emit(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel == null)
            {
                return;
            }

            if (semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            var markupable = compilation.GetTypeByMetadataName(AttributeFullName);
            if (markupable == null)
            {
                throw new InvalidOperationException($"{AttributeFullName} is not found");
            }

            var showInMarkup = compilation.GetTypeByMetadataName(ShowInMarkupAttributeEmitter.FullName);
            if (showInMarkup == null)
            {
                throw new InvalidOperationException($"{ShowInMarkupAttributeEmitter.FullName} is not found");
            }

            var markupableAttribute = typeSymbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(markupable, a.AttributeClass));
            if (markupableAttribute == null)
            {
                throw new InvalidOperationException($"processing class is not applied {AttributeName}"); // may be bug
            }

            var validateResult = Validate(context, syntax, compilation, typeSymbol, showInMarkup, markupableAttribute);
            if (validateResult != null)
            {
                context.ReportDiagnostic(validateResult);
                return;
            }

            var isPublic = (bool?)markupableAttribute.NamedArguments.FirstOrDefault(a => a.Key == "IsPublic").Value.Value ?? false;

            var overridedExtensionName = (string?)markupableAttribute.NamedArguments.FirstOrDefault(a => a.Key == "OverrideExtensionName").Value.Value;

            var ns = $"namespace {typeSymbol.ContainingNamespace}.Wpf.GeneratedMarkup;";

            var fileName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "")
                .Replace("<", "_")
                .Replace(">", "_") + $".{GetType().Name}.g.cs";

            var markupTypeName = overridedExtensionName ?? GetMarkupExtensionName(typeSymbol.Name);
            var enumTypeName = overridedExtensionName != null ? $"{overridedExtensionName}ResourceType" : GetMarkupResourceTypeName(typeSymbol.Name);

            context.CancellationToken.ThrowIfCancellationRequested();

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
using System.Windows.Data;
using System.Windows.Markup;

{{ns}}

{{(isPublic ? "public " : "")}}class {{markupTypeName}} : MarkupExtension
{
    public {{enumTypeName}} ResourceKey { get; set; }

    /// <summary>
    /// DataTemplate内で使用する時はTrue、そうでない場合はFalse
    /// </summary>
    public bool ReturnDynamicResource { get; set; }

    public {{markupTypeName}}() { }

    public {{markupTypeName}}({{enumTypeName}} resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
{{EmitProvideValueBody(context, syntax, compilation, typeSymbol, markupableAttribute)}}
    }
}

{{DefineExtra(context, syntax, compilation, typeSymbol, markupableAttribute)}}

{{(isPublic ? "public " : "")}}enum {{enumTypeName}}
{

""");

            context.CancellationToken.ThrowIfCancellationRequested();

            var fields = GetTargetSymbols(context, syntax, compilation, typeSymbol, showInMarkup);

            context.CancellationToken.ThrowIfCancellationRequested();

            code.AppendLine(string.Join(",\r\n", fields.Select(f => $"    {f.Name}")));

            code.AppendLine("}");

            context.AddSource(fileName, code.ToString());
        }
    }
}

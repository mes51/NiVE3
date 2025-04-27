using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    static class ViewModelWireableEmitter
    {
        static readonly string Namespace = typeof(PropertyAutoWireGenerator).Namespace;

        static readonly string ViewModelWireableAttributeName = $"{Namespace}.ViewModelWireableAttribute";

        static readonly string NeedWireAttributeName = $"{Namespace}.NeedWireAttribute";

        public static void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("ViewModelWireableAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ViewModelWireableAttribute : Attribute
{
    public string BindMethodName { get; }

    public bool WithInitializeProperty { get; set; }

    public bool KeepStrongReferenceBinder { get; set; }

    public ViewModelWireableAttribute(string bindMethodName)
    {
        BindMethodName = bindMethodName;
    }
}
""");

            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("NeedWireAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
sealed class NeedWireAttribute : Attribute
{
    public string SourceName { get; }

    public string? BindTargetName { get; set; }

    public bool IsOneWay { get; set; }

    public NeedWireAttribute(string sourceName)
    {
        SourceName = sourceName;
    }
}
""");
        }

        public static void RegisterEmit(IncrementalGeneratorInitializationContext context)
        {
            var typeDefinitions = context.SyntaxProvider.ForAttributeWithMetadataName(
                ViewModelWireableAttributeName,
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

            var needWireSymbol = EmitterUtil.GetTypeSymbol(compilation, NeedWireAttributeName);
            var viewModelWireableSymbol = EmitterUtil.GetTypeSymbol(compilation, ViewModelWireableAttributeName);

            context.CancellationToken.ThrowIfCancellationRequested();

            var wireableAttribute = EmitterUtil.GetAttributeData(typeSymbol, viewModelWireableSymbol).First();

            context.CancellationToken.ThrowIfCancellationRequested();

            if (!Validate(context, syntax, compilation, typeSymbol, wireableAttribute))
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var propertyHandlers = GetPropertyWireHandler(context, compilation, typeSymbol, needWireSymbol);

            context.CancellationToken.ThrowIfCancellationRequested();

            context.CancellationToken.ThrowIfCancellationRequested();

            if (propertyHandlers == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var withInitializeProperty = (bool?)wireableAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WithInitializeProperty").Value.Value ?? false;
            var keepStrongReferenceBinder = (bool?)wireableAttribute.NamedArguments.FirstOrDefault(a => a.Key == "KeepStrongReferenceBinder").Value.Value ?? false;
            var (binderClasses, bindingCodes) = GenerateBinderCode(typeSymbol, propertyHandlers.Values.ToArray(), withInitializeProperty, keepStrongReferenceBinder);

            context.CancellationToken.ThrowIfCancellationRequested();

            var fileName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "")
                .Replace("<", "_")
                .Replace(">", "_") + ".PropertyAutoWireGenerator.g.cs";

            var code = $$"""
// <auto-generated/>
#nullable enable
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS1522

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Reflection;

namespace {{typeSymbol.ContainingNamespace}};

{{binderClasses}}

partial class {{typeSymbol.Name}}
{
    partial void {{wireableAttribute.ConstructorArguments[0].Value ?? ""}}()
    {
{{bindingCodes}}
    }
}
""";
            context.AddSource(fileName, code);
        }

        static bool Validate(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData wireableAttribute)
        {
            if (!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.MustBePartial, syntax.Identifier.GetLocation(), typeSymbol.Name)
                );
                return false;
            }

            if (!EmitterUtil.IsInheritINotifyPropertyChanged(compilation, typeSymbol))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.TypeIsNotINotifyPropertyChanged, syntax.Identifier.GetLocation(), typeSymbol.Name)
                );
                return false;
            }

            var wiringMethodName = (wireableAttribute.ConstructorArguments[0].Value as string) ?? "";
            var wiringMethod = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == wiringMethodName);
            if (wiringMethod == null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.WiringMethodIsNotDefined, syntax.Identifier.GetLocation(), wiringMethodName)
                );
                return false;
            }
            else if (!wiringMethod.IsPartialDefinition)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.WiringMethodMustBePartial, syntax.Identifier.GetLocation(), wiringMethodName)
                );
                return false;
            }

            return true;
        }

        static Dictionary<string, Handlers>? GetPropertyWireHandler(SourceProductionContext context, Compilation compilation, ITypeSymbol typeSymbol, INamedTypeSymbol needWireSymbol)
        {
            var allProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .ToArray();
            var targetProperties = allProperties.Where(p => p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, needWireSymbol)));

            var checkedSource = new Dictionary<string, (IPropertySymbol[], HashSet<string>, HashSet<string>)>();
            var handlers = new Dictionary<string, Handlers>();
            foreach (var p in targetProperties)
            {
                if (!IsCanAccessGetter(p) || !IsCanAccessSetter(p))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.MustDefinredGetterAndSetter, p.Locations.First(), p.Name)
                    );
                    return null;
                }
                var attribute = p.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, needWireSymbol));

                var sourceName = (attribute.ConstructorArguments[0].Value as string) ?? "";
                var sourceProperty = allProperties.FirstOrDefault(p => p.Name == sourceName);
                if (sourceProperty == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.SourcePropertyIsNotDefined, p.Locations.First(), sourceName)
                    );
                    return null;
                }

                var sourcePropertyName = (attribute.NamedArguments.FirstOrDefault(n => n.Key == "BindTargetName").Value.Value as string) ?? p.Name;
                var isOneWay = (bool)(attribute.NamedArguments.FirstOrDefault(n => n.Key == "IsOneWay").Value.Value ?? false);

                if (!checkedSource.ContainsKey(sourceName))
                {
                    if (!EmitterUtil.IsInheritINotifyPropertyChanged(compilation, sourceProperty.Type))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.SourcePropertyTypeIsNotIINotifyPropertyChanged, p.Locations.First(), sourceName)
                        );
                        return null;
                    }

                    var sourceTypeProperties = sourceProperty.Type
                        .GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                        .ToArray();
                    checkedSource.Add(sourceName, (sourceTypeProperties, new HashSet<string>(), new HashSet<string>()));
                }
                if (isOneWay && !checkedSource[sourceName].Item3.Contains(sourcePropertyName))
                {
                    var (sourceTypeProperties, checkedSourceProperties, checkedOneWaySourceProperties) = checkedSource[sourceName];
                    var sp = sourceTypeProperties.FirstOrDefault(p => p.Name == sourcePropertyName);
                    if (sp == null)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.OneWaySourceTypePropertyMustDefinredGetter, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }

                    checkedOneWaySourceProperties.Add(sourcePropertyName);
                }
                else if (!isOneWay && !checkedSource[sourceName].Item2.Contains(sourcePropertyName))
                {
                    var (sourceTypeProperties, checkedSourceProperties, checkedOneWaySourceProperties) = checkedSource[sourceName];
                    var sp = sourceTypeProperties.FirstOrDefault(p => p.Name == sourcePropertyName);
                    if (sp == null)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp) || !IsCanAccessSetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ViewModelWireableDiagnosticDescriptors.SourceTypePropertyMustDefinredGetterAndSetter, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }

                    checkedSourceProperties.Add(sourcePropertyName);
                    checkedOneWaySourceProperties.Add(sourcePropertyName);
                }

                if (!handlers.ContainsKey(sourceName))
                {
                    handlers.Add(sourceName, new Handlers(sourceName, sourceProperty.Type));
                }

                handlers[sourceName].PropertyWires.Add(new PropertyWire(p.Name, sourcePropertyName, isOneWay));

                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return handlers;
        }

        static (string, string) GenerateBinderCode(INamedTypeSymbol typeSymbol, Handlers[] handlers, bool withInitializeProperty, bool keepStrongReferenceBinder)
        {
            var binderClasses = new StringBuilder();
            var bindingCodes = new StringBuilder();
            foreach (var h in handlers)
            {
                var modelHandler = new StringBuilder();
                var viewModelHandler = new StringBuilder();
                foreach (var wire in h.PropertyWires)
                {
                    modelHandler.AppendLine($$"""
                case nameof({{h.SourceType}}.{{wire.SourcePropertyName}}):
                    viewModel.{{wire.PropertyName}} = model.{{wire.SourcePropertyName}};
                    break;
""");

                    if (!wire.IsOneWay)
                    {
                        viewModelHandler.AppendLine($$"""
                case nameof({{typeSymbol}}.{{wire.PropertyName}}):
                    model.{{wire.SourcePropertyName}} = viewModel.{{wire.PropertyName}};
                    break;
""");
                    }

                    if (withInitializeProperty)
                    {
                        bindingCodes.AppendLine($"        {wire.PropertyName} = {h.SourceName}.{wire.SourcePropertyName};");
                    }
                }

                var binderTypeName = $"{typeSymbol.Name}_{h.SourceName}Binder";
                binderClasses.AppendLine($$"""
file class {{binderTypeName}}
{
    static List<{{binderTypeName}}> Instances { get; } = [];

    WeakReference<{{typeSymbol}}> ViewModel { get; }

    WeakReference<{{h.SourceType}}> Model { get; }

    public {{binderTypeName}}({{typeSymbol.Name}} viewModel, {{h.SourceType}} model)
    {
        ViewModel = new WeakReference<{{typeSymbol}}>(viewModel);
        Model = new WeakReference<{{h.SourceType}}>(model);

        viewModel.PropertyChanged += ViewModelPropertyChanged;
        model.PropertyChanged += ModelPropertyChanged;

        {{(keepStrongReferenceBinder ? "Instances.Add(this);" : "")}}
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Model.TryGetTarget(out var model) && ViewModel.TryGetTarget(out var viewModel))
        {
            switch (e.PropertyName)
            {
{{viewModelHandler}}
            }
        }
        else
        {
            Unbind();
        }
    }

    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Model.TryGetTarget(out var model) && ViewModel.TryGetTarget(out var viewModel))
        {
            switch (e.PropertyName)
            {
{{modelHandler}}
            }
        }
        else
        {
            Unbind();
        }
    }

    void Unbind()
    {
        if (ViewModel.TryGetTarget(out var viewModel))
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }
        if (Model.TryGetTarget(out var model))
        {
            model.PropertyChanged -= ModelPropertyChanged;
        }

        Instances.Remove(this);
    }
}
""");
                bindingCodes.AppendLine($"        new {binderTypeName}(this, {h.SourceName});");
            }

            return (binderClasses.ToString(), bindingCodes.ToString());
        }

        static bool IsCanAccessGetter(IPropertySymbol property)
        {
            return property.GetMethod != null && property.GetMethod.DeclaredAccessibility == Accessibility.Public;
        }

        static bool IsCanAccessSetter(IPropertySymbol property)
        {
            return property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public;
        }
    }
}

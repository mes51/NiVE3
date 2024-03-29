using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    static class ManualViewModelWireableEmitter
    {
        static readonly string Namespace = typeof(PropertyAutoWireGenerator).Namespace;

        static readonly string ManualViewModelWireableAttributeName = $"{Namespace}.ManualViewModelWireableAttribute";

        static readonly string ManualWireAttributeName = $"{Namespace}.ManualWireAttribute";

        public static void RegisterAttributes(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("ManualViewModelWireableAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
sealed class ManualViewModelWireableAttribute : Attribute
{
    public string TargetSource { get; }

    public string BindMethodName { get; }

    public string UnbindMethodName { get; }

    public bool WithInitializeProperty { get; set; }

    public ManualViewModelWireableAttribute(string targetSource, string bindMethodName, string unbindMethodName)
    {
        TargetSource = targetSource;
        BindMethodName = bindMethodName;
        UnbindMethodName = unbindMethodName;
    }
}
""");

            context.CancellationToken.ThrowIfCancellationRequested();

            context.AddSource("ManualWireAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
sealed class ManualWireAttribute : Attribute
{
    public string SourceName { get; }

    public string? BindTargetName { get; set; }

    public bool IsOneWay { get; set; }

    public ManualWireAttribute(string sourceName)
    {
        SourceName = sourceName;
    }
}
""");
        }

        public static void RegisterEmit(IncrementalGeneratorInitializationContext context)
        {
            var typeDefinitions = context.SyntaxProvider.ForAttributeWithMetadataName(
                ManualViewModelWireableAttributeName,
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

            var manualWireSymbol = EmitterUtil.GetTypeSymbol(compilation, ManualWireAttributeName);
            var manualViewModelWireableSymbol = EmitterUtil.GetTypeSymbol(compilation, ManualViewModelWireableAttributeName);

            context.CancellationToken.ThrowIfCancellationRequested();

            var wireableAttributes = EmitterUtil.GetAttributeData(typeSymbol, manualViewModelWireableSymbol);

            context.CancellationToken.ThrowIfCancellationRequested();

            if (!Validate(context, syntax, compilation, typeSymbol, wireableAttributes))
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var targetSources = wireableAttributes.Select(a => a.ConstructorArguments[0].Value).Cast<string>().ToArray();
            var propertyHandlers = GetPropertyWireHandler(context, compilation, typeSymbol, manualWireSymbol, targetSources);

            context.CancellationToken.ThrowIfCancellationRequested();

            if (propertyHandlers == null)
            {
                return;
            }

            foreach (var a in wireableAttributes)
            {
                if (a.ConstructorArguments[0].Value is not string sourceName)
                {
                    continue; // may be bug
                }

                var handler = propertyHandlers[sourceName];
                var withInitializeProperty = (bool?)a.NamedArguments.FirstOrDefault(a => a.Key == "WithInitializeProperty").Value.Value ?? false;
                var (binderClass, bindMethodBody, unbindMethodBody) = GenerateBinderCode(typeSymbol, handler, withInitializeProperty);

                var fileName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
    .Replace("global::", "")
    .Replace("<", "_")
    .Replace(">", "_") + $".{sourceName}.ManualPropertyAutoWireGenerator.g.cs";

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
using System.Linq;

namespace {{typeSymbol.ContainingNamespace}};

{{binderClass}}

partial class {{typeSymbol.Name}}
{
    partial void {{a.ConstructorArguments[1].Value ?? ""}}()
    {
{{bindMethodBody}}
    }

    partial void {{a.ConstructorArguments[2].Value ?? ""}}()
    {
{{unbindMethodBody}}
    }
}
""";
                context.AddSource(fileName, code);
            }
        }

        static bool Validate(SourceProductionContext context, TypeDeclarationSyntax syntax, Compilation compilation, INamedTypeSymbol typeSymbol, AttributeData[] wireableAttributes)
        {
            var targetSources = new HashSet<string>();
            var bindMethods = new HashSet<string>();
            var unbindMethods = new HashSet<string>();

            foreach (var wireableAttribute in wireableAttributes)
            {
                if (!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.MustBePartial, syntax.Identifier.GetLocation(), typeSymbol.Name)
                    );
                    return false;
                }

                if (!EmitterUtil.IsInheritINotifyPropertyChanged(compilation, typeSymbol))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.TypeIsNotINotifyPropertyChanged, syntax.Identifier.GetLocation(), typeSymbol.Name)
                    );
                    return false;
                }

                var sourceName = (wireableAttribute.ConstructorArguments[0].Value as string) ?? "";
                var sourceProperty = typeSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                    .FirstOrDefault(p => p.Name == sourceName);
                if (sourceProperty == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.TargetSourcePropertyIsNotDefined, syntax.Identifier.GetLocation(), sourceName)
                    );
                    return false;
                }

                if (targetSources.Contains(sourceName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.TargetSourceIsAlreadyUsed, syntax.Identifier.GetLocation(), sourceName)
                    );
                    return false;
                }

                if (!EmitterUtil.IsInheritINotifyPropertyChanged(compilation, sourceProperty.Type))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.TargetSourceTypeIsNotINotifyPropertyChanged, syntax.Identifier.GetLocation(), typeSymbol.Name)
                    );
                    return false;
                }

                var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>().ToArray();
                var bindMethodName = (wireableAttribute.ConstructorArguments[1].Value as string) ?? "";
                var bindMethod = allMethods.FirstOrDefault(m => m.Name == bindMethodName);
                if (bindMethod == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.BindMethodIsNotDefined, syntax.Identifier.GetLocation(), bindMethodName)
                    );
                    return false;
                }
                if (!bindMethod.IsPartialDefinition)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.BindMethodMustBePartial, syntax.Identifier.GetLocation(), bindMethodName)
                    );
                    return false;
                }
                if (bindMethods.Contains(bindMethodName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.BindMethodIsAlreadyUsed, syntax.Identifier.GetLocation(), bindMethodName)
                    );
                    return false;
                }

                var unbindMethodName = (wireableAttribute.ConstructorArguments[2].Value as string) ?? "";
                var unbindMethod = allMethods.FirstOrDefault(m => m.Name == unbindMethodName);
                if (unbindMethod == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.UnbindMethodIsNotDefined, syntax.Identifier.GetLocation(), unbindMethodName)
                    );
                    return false;
                }
                if (!unbindMethod.IsPartialDefinition)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.UnbindMethodMustBePartial, syntax.Identifier.GetLocation(), unbindMethodName)
                    );
                    return false;
                }
                if (unbindMethods.Contains(unbindMethodName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.UnbindMethodIsAlreadyUsed, syntax.Identifier.GetLocation(), unbindMethodName)
                    );
                    return false;
                }

                targetSources.Add(sourceName);
                bindMethods.Add(bindMethodName);
                unbindMethods.Add(unbindMethodName);
            }

            return true;
        }

        static Dictionary<string, Handlers>? GetPropertyWireHandler(SourceProductionContext context, Compilation compilation, ITypeSymbol typeSymbol, INamedTypeSymbol manualWireSymbol, string[] targetSources)
        {
            var allProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .ToArray();
            var targetProperties = allProperties.Where(p => p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, manualWireSymbol)));

            var checkedSource = new Dictionary<string, (IPropertySymbol[], HashSet<string>, HashSet<string>)>();
            var handlers = new Dictionary<string, Handlers>();
            foreach (var p in targetProperties)
            {
                if (!IsCanAccessGetter(p) || !IsCanAccessSetter(p))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.MustDefinredGetterAndSetter, p.Locations.First(), p.Name)
                    );
                    return null;
                }
                var attribute = p.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, manualWireSymbol));

                var sourceName = (attribute.ConstructorArguments[0].Value as string) ?? "";
                if (!targetSources.Contains(sourceName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.DetectUntargetedSource, p.Locations.First(), sourceName)
                    );
                    return null;
                }
                var sourceProperty = allProperties.FirstOrDefault(p => p.Name == sourceName);

                var sourcePropertyName = (attribute.NamedArguments.FirstOrDefault(n => n.Key == "BindTargetName").Value.Value as string) ?? p.Name;
                var isOneWay = (bool)(attribute.NamedArguments.FirstOrDefault(n => n.Key == "IsOneWay").Value.Value ?? false);

                if (!checkedSource.ContainsKey(sourceName))
                {
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
                            Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.OneWaySourceTypePropertyMustDefinredGetter, p.Locations.First(), sourceName, sourcePropertyName)
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
                            Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp) || !IsCanAccessSetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(ManualViewModelWireableDiagnosticDescriptors.SourceTypePropertyMustDefinredGetterAndSetter, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }

                    checkedSourceProperties.Add(sourcePropertyName);
                    checkedOneWaySourceProperties.Add(sourcePropertyName);
                }

                if (!handlers.ContainsKey(sourceName))
                {
                    handlers.Add(sourceName, new Handlers(sourceName, sourceProperty.Type.OriginalDefinition));
                }

                handlers[sourceName].PropertyWires.Add(new PropertyWire(p.Name, sourcePropertyName, isOneWay));

                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return handlers;
        }

        static (string binderClass, string bindMethodBody, string unbindMethodBody) GenerateBinderCode(INamedTypeSymbol typeSymbol, Handlers handlers, bool withInitializeProperty)
        {
            var modelHandler = new StringBuilder();
            var viewModelHandler = new StringBuilder();
            var bindingCodes = new StringBuilder();
            foreach (var wire in handlers.PropertyWires)
            {
                modelHandler.AppendLine($$"""
                case nameof({{handlers.SourceType}}.{{wire.SourcePropertyName}}):
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
                    bindingCodes.AppendLine($"        {wire.PropertyName} = {handlers.SourceName}.{wire.SourcePropertyName};");
                }
            }

            var binderTypeName = $"{typeSymbol.Name}_{handlers.SourceName}ManualBinder";
            var binderClass = $$"""
file class {{binderTypeName}}
{
    static List<WeakReference<{{binderTypeName}}>> Binders { get; } = new List<WeakReference<{{binderTypeName}}>>();

    WeakReference<{{typeSymbol}}> ViewModel { get; }

    WeakReference<{{handlers.SourceType}}> Model { get; }

    private {{binderTypeName}}({{typeSymbol.Name}} viewModel, {{handlers.SourceType}} model)
    {
        ViewModel = new WeakReference<{{typeSymbol}}>(viewModel);
        Model = new WeakReference<{{handlers.SourceType}}>(model);

        viewModel.PropertyChanged += ViewModelPropertyChanged;
        model.PropertyChanged += ModelPropertyChanged;
    }

    public static void Bind({{typeSymbol.Name}} viewModel, {{handlers.SourceType}} model)
    {
        foreach (var weakBinder in Binders)
        {
            if (weakBinder.TryGetTarget(out var binder) && binder.IsSameBinder(viewModel, model))
            {
                return;
            }
        }
        Binders.Add(new WeakReference<{{binderTypeName}}>(new {{binderTypeName}}(viewModel, model)));
    }

    public static void Unbind({{typeSymbol.Name}} viewModel, {{handlers.SourceType}} model)
    {
        var binders = Binders.ToArray();
        foreach (var weakBinder in binders)
        {
            if (weakBinder.TryGetTarget(out var binder) && binder.IsSameBinder(viewModel, model))
            {
                binder.UnbindInstance();
                Binders.Remove(weakBinder);
                break;
            }
        }
    }

    bool IsSameBinder({{typeSymbol.Name}} viewModel, {{handlers.SourceType}} model)
    {
        return ViewModel.TryGetTarget(out var currentViewModel) &&
            Model.TryGetTarget(out var currentModel) &&
            currentViewModel == viewModel && currentModel == model;
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
            UnbindInstance();
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
            UnbindInstance();
        }
    }

    void UnbindInstance()
    {
        if (ViewModel.TryGetTarget(out var viewModel))
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }
        if (Model.TryGetTarget(out var model))
        {
            model.PropertyChanged -= ModelPropertyChanged;
        }
    }
}
""";

            var bindMethodBody = $$"""
{{bindingCodes}}
        {{binderTypeName}}.Bind(this, {{handlers.SourceName}});
""";

            var unbindMethodBody = $$"""
        {{binderTypeName}}.Unbind(this, {{handlers.SourceName}});
""";

            return (binderClass, bindMethodBody, unbindMethodBody);
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

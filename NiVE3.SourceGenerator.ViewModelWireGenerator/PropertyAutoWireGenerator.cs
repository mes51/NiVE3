using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// SEE: https://neue.cc/2022/12/16_IncrementalSourceGenerator.html

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    [Generator(LanguageNames.CSharp)]
    public partial class PropertyAutoWireGenerator : IIncrementalGenerator
    {
        static readonly string Namespace = typeof(PropertyAutoWireGenerator).Namespace;

        static readonly string NeedWireAttributeName = $"{Namespace}.NeedWireAttribute";

        static readonly string ViewModelWireableAttributeName = $"{Namespace}.ViewModelWireableAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                context.AddSource("ViewModelWireableAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ViewModelWireableAttribute : Attribute
{
    public string BindMethodName { get; }

    public bool WithInitializeProperty { get; set; }

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
            });

            var typeDefinitions = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{Namespace}.ViewModelWireableAttribute",
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

            if (semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            var needWireSymbol = compilation.GetTypeByMetadataName(NeedWireAttributeName);
            if (needWireSymbol == null)
            {
                throw new InvalidOperationException($"{NeedWireAttributeName} is not found");
            }

            var viewModelWireableSymbol = compilation.GetTypeByMetadataName(ViewModelWireableAttributeName);
            if (viewModelWireableSymbol == null)
            {
                throw new InvalidOperationException($"{ViewModelWireableAttributeName} is not found");
            }

            var wireableAttribute = typeSymbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(viewModelWireableSymbol, a.AttributeClass));
            if (wireableAttribute == null)
            {
                throw new InvalidOperationException("processing class is not applied ViewModelWireableAttribute"); // may be bug
            }
            var withInitializeProperty = (bool?)wireableAttribute.NamedArguments.FirstOrDefault(a => a.Key == "WithInitializeProperty").Value.Value ?? false;

            if (!Validate(context, syntax, compilation, typeSymbol, wireableAttribute))
            {
                return;
            }

            var handlerPairs = GetHandlerPairs(context, compilation, typeSymbol, needWireSymbol);
            if (handlerPairs == null)
            {
                return;
            }

            var (binderClasses, bindingCodes) = GenerateBinderCode(typeSymbol, handlerPairs, withInitializeProperty);

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

using System;
using System.ComponentModel;
using System.Windows;

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
                    Diagnostic.Create(DiagnosticDescriptors.MustBePartial, syntax.Identifier.GetLocation(), typeSymbol.Name)
                );
                return false;
            }

            if (!IsInheritINotifyPropertyChanged(compilation, typeSymbol))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(DiagnosticDescriptors.TypeIsNotINotifyPropertyChanged, syntax.Identifier.GetLocation(), typeSymbol.Name)
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
                    Diagnostic.Create(DiagnosticDescriptors.WiringMethodIsNotDefined, syntax.Identifier.GetLocation(), wiringMethodName)
                );
                return false;
            }
            else if (!wiringMethod.IsPartialDefinition)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(DiagnosticDescriptors.WiringMethodMustBePartial, syntax.Identifier.GetLocation(), wiringMethodName)
                );
                return false;
            }

            return true;
        }

        static bool IsInheritINotifyPropertyChanged(Compilation compilation, ITypeSymbol? type)
        {
            var notifyPropertyChangedSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
            while (type != null)
            {
                foreach (var i in type.Interfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(i, notifyPropertyChangedSymbol))
                    {
                        return true;
                    }
                }
                type = type.BaseType;
            }

            return false;
        }

        static Dictionary<string, (ITypeSymbol, List<(string propertyName, string sourcePropertyName, bool isOneWay)>)>? GetHandlerPairs(SourceProductionContext context, Compilation compilation, ITypeSymbol typeSymbol, INamedTypeSymbol needWireSymbol)
        {
            var allProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .ToArray();
            var targetProperties = allProperties.Where(p => p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, needWireSymbol)));

            var checkedSource = new Dictionary<string, (IPropertySymbol[], HashSet<string>, HashSet<string>)>();
            var handlerPair = new Dictionary<string, (ITypeSymbol, List<(string, string, bool)>)>();
            foreach (var p in targetProperties)
            {
                if (!IsCanAccessGetter(p) || !IsCanAccessSetter(p))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.MustDefinredGetterAndSetter, p.Locations.First(), p.Name)
                    );
                    return null;
                }
                var attribute = p.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, needWireSymbol));

                var sourceName = (attribute.ConstructorArguments[0].Value as string) ?? "";
                var sourceProperty = allProperties.FirstOrDefault(p => p.Name == sourceName);
                if (sourceProperty == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.SourcePropertyIsNotDefined, p.Locations.First(), sourceName)
                    );
                    return null;
                }

                var sourcePropertyName = (attribute.NamedArguments.FirstOrDefault(n => n.Key == "BindTargetName").Value.Value as string) ?? p.Name;
                var isOneWay = (bool)(attribute.NamedArguments.FirstOrDefault(n => n.Key == "IsOneWay").Value.Value ?? false);

                if (!checkedSource.ContainsKey(sourceName))
                {
                    if (!IsInheritINotifyPropertyChanged(compilation, sourceProperty.Type))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.SourcePropertyITypeIsNotINotifyPropertyChanged, p.Locations.First(), sourceName)
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
                            Diagnostic.Create(DiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.OneWaySourceTypePropertyMustDefinredGetter, p.Locations.First(), sourceName, sourcePropertyName)
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
                            Diagnostic.Create(DiagnosticDescriptors.SourceTypePropertyIsNotDefined, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }
                    else if (!IsCanAccessGetter(sp) || !IsCanAccessSetter(sp))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.SourceTypePropertyMustDefinredGetterAndSetter, p.Locations.First(), sourceName, sourcePropertyName)
                        );
                        return null;
                    }

                    checkedSourceProperties.Add(sourcePropertyName);
                    checkedOneWaySourceProperties.Add(sourcePropertyName);
                }

                if (!handlerPair.ContainsKey(sourceName))
                {
                    handlerPair.Add(sourceName, (sourceProperty.Type, new List<(string, string, bool)>()));
                }

                handlerPair[sourceName].Item2.Add((p.Name, sourcePropertyName, isOneWay));

                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return handlerPair;
        }

        static (string, string) GenerateBinderCode(INamedTypeSymbol typeSymbol, Dictionary<string, (ITypeSymbol, List<(string propertyName, string sourcePropertyName, bool isOneWay)>)> handlerPairs, bool withInitializeProperty)
        {
            var binderClasses = new StringBuilder();
            var bindingCodes = new StringBuilder();
            foreach (var (sourceName, (sourceType, properties)) in handlerPairs)
            {
                var modelHandler = new StringBuilder();
                var viewModelHandler = new StringBuilder();
                foreach (var (propertyName, sourcePropertyName, isOneWay) in properties)
                {
                    modelHandler.AppendLine($$"""
                case nameof({{sourceType}}.{{sourcePropertyName}}):
                    viewModel.{{propertyName}} = model.{{sourcePropertyName}};
                    break;
""");

                    if (!isOneWay)
                    {
                        viewModelHandler.AppendLine($$"""
                case nameof({{typeSymbol}}.{{propertyName}}):
                    model.{{sourcePropertyName}} = viewModel.{{propertyName}};
                    break;
""");
                    }

                    if (withInitializeProperty)
                    {
                        bindingCodes.AppendLine($"        {propertyName} = {sourceName}.{sourcePropertyName};");
                    }
                }

                var binderTypeName = $"{typeSymbol.Name}_{sourceName}Binder";
                binderClasses.AppendLine($$"""
file class {{binderTypeName}}
{
    WeakReference<{{typeSymbol}}> ViewModel { get; }

    WeakReference<{{sourceType}}> Model { get; }

    public {{binderTypeName}}({{typeSymbol.Name}} viewModel, {{sourceType}} model)
    {
        ViewModel = new WeakReference<{{typeSymbol}}>(viewModel);
        Model = new WeakReference<{{sourceType}}>(model);

        viewModel.PropertyChanged += ViewModelPropertyChanged;
        model.PropertyChanged += ModelPropertyChanged;
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
    }
}
""");
                bindingCodes.AppendLine($"        new {binderTypeName}(this, {sourceName});");
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

    // https://github.com/Cysharp/MemoryPack/blob/0093aa8f9ae37f15c72afbd953fa702649e915f5/src/MemoryPack.Generator/MemoryPackGenerator.cs#L266
    class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new Comparer();

        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
        {
            return x.Item1.Equals(y.Item1);
        }

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }

    static class KeyValuePairDeconstructor
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
        {
            key = kv.Key;
            value = kv.Value;
        }
    }
}

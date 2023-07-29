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

        static readonly string BindWeakEventAttributeName = $"{Namespace}.BindWeakEventAttribute";

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

                context.CancellationToken.ThrowIfCancellationRequested();

                context.AddSource("BindWeakEventAttribute", $"namespace {Namespace};" + """

using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
sealed class BindWeakEventAttribute : Attribute
{
    public string SourceName { get; }

    public string EventName { get; }

    public BindWeakEventAttribute(string sourceName, string eventName)
    {
        SourceName = sourceName;
        EventName = eventName;
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

            var bindWeakEventSymbol = compilation.GetTypeByMetadataName(BindWeakEventAttributeName);
            if (bindWeakEventSymbol == null)
            {
                throw new InvalidOperationException($"{BindWeakEventAttributeName} is not found");
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

            var propertyHandlers = GetPropertyWireHandler(context, compilation, typeSymbol, needWireSymbol);
            var bindEventHandlers = GetBindEventHandler(context, compilation, typeSymbol, bindWeakEventSymbol);
            if (propertyHandlers == null && bindEventHandlers == null)
            {
                return;
            }

            propertyHandlers ??= new Dictionary<string, Handlers>();
            bindEventHandlers ??= new Dictionary<string, Handlers>();
            foreach (var key in propertyHandlers.Keys)
            {
                if (bindEventHandlers.TryGetValue(key, out var handler))
                {
                    propertyHandlers[key].BindEventHandlers.AddRange(handler.BindEventHandlers);
                }
            }
            var allHandlers = propertyHandlers.Values.Concat(bindEventHandlers.Keys.Except(propertyHandlers.Keys).Select(k => bindEventHandlers[k])).ToList();

            var (binderClasses, bindingCodes) = GenerateBinderCode(typeSymbol, allHandlers, withInitializeProperty);

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

                if (!handlers.ContainsKey(sourceName))
                {
                    handlers.Add(sourceName, new Handlers(sourceName, sourceProperty.Type));
                }

                handlers[sourceName].PropertyWires.Add(new PropertyWire(p.Name, sourcePropertyName, isOneWay));

                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return handlers;
        }

        static Dictionary<string, Handlers>? GetBindEventHandler(SourceProductionContext context, Compilation compilation, ITypeSymbol typeSymbol, INamedTypeSymbol bindWeakEventSymbol)
        {
            var allProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .ToArray();
            var targetMethods = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
                .Where(m => m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bindWeakEventSymbol)))
                .ToArray();

            var checkedEvents = new Dictionary<string, HashSet<string>>();
            var handlers = new Dictionary<string, Handlers>();
            foreach(var m in targetMethods)
            {
                var attribute = m.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bindWeakEventSymbol));

                var sourceName = (attribute.ConstructorArguments[0].Value as string) ?? "";
                var sourceProperty = allProperties.FirstOrDefault(p => p.Name == sourceName);
                if (sourceProperty == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.SourcePropertyIsNotDefined, m.Locations.First(), sourceName)
                    );
                    return null;
                }

                var eventName = attribute.ConstructorArguments[1].Value as string;
                if (eventName == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.EventNameIsEmpty, m.Locations.First())
                    );
                    return null;
                }
                if (!checkedEvents.TryGetValue(sourceName, out var checkedEventNames) || !checkedEventNames.Contains(eventName))
                {
                    if (!checkedEvents.ContainsKey(sourceName))
                    {
                        checkedEvents.Add(sourceName, new HashSet<string>());
                    }

                    var exists = sourceProperty.Type
                        .GetMembers()
                        .OfType<IEventSymbol>()
                        .Any(p => p is { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true } && p.Name == eventName);
                    if (exists)
                    {
                        checkedEvents[sourceName].Add(eventName);
                    }
                    else
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.EventIsNotDefinedInSourceType, m.Locations.First(), eventName, sourceName)
                        );
                        return null;
                    }
                }

                if (!handlers.ContainsKey(sourceName))
                {
                    handlers.Add(sourceName, new Handlers(sourceName, sourceProperty.Type));
                }
                handlers[sourceName].BindEventHandlers.Add(new BindEventHandler(eventName, m));
            }

            return handlers;
        }

        static (string, string) GenerateBinderCode(INamedTypeSymbol typeSymbol, List<Handlers> handlers, bool withInitializeProperty)
        {
            var binderClasses = new StringBuilder();
            var bindingCodes = new StringBuilder();
            var addHandlerCodes = new StringBuilder();
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

                var bindEventHandlerToModel = new StringBuilder();
                var handlerMethods = new StringBuilder();
                var generatedHandlers = new HashSet<string>();
                foreach (var bind in h.BindEventHandlers)
                {
                    if (!generatedHandlers.Contains(bind.EventName))
                    {
                        bindEventHandlerToModel.AppendLine($"        model.{bind.EventName} += Binder_{bind.EventName};");
                        handlerMethods.AppendLine($$"""
    void Binder_{{bind.EventName}}({{bind.HandlerSymbol.Parameters[0].Type}} sender, {{bind.HandlerSymbol.Parameters[1].Type}} e)
    {
        foreach (var l in EventListeners["{{bind.EventName}}"])
        {
            l.Invoke(sender, e);
        }
    }
""");
                        generatedHandlers.Add(bind.EventName);
                    }

                    addHandlerCodes.AppendLine($"            binder.AddWeakEventHandler(\"{bind.EventName}\", {bind.HandlerSymbol.Name});");
                }

                var binderTypeName = $"{typeSymbol.Name}_{h.SourceName}Binder";
                binderClasses.AppendLine($$"""
file class {{binderTypeName}}
{
    WeakReference<{{typeSymbol}}> ViewModel { get; }

    WeakReference<{{h.SourceType}}> Model { get; }

    Dictionary<string, List<{{binderTypeName}}_WeakAction>> EventListeners { get; } = new Dictionary<string, List<{{binderTypeName}}_WeakAction>>();

    public {{binderTypeName}}({{typeSymbol.Name}} viewModel, {{h.SourceType}} model)
    {
        ViewModel = new WeakReference<{{typeSymbol}}>(viewModel);
        Model = new WeakReference<{{h.SourceType}}>(model);

        viewModel.PropertyChanged += ViewModelPropertyChanged;
        model.PropertyChanged += ModelPropertyChanged;
{{bindEventHandlerToModel}}
    }

    public void AddWeakEventHandler(string eventName, Delegate handler)
    {
        if (!EventListeners.ContainsKey(eventName))
        {
            EventListeners.Add(eventName, new List<{{binderTypeName}}_WeakAction>());
        }
        EventListeners[eventName].Add(new {{binderTypeName}}_WeakAction(handler));
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

{{handlerMethods}}
}

file class {{binderTypeName}}_WeakAction
{
    public MethodInfo Body { get; }

    public object Target => WeakTarget.Target;

    WeakReference WeakTarget { get; }

    public {{binderTypeName}}_WeakAction(Delegate action)
    {
        Body = action.Method;
        WeakTarget = new WeakReference(action.Target);
    }

    public void Invoke(object arg1, object arg2)
    {
        var target = WeakTarget.Target;
        if (target != null)
        {
            Body.Invoke(target, new object[] { arg1, arg2 });
        }
    }
}
""");
                if (bindEventHandlerToModel.Length > 0)
                {
                    bindingCodes.AppendLine($$"""
        {
            var binder = new {{binderTypeName}}(this, {{h.SourceName}});
{{addHandlerCodes}}
        }
""");
                }
                else
                {
                    bindingCodes.AppendLine($"        new {binderTypeName}(this, {h.SourceName});");
                }
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ViewModelWireGenerator
{
    class Handlers
    {
        public string SourceName { get; }

        public ITypeSymbol SourceType { get; }

        public List<PropertyWire> PropertyWires { get; } = new List<PropertyWire>();

        public List<BindEventHandler> BindEventHandlers { get; } = new List<BindEventHandler>();

        public Handlers(string sourceName, ITypeSymbol sourceType)
        {
            SourceName = sourceName;
            SourceType = sourceType;
        }
    }

    class PropertyWire
    {
        public string PropertyName { get; }

        public string SourcePropertyName { get; }

        public bool IsOneWay { get; }

        public PropertyWire(string propertyName, string sourcePropertyName, bool isOneWay)
        {
            PropertyName = propertyName;
            SourcePropertyName = sourcePropertyName;
            IsOneWay = isOneWay;
        }
    }

    public class BindEventHandler
    {
        public string EventName { get; }

        public IMethodSymbol HandlerSymbol { get; }

        public BindEventHandler(string eventName, IMethodSymbol handlerSymbol)
        {
            EventName = eventName;
            HandlerSymbol = handlerSymbol;
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

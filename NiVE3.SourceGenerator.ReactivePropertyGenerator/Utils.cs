using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NiVE3.SourceGenerator.ReactivePropertyGenerator
{
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
    static class EmitterUtil
    {
        public static INamedTypeSymbol GetTypeSymbol(Compilation compilation, string symbolName)
        {
            var symbol = compilation.GetTypeByMetadataName(symbolName);
            if (symbol == null)
            {
                throw new InvalidOperationException($"{symbolName} is not found");
            }

            return symbol;
        }

        public static AttributeData[] GetAttributeData(ITypeSymbol targetTypeSymbol, ITypeSymbol attributeTypeSymbol)
        {
            var attributeData = targetTypeSymbol
                .GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(attributeTypeSymbol, a.AttributeClass))
                .ToArray();
            if (attributeData.Length < 1)
            {
                throw new InvalidOperationException($"processing class is not applied {attributeTypeSymbol.Name}");
            }

            return attributeData;
        }
    }
}

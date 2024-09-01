using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace NiVE3.SourceGenerator.ResourceMarkupGenerator
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

    static class Util
    {
        public static bool IsInherit(ITypeSymbol typeSymbol, string targetTypeFullName)
        {
            var baseType = typeSymbol;
            while (baseType != null)
            {
                if (baseType.ToString() == targetTypeFullName)
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}

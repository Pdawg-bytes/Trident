using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Trident.CodeGeneration.Attributes
{
    public static class ARMAttributeParser
    {
        public static List<(string Name, string Type)> Parse(IMethodSymbol method)
        {
            var results = new List<(string, string)>();

            foreach (var attrData in method.GetAttributes())
            {
                var attrClass = attrData.AttributeClass;
                if (!attrClass?.Name.StartsWith("ARMParameterAttribute") ?? true)
                    continue;

                string name = attrData.ConstructorArguments[0].Value?.ToString();
                string genericArg = attrClass.TypeArguments.FirstOrDefault()?.ToDisplayString();

                if (name is not null && genericArg is not null)
                    results.Add((name, genericArg));
            }

            return results;
        }

        public static List<(string Name, string Type, int Size, int Bit, int Hi, int Lo)> ParseFullTraits(IMethodSymbol method)
        {
            var results = new List<(string, string, int, int, int, int)>();

            foreach (var attrData in method.GetAttributes())
            {
                var attrClass = attrData.AttributeClass;
                if (!attrClass?.Name.StartsWith("ARMParameterAttribute") ?? true)
                    continue;

                string name = attrData.ConstructorArguments[0].Value?.ToString();
                string type = attrClass.TypeArguments.FirstOrDefault()?.ToDisplayString();

                int size = GetOptionalArg<int>(attrData, "size");
                int bit = GetOptionalArg<int>(attrData, "bit");
                int hi = GetOptionalArg<int>(attrData, "hi");
                int lo = GetOptionalArg<int>(attrData, "lo");

                if (name is not null && type is not null)
                    results.Add((name, type, size, bit, hi, lo));
            }

            return results;
        }

        private static T? GetOptionalArg<T>(AttributeData attr, string argName)
        {
            ImmutableArray<IParameterSymbol> ctorParams = (ImmutableArray<IParameterSymbol>)(attr.AttributeConstructor?.Parameters);
            if (ctorParams != null)
            {
                for (int i = 0; i < attr.ConstructorArguments.Length; i++)
                {
                    if (ctorParams[i].Name.ToLower() == argName.ToLower() && attr.ConstructorArguments[i].Value is T ctorArg)
                        return ctorArg;
                }
            }
            return default;
        }

    }
}
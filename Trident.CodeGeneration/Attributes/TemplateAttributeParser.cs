using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Trident.CodeGeneration.Attributes
{
    public static class TemplateAttributeParser
    {
        public static List<TemplateTrait> Parse(IMethodSymbol method)
        {
            var results = new List<(string, string, int, int, int, int)>();

            foreach (var attrData in method.GetAttributes())
            {
                var attrClass = attrData.AttributeClass;
                if (!attrClass?.Name.StartsWith("TemplateParameterAttribute") ?? true)
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

        private static T GetOptionalArg<T>(AttributeData attr, string argName)
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
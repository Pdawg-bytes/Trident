using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using Trident.CodeGeneration.Helpers;
using Trident.CodeGeneration.CodeGen.Metadata;

namespace Trident.CodeGeneration.Attributes;

internal static class TemplateAttributeParser
{
    internal static EquatableArray<TemplateTrait> Parse(IMethodSymbol method)
    {
        var results = new List<TemplateTrait>();

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
                results.Add(new TemplateTrait(name, type, size, bit, hi, lo));
        }

        return ImmutableArray.Create(results.ToArray());
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
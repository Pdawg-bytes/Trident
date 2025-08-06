using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Trident.CodeGeneration.Helpers;
using Trident.CodeGeneration.CodeGen.Metadata;

namespace Trident.CodeGeneration.CodeGen
{
    internal static class TemplateContractGenerator
    {
        internal static string Generate(string methodName, EquatableArray<TemplateTrait> traits)
        {
            IEnumerable<string> members = traits.Select(t => $"static abstract {t.Type} {t.Name} {{ get; }}");

            string source = $@"    internal interface I{methodName}_Traits
    {{
        {string.Join("\n        ", members)}
    }}";
            source += '\n';
            return source;
        }
    }
}
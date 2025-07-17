using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Trident.CodeGeneration.CodeGen
{
    internal static class TemplateContractGenerator
    {
        internal static string Generate(string methodName, List<TemplateTrait> traits)
        {
            string interfaceName = $"I{methodName}_Traits";
            IEnumerable<string> members = traits.Select(t => $"static abstract {t.Type} {t.Name} {{ get; }}");

            string source = $@"    internal interface {interfaceName}
    {{
        {string.Join("\n        ", members)}
    }}";
            source += '\n';
            return source;
        }
    }
}
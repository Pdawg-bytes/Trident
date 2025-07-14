using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Trident.CodeGeneration.CodeGen
{
    internal static class TraitStructGenerator
    {
        internal static string GenerateTraitStruct(string structName, string methodName, List<TemplateTrait> traits, Dictionary<string, object> values)
        {
            StringBuilder structBuilder = new();
            structBuilder.AppendLine($"    public struct {structName} : I{methodName}_Traits");
            structBuilder.AppendLine("    {");

            for (int i = 0; i < traits.Count; i++)
            {
                TemplateTrait trait = traits[i];
                object val = values[trait.Name];
                structBuilder.AppendLine($"        public static {trait.Type} {trait.Name} => {val.ToString().ToLowerInvariant()};");
            }

            structBuilder.AppendLine("    }");
            return structBuilder.ToString();
        }

        internal static string GetPermutationKey(string methodName, Dictionary<string, object> traitValues) =>
            $"{methodName}_{string.Join("_", traitValues.Select(kv => $"{kv.Key}{kv.Value.ToString()}"))}";
    }
}
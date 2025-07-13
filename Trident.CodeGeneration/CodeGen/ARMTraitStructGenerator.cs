using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Trident.CodeGeneration.CodeGen
{
    internal static class ARMTraitStructGenerator
    {
        internal static string GenerateTraitStruct(string structName, string methodName, List<(string Name, string Type)> traits, Dictionary<string, object> values)
        {
            StringBuilder structBuilder = new();
            structBuilder.AppendLine($"public struct {structName} : Trident.Core.CPU.Decoding.ARM.I{methodName}_Traits");
            structBuilder.AppendLine("{");

            for (int i = 0; i < traits.Count; i++)
            {
                var (paramName, typeName) = traits[i];
                object val = values[paramName];
                structBuilder.AppendLine($"    public static {typeName} {paramName} => {val.ToString().ToLowerInvariant()};");
            }

            structBuilder.AppendLine("}");
            return structBuilder.ToString();
        }

        internal static string GetPermutationKey(IMethodSymbol method, Dictionary<string, object> traitValues) =>
            $"{method.Name}_{string.Join("_", traitValues.Select(kv => $"{kv.Key}{kv.Value.ToString()}"))}";
    }
}
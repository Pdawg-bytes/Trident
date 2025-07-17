using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Trident.CodeGeneration.CodeGen
{
    internal static class TraitStructGenerator
    {
        internal static string GenerateTraitStruct(string structName, string methodName, List<TemplateTrait> traits, Dictionary<string, object> values)
        {
            StringBuilder structBuilder = new();
            structBuilder.AppendLine($"    internal struct {structName} : I{methodName}_Traits");
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
            $"{methodName}__{ComputeSafeHash(traitValues)}";

        static string ComputeSafeHash(Dictionary<string, object> traits)
        {
            using SHA256 sha = SHA256.Create();
            string input = string.Join("_", traits.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}_{kv.Value}"));
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 24);
        }
    }
}
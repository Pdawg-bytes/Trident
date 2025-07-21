using System.Linq;
using Microsoft.CodeAnalysis;
using Trident.CodeGeneration.Helpers;
using Trident.CodeGeneration.Attributes;

namespace Trident.CodeGeneration.CodeGen.Metadata
{
    internal sealed record class TemplateMethodInfo
    {
        public string Name { get; }
        public EquatableArray<TemplateTrait> Traits { get; }
        public int? Group { get; }
        public bool IsARM { get; }

        public TemplateMethodInfo(IMethodSymbol method)
        {
            Name = method.Name;
            Traits = TemplateAttributeParser.Parse(method);


            // Determine group from method
            AttributeData groupAttribute = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString().Contains(CodeGenUtils.GroupAttributeName.Split('`')[0]) == true);

            if (groupAttribute?.ConstructorArguments[0].Value is int value)
                Group = value;
            else
                Group = null;


            // Determine if we're working with a Thumb or an ARM instruction
            if (groupAttribute?.AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol groupType)
            {
                string typeName = groupType.ToDisplayString();
                IsARM = typeName.Contains("ARM");
            }
            else
                IsARM = true; // Assume by default
        }
    }
}
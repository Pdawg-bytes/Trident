using System.Linq;
using Microsoft.CodeAnalysis;
using Trident.CodeGeneration.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Trident.CodeGeneration.Decoding
{
    internal static class ARMDecoder
    {
        public enum ARMGroup
        {
            Multiply,
            MultiplyLong,
            BranchExchange,
            BranchWithLink,
            Swap,
            SmallSignedTransfer,
            DataProcessing,
            PSRTransfer,
            SingleDataTrasnfer,
            BlockDataTransfer,
            Undefined,
            SoftwareInterrupt,
            CoprocDataOperation,
            CoprocDataTransfer,
            CoprocRegisterTransfer
        }

        internal static ARMGroup DetermineARMGroup(uint instruction)
        {
            uint opcode = instruction & 0x0FFFFFFF;
            int subOp = (int)(instruction >> 21 & 0xF);

            return (opcode >> 26) switch
            {
                0b00 => instruction switch
                {
                    _ when instruction.IsBitSet(25) =>
                        !instruction.IsBitSet(20) && subOp >= 0b1000 && subOp <= 0b1011
                            ? ARMGroup.PSRTransfer : ARMGroup.DataProcessing,

                    _ when (instruction & 0xFF000F0) == 0x1200010 => ARMGroup.BranchExchange,

                    _ when (instruction & 0x10000F0) == 0x0000090 =>
                        instruction.IsBitSet(23) ? ARMGroup.MultiplyLong : ARMGroup.Multiply,

                    _ when (instruction & 0x10000F0) == 0x1000090 => ARMGroup.Swap,

                    _ when (instruction & 0xF0) == 0xB0 || (instruction & 0xD0) == 0xD0 =>
                        ARMGroup.SmallSignedTransfer,

                    _ =>
                        !instruction.IsBitSet(20) && subOp >= 0b1000 && subOp <= 0b1011
                            ? ARMGroup.PSRTransfer : ARMGroup.DataProcessing,
                },

                0b01 => (instruction & 0x2000010) == 0x2000010
                    ? ARMGroup.Undefined : ARMGroup.SingleDataTrasnfer,

                0b10 => instruction.IsBitSet(25)
                    ? ARMGroup.BranchWithLink : ARMGroup.BlockDataTransfer,

                0b11 => instruction switch
                {
                    _ when instruction.IsBitSet(25) && instruction.IsBitSet(24) =>
                        ARMGroup.SoftwareInterrupt,

                    _ when instruction.IsBitSet(25) =>
                        instruction.IsBitSet(4)
                            ? ARMGroup.CoprocRegisterTransfer : ARMGroup.CoprocDataOperation,

                    _ => ARMGroup.CoprocDataTransfer,
                },

                _ => ARMGroup.Undefined
            };
        }


        internal static IMethodSymbol FindGroupMethod(Compilation compilation, ARMGroup group)
        {
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                var methodDecls = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (MethodDeclarationSyntax methodDecl in methodDecls)
                {
                    IMethodSymbol symbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                    AttributeData groupAttr = symbol?.GetAttributes().FirstOrDefault(attr =>
                        attr.AttributeClass?.Name == "ARMGroupAttribute");

                    if (groupAttr is not null &&
                        (int)(groupAttr.ConstructorArguments[0].Value ?? -1) == (int)group)
                    {
                        return symbol;
                    }
                }
            }

            return null!;
        }
    }
}

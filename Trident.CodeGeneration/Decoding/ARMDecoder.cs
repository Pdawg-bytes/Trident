using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Trident.CodeGeneration.Shared;
using Trident.CodeGeneration.CodeGen;

namespace Trident.CodeGeneration.Decoding
{
    internal static class ARMDecoder
    {
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


        internal static IMethodSymbol? FindGroupMethod(IEnumerable<IMethodSymbol> methods, ARMGroup group)
        {
            return methods.FirstOrDefault(method =>
            {
                var attr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == CodeGenUtils.GenericParameterAttributeName);

                if (attr is null)
                    return false;

                var arg = attr.ConstructorArguments.FirstOrDefault();
                return arg.Value is int value && value == (int)group;
            });
        }

        internal static Dictionary<ARMGroup, List<uint>> BuildGroupOpcodeMap()
        {
            var map = new Dictionary<ARMGroup, List<uint>>();
            for (uint opcode = 0; opcode < 4096; opcode++)
            {
                uint expanded = (opcode & 0xFF0) << 16 | (opcode & 0x00F) << 4;
                ARMGroup group = DetermineARMGroup(expanded);
                if (!map.TryGetValue(group, out var list))
                    map[group] = list = new List<uint>();
                list.Add(expanded);
            }
            return map;
        }
    }
}
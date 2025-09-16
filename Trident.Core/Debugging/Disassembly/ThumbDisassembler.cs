using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.CodeGeneration.Shared;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

using InstructionData = (string Mnemonic, System.Collections.Generic.List<string> Operands);

namespace Trident.Core.Debugging.Disassembly
{
    internal class ThumbDisassembler
    {
        internal static DisassembledInstruction Disassemble(uint address, ushort opcode)
        {
            var instr = new DisassembledInstruction
            {
                Address = address,
                Opcode = opcode,
                MnemonicBase = "??",
                ConditionCode = "",
                Operands = ["??"]
            };

            InstructionData data = ThumbDecoder.DetermineThumbGroup(opcode) switch
            {
                _ => new InstructionData { Mnemonic = "??", Operands = ["??"] }
            };

            instr.MnemonicBase = data.Mnemonic;
            instr.Operands = data.Operands;
            return instr;
        }
    }
}

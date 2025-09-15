using Trident.Core.Global;
using Trident.Core.Memory.Region;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Debugging.Disassembly
{
    public sealed class Disassembler
    {
        private readonly Func<uint, IDebugMemory?> _getRegion;
        private readonly Func<uint> _getPC;
        private readonly Func<CPUSnapshot> _getSnapshot;

        public Disassembler(Func<uint, IDebugMemory?> getRegion, Func<uint> getPC, Func<CPUSnapshot> getSnapshot)
        {
            _getRegion = getRegion;
            _getPC = getPC;
            _getSnapshot = getSnapshot;
        }

        public (uint, List<DisassembledInstruction>) GetAroundPC(uint before, uint after)
        {
            uint pc = _getPC();
            bool thumb = _getSnapshot().CPSR.IsBitSet(5);

            IDebugMemory? region = _getRegion(pc >> 24);
            if (region is null)
                return (0, []);

            uint instrSize = thumb ? 2 : 4u;
            var (start, end) = GetDisasmWindow(pc, before, after, instrSize, region);

            var instructions = new List<DisassembledInstruction>();

            for (uint addr = start; addr < end; addr += thumb ? 2u : 4u)
            {
                var opcode = region.DebugRead<uint>(addr);

                if (thumb)
                    instructions.Add(new(addr, opcode, "??", "??", ["??"]));
                else
                    instructions.Add(ARMDisassembler.Disassemble(addr, opcode));
            }

            return (pc - (thumb ? 4 : 8u), instructions);
        }

        private static (uint start, uint end) GetDisasmWindow(uint pc, uint before, uint after, uint instrSize, IDebugMemory region)
        {
            before *= instrSize;
            after *= instrSize;

            uint min = region.BaseAddress;
            uint max = region.EndAddress;

            uint start = pc > before ? pc - before : min;
            if (start < min) start = min;

            uint end = pc + after;
            if (end > max) end = max;

            return (start, end);
        }
    }

    public struct DisassembledInstruction(uint address, uint opcode, string mnemonicBase, string conditionCode, List<string> operands)
    {
        public uint Address = address;
        public uint Opcode = opcode;
        public string MnemonicBase = mnemonicBase;
        public string ConditionCode = conditionCode;
        public List<string> Operands = operands;
    }
}
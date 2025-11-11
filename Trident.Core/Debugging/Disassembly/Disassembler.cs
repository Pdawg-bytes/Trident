using Trident.Core.Global;
using Trident.Core.Memory.Region;
using Trident.CodeGeneration.Shared;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Debugging.Disassembly
{
    public sealed class Disassembler(Func<uint, IDebugMemory?> getRegion, Func<uint> getPC, Func<CPUSnapshot> getSnapshot)
    {
        internal bool Enabled { get; set; }

        private byte[][] _tokenBuffer = [];
        private DisassembledInstruction[] _disasmBuffer = [];
        private int _disasmCount;


        public (uint ActualPC, bool Thumb, ReadOnlyMemory<DisassembledInstruction> Instructions) GetAroundPC(uint before, uint after)
        {
            if (!Enabled)
                return (0, false, ReadOnlyMemory<DisassembledInstruction>.Empty);

            uint lr = 0;

            uint pc    = getPC();
            bool thumb = getSnapshot().CPSR.IsBitSet(5);

            IDebugMemory? region = getRegion(pc >> 24);
            if (region is null)
                return (0, thumb, ReadOnlyMemory<DisassembledInstruction>.Empty);

            uint instrSize   = thumb ? 2 : 4u;
            var (start, end) = GetDisasmWindow(pc, before, after, instrSize, region);

            int length = (int)((end - start) / instrSize);
            if (length > 512 || length < 0)
                throw new Exception("Disassembly window out of range.");

            if (_disasmBuffer.Length != length)
            {
                _disasmBuffer = new DisassembledInstruction[length];
                _tokenBuffer  = new byte[length][];

                for (int i = 0; i < length; i++)
                    _tokenBuffer[i] = new byte[96];
            }

            _disasmCount = length;

            for (int i = 0; i < length; i++)
            {
                uint addr = start + (uint)(i * instrSize);

                if (thumb)
                {
                    ushort opcode    = region.DebugRead<ushort>(addr);
                    ThumbGroup group = ThumbDecoder.DetermineThumbGroup(opcode);

                    DisassembledInstruction instr = ThumbDisassembler.Disassemble(addr, lr, opcode, group, _tokenBuffer[i]);
                    _disasmBuffer[i] = instr;

                    if (group == ThumbGroup.LongBranchWithLink)
                    {
                        uint offset = (uint)((uint)opcode & 0x07FF).ExtendFrom(11) << 12;
                        lr = addr + 4 + offset;
                    }
                }
                else
                {
                    uint opcode = region.DebugRead<uint>(addr);

                    DisassembledInstruction instr = ARMDisassembler.Disassemble(addr, opcode, _tokenBuffer[i]);
                    _disasmBuffer[i] = instr;
                }
            }

            return (pc - (thumb ? 4 : 8u), thumb, _disasmBuffer.AsMemory(0, _disasmCount));
        }

        private static (uint start, uint end) GetDisasmWindow(uint pc, uint before, uint after, uint instrSize, IDebugMemory region)
        {
            before *= instrSize;
            after  *= instrSize;

            uint min = region.BaseAddress;
            uint max = region.EndAddress;

            uint start = pc > before ? pc - before : min;
            if (start < min) start = min;

            uint end = pc + after;
            if (end > max) end = max;

            return (start, end);
        }
    }


    public struct DisassembledInstruction()
    {
        public uint Address { get; set; }
        public readonly uint Opcode { get; init; }

        public readonly ReadOnlyMemory<byte> Tokens { get; init; }

        public readonly int OperandsStartIndex { get; init; }
    }
}
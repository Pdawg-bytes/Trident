using Trident.Core.CPU;
using Trident.Core.Global;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.Debugging.Disassembly
{
    internal static class DisassemblerUtilities
    {
        internal readonly static string[] _registers = ["r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc"];


        internal static string ConditionCodeString(uint condition) => condition switch
        {
            CondEQ => "eq",
            CondNE => "ne",
            CondCS => "cs",
            CondCC => "cc",
            CondMI => "mi",
            CondPL => "pl",
            CondVS => "vs",
            CondVC => "vc",
            CondHI => "hi",
            CondLS => "ls",
            CondGE => "ge",
            CondLT => "lt",
            CondGT => "gt",
            CondLE => "le",
            CondAL => "",
            _ => "??"
        };


        internal static string RegisterList(uint rlist)
        {
            List<string> parts = [];
            int? rangeStart = null;
            int rangeLength = 0;

            for (int i = 0; i <= 16; i++)
            {
                bool set = i < 16 && (rlist & (1u << i)) != 0;

                if (set)
                {
                    if (!rangeStart.HasValue)
                    {
                        rangeStart = i;
                        rangeLength = 1;
                    }
                    else
                        rangeLength++;
                }
                else if (rangeStart.HasValue)
                {
                    int start = rangeStart.Value;
                    int end = i - 1;

                    if (rangeLength >= 3)
                        parts.Add($"{_registers[start]}-{_registers[end]}");
                    else
                        for (int j = start; j <= end; j++)
                            parts.Add(_registers[j]);

                    rangeStart = null;
                    rangeLength = 0;
                }
            }

            return "{ " + string.Join(", ", parts) + " }";
        }


        internal static string ShiftedRegister(uint shiftData)
        {
            uint rm = shiftData & 0x0F;
            bool regShift = shiftData.IsBitSet(4);
            ShiftType type = (ShiftType)((shiftData >> 5) & 0b11);

            string mnemonic = new[] { "lsl", "lsr", "asr", "ror" }[(int)type];

            if (regShift)
            {
                string rs = _registers[(shiftData >> 8) & 0x0F];
                return $"{_registers[rm]}, {mnemonic} {rs}";
            }

            uint shamt = (shiftData >> 7) & 0x1F;

            if (shamt == 0 && (type == ShiftType.LSR || type == ShiftType.ASR))
                shamt = 32;

            if (shamt == 0)
            {
                return type == ShiftType.ROR
                    ? $"{_registers[rm]}, rrx"
                    : _registers[rm];
            }

            return $"{_registers[rm]}, {mnemonic} #{shamt}";
        }

        internal static uint RotatedImmediate(uint immData) => (immData & 0xFF).RotateRight((((int)immData >> 8) & 0x0F) << 1);


        internal static string BuildFSXC(uint maskBits)
        {
            if (maskBits == 0) return "";

            const string flags = "fsxc";
            Span<char> buffer = stackalloc char[5];
            buffer[0] = '_';
            int index = 1;

            for (int i = 0; i < 4; i++)
            {
                if ((maskBits & (1 << i)) != 0)
                    buffer[index++] = flags[i];
            }

            return new string(buffer[..index]);
        }
    }
}

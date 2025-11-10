using Trident.Core.CPU;
using Trident.Core.Debugging.Disassembly.Tokens;
using Trident.Core.Global;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.Debugging.Disassembly
{
    internal static class DisassemblerUtilities
    {
        internal readonly static string[] _registers = ["r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc"];


        internal static ReadOnlySpan<char> ConditionCodeString(uint condition) => condition switch
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
            CondAL => ReadOnlySpan<char>.Empty,
            _ => "??"
        };


        internal static void AppendRegisterList(ref TokenWriter writer, uint rlist, bool userMode)
        {
            writer.Syntax('{');

            uint? rangeStart = null;
            uint rangeLength = 0;
            bool first = true;

            for (uint i = 0; i <= 16; i++)
            {
                bool set = i < 16 && rlist.IsBitSet((int)i);

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
                    uint start = rangeStart.Value;
                    uint end = i - 1;

                    if (!first)
                        writer.SyntaxSpace(',');
                    
                    if (rangeLength >= 3)
                    {
                        writer.AppendFormatted(new Register(start));
                        writer.Syntax('-');
                        writer.AppendFormatted(new Register(end));
                    }
                    else
                    {
                        for (uint j = start; j <= end; j++)
                        {
                            if (j > start)
                                writer.SyntaxSpace(',');

                            writer.AppendFormatted(new Register(j));
                        }
                    }

                    first = false;
                    rangeStart = null;
                    rangeLength = 0;
                }
            }

            writer.Syntax('}');
            if (userMode)
                writer.Syntax('^');
        }


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

            return '{' + string.Join(", ", parts) + '}';
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
    }
}
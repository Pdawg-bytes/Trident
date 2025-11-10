using Trident.Core.Global;
using Trident.Core.Debugging.Disassembly.Tokens;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.Debugging.Disassembly
{
    internal static class DisassemblerUtilities
    {
        internal static WriteResult EmitUnknownInstruction(Span<byte> buffer)
        {
            TokenWriter writer = new(buffer);
            writer.AppendFormatted(new Mnemonic("???"));
            writer.BeginOperands();
            writer.Unknown("???");
            return writer.Finalize();
        }


        internal static uint RotatedImmediate(uint immData) => (immData & 0xFF).RotateRight((((int)immData >> 8) & 0x0F) << 1);


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

        internal static void InsertConditionMnemonic(uint condition, Span<byte> tokenBuffer, ref WriteResult result)
        {
            ReadOnlySpan<char> condText = ConditionCodeString(condition);
            if (condText.IsEmpty)
                return;

            int condLen = 2 + condText.Length;

            if (result.BytesWritten + condLen > tokenBuffer.Length)
                throw new InvalidOperationException($"Not enough space in token buffer to insert condition token (need {condLen} bytes).");

            for (int i = result.BytesWritten - 1; i >= result.OperandsStartIndex; i--)
                tokenBuffer[i + condLen] = tokenBuffer[i];

            var slice  = tokenBuffer.Slice(result.OperandsStartIndex, condLen);
            var writer = new TokenWriter(slice);
            writer.AppendFormatted(new Mnemonic(condText, isCondition: true));

            result.BytesWritten += condLen;
            result.OperandsStartIndex += condLen;
        }


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
    }
}
using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Registers;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Add(uint op1, uint op2, bool modifyFlags)
        {
            uint result = op1 + op2;

            if (modifyFlags)
            {
                SetNZ(result);
                Registers.ModifyFlag(Flags.C, result < op1);
                // If sign of op1 == op2, but result has a different sign, then an overflow occurred.
                Registers.ModifyFlag(Flags.V, ((~(op1 ^ op2) & (op2 ^ result)) >> 31) != 0);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Subtract(uint op1, uint op2, bool modifyFlags)
        {
            uint result = op1 - op2;

            if (modifyFlags)
            {
                SetNZ(result);
                Registers.ModifyFlag(Flags.C, op1 >= op2);
                // If sign of op1 != op2 && sign of op1 != result, then an overflow occurred.
                Registers.ModifyFlag(Flags.V, (((op1 ^ op2) & (op1 ^ result)) >> 31) != 0);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint AddCarry(uint op1, uint op2, bool modifyFlags)
        {
            if (modifyFlags)
            {
                ulong result64 = (ulong)op1 + (ulong)op2 + (ulong)((uint)Registers.CPSR).GetBit(29);
                uint result    = (uint)result64;

                SetNZ(result);
                Registers.ModifyFlag(Flags.C, (result64 >> 32) != 0);
                // If sign of op1 == op2, but result has a different sign, then an overflow occurred.
                Registers.ModifyFlag(Flags.V, ((~(op1 ^ op2) & (op2 ^ result)) >> 31) != 0);
                return result;
            }
            else
                return op1 + op2 + ((uint)Registers.CPSR).GetBit(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint SubtractCarry(uint op1, uint op2, bool modifyFlags)
        {
            uint carry  = ((uint)Registers.CPSR).GetBit(29) ^ 1;
            uint result = op1 - op2 - carry;

            if (modifyFlags)
            {
                SetNZ(result);
                Registers.ModifyFlag(Flags.C, (ulong)op1 >= ((ulong)op2 + (ulong)carry));
                // If sign of op1 != op2 && sign of op1 != result, then an overflow occurred.
                Registers.ModifyFlag(Flags.V, (((op1 ^ op2) & (op1 ^ result)) >> 31) != 0);
            }

            return result;
        }
    }
}
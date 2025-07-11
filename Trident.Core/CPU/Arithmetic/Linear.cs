using Trident.Core.Bus;
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
                SetZeroSign(result);
                Registers.ModifyFlag(Flags.C, result < op1);
                // If sign of op1 == op2, but result has a different sign, then an overflow occured.
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
                SetZeroSign(result);
                Registers.ModifyFlag(Flags.C, op1 >= op2);
                // If sign of op1 != op2 && sign of op1 != result, then an overflow occured.
                Registers.ModifyFlag(Flags.V, (((op1 ^ op2) & (op1 ^ result)) >> 31) != 0);
            }

            return result;
        }
    }
}
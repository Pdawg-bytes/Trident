using Trident.Core.Bus;
using Trident.Core.CPU.Registers;
using System.Runtime.CompilerServices;
using Trident.Core.Global;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        private enum ShiftType : uint
        {
            /// <summary>Logical shift left</summary>
            LSL = 0b00,
            /// <summary>Logical shift right</summary>
            LSR = 0b01,
            /// <summary>Arithmetic shift right</summary>
            ASR = 0b10,
            /// <summary>Rotate right</summary>
            ROR = 0b11
        }

        private uint PerformShift(ShiftType operation, bool immediateShift, uint value, uint shiftAmount, ref bool carryOut)
        {
            return operation switch
            {
                ShiftType.LSL => LogicalShiftLeft(value, shiftAmount, ref carryOut),
                ShiftType.LSR => LogicalShiftRight(value, shiftAmount == 0 ? 32 : shiftAmount, ref carryOut, immediateShift),
                ShiftType.ASR => ArithmeticShiftRight(value, shiftAmount == 0 ? 32 : shiftAmount, ref carryOut, immediateShift),
                ShiftType.ROR => RotateRight(value, shiftAmount, ref carryOut, immediateShift),
                _ => throw new InvalidInstructionException<TBus>($"PerformShift: invalid shift operation: {operation}.", this)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LogicalShiftLeft(uint value, uint shiftAmount, ref bool carry)
        {
            shiftAmount = (uint)Math.Min((int)shiftAmount, 33);
            uint result = (uint)((ulong)value << (int)shiftAmount);

            if (shiftAmount != 0)
                carry = ((uint)((ulong)value << (int)(shiftAmount - 1)) >> 31) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LogicalShiftRight(uint value, uint shiftAmount, ref bool carry, bool immediateShift)
        {
            if (immediateShift && shiftAmount == 0)
                shiftAmount = 32;

            shiftAmount = (uint)Math.Min((int)shiftAmount, 33);
            uint result = (uint)((ulong)value >> (int)shiftAmount);

            if (shiftAmount != 0)
                carry = (((ulong)value >> (int)(shiftAmount - 1)) & 1u) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ArithmeticShiftRight(uint value, uint shiftAmount, ref bool carry, bool immediateShift)
        {
            if (immediateShift && shiftAmount == 0)
                shiftAmount = 32;

            shiftAmount = (uint)Math.Min((int)shiftAmount, 33);
            uint result = (uint)((long)(int)value >> (int)shiftAmount);

            if (shiftAmount != 0)
                carry = (((long)(int)value >> (int)(shiftAmount - 1)) & 1u) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint RotateRight(uint value, uint shiftAmount, ref bool carry, bool immediateShift)
        {
            uint result = value;

            if (immediateShift && shiftAmount == 0)
            {
                uint oldLsb = value & 1;
                result = value.RotateRight((int)shiftAmount);
                carry = oldLsb != 0;
            }
            else
            {
                if (shiftAmount == 0)
                    return value;

                shiftAmount &= 31;
                result = value.RotateRight((int)shiftAmount);
                carry = (result >> 31) != 0;
            }

            return result;
        }
    }
}
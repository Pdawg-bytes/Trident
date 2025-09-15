using Trident.Core.Bus;
using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    internal enum ShiftType : uint
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

    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        private uint PerformShift(ShiftType operation, bool immediateShift, uint value, byte shiftAmount, ref bool carryOut)
        {
            return operation switch
            {
                ShiftType.LSL => LogicalShiftLeft(value, shiftAmount, ref carryOut),
                ShiftType.LSR => LogicalShiftRight(value, shiftAmount, ref carryOut, immediateShift),
                ShiftType.ASR => ArithmeticShiftRight(value, shiftAmount, ref carryOut, immediateShift),
                ShiftType.ROR => RotateRight(value, shiftAmount, ref carryOut, immediateShift),
                _ => throw new InvalidInstructionException<TBus>($"PerformShift: invalid shift operation: {operation}.", this)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LogicalShiftLeft(uint value, byte shiftAmount, ref bool carry)
        {
            shiftAmount = Math.Min(shiftAmount, (byte)33);
            uint result = (uint)((ulong)value << shiftAmount);

            if (shiftAmount != 0)
                carry = ((uint)((ulong)value << (shiftAmount - 1)) >> 31) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LogicalShiftRight(uint value, byte shiftAmount, ref bool carry, bool immediateShift)
        {
            if (immediateShift && shiftAmount == 0)
                shiftAmount = 32;

            int shamt = Math.Min(shiftAmount, (byte)33);
            uint result = (uint)((ulong)value >> shamt);

            if (shiftAmount != 0)
                carry = (((ulong)value >> (shamt - 1)) & 1) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ArithmeticShiftRight(uint value, byte shiftAmount, ref bool carry, bool immediateShift)
        {
            if (immediateShift && shiftAmount == 0)
                shiftAmount = 32;

            shiftAmount = Math.Min(shiftAmount, (byte)33);
            uint result = (uint)((long)(int)value >> shiftAmount);

            if (shiftAmount != 0)
                carry = (((long)(int)value >> (shiftAmount - 1)) & 1) != 0;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint RotateRight(uint value, byte shiftAmount, ref bool carry, bool immediateShift)
        {
            uint result = value;

            if (immediateShift && shiftAmount == 0)
            {
                uint oldLsb = value & 1;
                result = (value >> 1) | ((carry ? (uint)1 : 0) << 31);
                carry = oldLsb != 0;
            }
            else
            {
                if (shiftAmount == 0)
                    return value;

                shiftAmount &= 31;
                result = value.RotateRight(shiftAmount);
                carry = (result >> 31) != 0;
            }

            return result;
        }
    }
}
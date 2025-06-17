using System.Diagnostics;
using Trident.Core.Enums;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    /// <summary>
    /// Defines the ARM7TDMI register set, with support for register banking based on the current mode.
    /// </summary>
    public struct RegisterSet
    {
        [InlineArray(16)]
        private struct RegisterArray
        {
            private uint _r0;
            internal static int Length => 16;
        }

        // In reality, we have 6 distinct modes, but we are once again indexing based on the mode,
        // meaning we have 2^5 possible indices. Masking out the MSB could bring us down to 2^4,
        // but it's really not worth it to add more logic to the mode switch.
        private readonly BankParameters[] _bankParams = new BankParameters[32];

        private uint[] _bankStore = new uint[22];   // USR/SYS (7 regs, default set), FIQ (7 regs), other 4 modes (2 regs ea.)
        private Flags[] _bankedSpsr = new Flags[6]; // 6 distinct modes, usr/sys don't use SPSR, but our bank switch relies on it anyways.
        private RegisterArray _registers = new RegisterArray();

        public PrivilegeMode CurrentMode { get; private set; }

        public RegisterSet()
        {
            _bankParams[(uint)PrivilegeMode.User] = new BankParameters(8, 0, 7, 0);
            _bankParams[(uint)PrivilegeMode.System] = new BankParameters(8, 0, 7, 0);
            _bankParams[(uint)PrivilegeMode.FIQ] = new BankParameters(8, 7, 7, 1);
            _bankParams[(uint)PrivilegeMode.IRQ] = new BankParameters(13, 14, 2, 2);
            _bankParams[(uint)PrivilegeMode.Supervisor] = new BankParameters(13, 16, 2, 3);
            _bankParams[(uint)PrivilegeMode.Abort] = new BankParameters(13, 18, 2, 4);
            _bankParams[(uint)PrivilegeMode.Undefined] = new BankParameters(13, 20, 2, 5);

            CurrentMode = PrivilegeMode.System;
            SwitchMode(PrivilegeMode.System);
        }

        internal unsafe ref uint GetRegisterRef(int index)
        {
            Debug.Assert(index >= 0 && index < 16);
            ref uint first = ref Unsafe.AsRef<uint>(Unsafe.AsPointer(ref _registers));
            return ref Unsafe.Add(ref first, index);
        }

        public uint this[int index]
        {
            get => GetRegisterRef(index);
            set => GetRegisterRef(index) = value;
        }

        public uint this[uint index]
        {
            get => GetRegisterRef((int)index);
            set => GetRegisterRef((int)index) = value;
        }

        internal uint SP
        {
            get => this[13];
            set => this[13] = value;
        }

        internal uint LR
        {
            get => this[14];
            set => this[14] = value;
        }

        public uint PC
        {
            get => this[15];
            set => this[15] = value;
        }


        /// <summary>
        /// Switches the register bank that the processor is using based on the requested <see cref="PrivilegeMode"/>.
        /// </summary>
        /// <param name="newMode">The mode to set the processor to.</param>
        public void SwitchMode(PrivilegeMode newMode)
        {
            // Don't copy anything unless we have to
            if (CurrentMode == newMode) return;

            // Copy state into respective bank
            BankParameters currentCopy = _bankParams[(uint)CurrentMode];
            for (int i = 0; i < currentCopy.RegisterCount; i++)
                _bankStore[currentCopy.BankIndex + i] = _registers[currentCopy.ActiveSetIndex + i];

            _bankedSpsr[currentCopy.SPSRIndex] = SPSR;

            // Copy in r8-r12 from the user bank if we're leaving FIQ and entering anything except for USR/SYS.
            // We don't need to copy r13 or r14 because every other mode overwrites them anyways.
            if (CurrentMode == PrivilegeMode.FIQ && (newMode != PrivilegeMode.User && newMode != PrivilegeMode.System))
                for (int i = 0; i < 5; i++) _registers[8 + i] = _bankStore[i];

            // Copy new bank into working set
            BankParameters newCopy = _bankParams[(uint)newMode];
            for (int i = 0; i < newCopy.RegisterCount; i++)
                _registers[newCopy.ActiveSetIndex + i] = _bankStore[newCopy.BankIndex + i];

            SPSR = _bankedSpsr[newCopy.SPSRIndex];

            CPSR = (CPSR & ~(Flags)0x1F) | (Flags)(uint)newMode;
            CurrentMode = newMode;
        }

        public void SetBankForMode(List<uint> values, PrivilegeMode mode)
        {
            BankParameters bank = _bankParams[(uint)mode];

            if (values.Count != bank.RegisterCount)
                throw new ArgumentException($"Expected {bank.RegisterCount} registers for mode {mode}, got {values.Count}");

            for (int i = 0; i < bank.RegisterCount; i++)
                _bankStore[bank.BankIndex + i] = values[i];
        }

        public void SetSpsrForMode(PrivilegeMode mode, Flags value) => _bankedSpsr[_bankParams[(uint)mode].SPSRIndex] = value;

        public void GetBankForMode(PrivilegeMode mode, Span<uint> destination)
        {
            BankParameters bank = _bankParams[(uint)mode];

            if (destination.Length < bank.RegisterCount)
                throw new ArgumentException("Destination span too small");

            for (int i = 0; i < bank.RegisterCount; i++)
                destination[i] = _bankStore[bank.BankIndex + i];
        }

        internal void ResetRegisters()
        {
            for (int i = 0; i < 16; i++) _registers[i] = 0;
            Array.Clear(_bankStore, 0, _bankStore.Length);
            Array.Clear(_bankedSpsr, 0, _bankedSpsr.Length);
        }

        public void PrintRegisters()
        {
            Console.WriteLine($"Mode: {CurrentMode}");
            for (int i = 0; i < RegisterArray.Length; i++)
            {
                Console.WriteLine($"R{i}: 0x{_registers[i]:X8}");
            }
            Console.WriteLine($"CPSR: 0x{CPSR:X8}");
            if (CurrentMode is not PrivilegeMode.User && CurrentMode is not PrivilegeMode.System) Console.WriteLine($"SPSR: 0x{SPSR:X8}");
            Console.WriteLine(new string('-', 30));
        }


        /// <summary>The saved program status register.</summary>
        public Flags SPSR;

        /// <summary>The current program status register.</summary>
        public Flags CPSR;

        #region Program status utilities
        /// <summary>
        /// Sets the specified <paramref name="flag"/> in the <see cref="CPSR"/>.
        /// </summary>
        /// <param name="flag">The flag to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(Flags flag) => CPSR |= flag;

        /// <summary>
        /// Clears the <paramref name="flag"/> in the <see cref="CPSR"/>.
        /// </summary>
        /// <param name="flag">The flag to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearFlag(Flags flag) => CPSR &= ~flag;

        /// <summary>
        /// Conditionally sets the specified <paramref name="flag"/> in the <see cref="CPSR"/> based on <paramref name="condition"/>.
        /// </summary>
        /// <param name="flag">The flag to modify.</param>
        /// <param name="condition">True to set the flag; false otherwise.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ModifyFlag(Flags flag, bool condition) { if (condition) CPSR |= flag; else CPSR &= ~flag; }

        /// <summary>
        /// Determines whether a specified flag is set in the <see cref="CPSR"/>
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns>True if the flag is set; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsFlagSet(Flags flag) => (CPSR & flag) != 0;
        #endregion
    }


    /// <summary>
    /// Represents the parameters for a specific register bank used in different processor modes.
    /// This struct contains the information needed to switch between modes.
    /// </summary>
    readonly struct BankParameters
    {
        /// <summary>
        /// The index in the active register set where the registers for this mode start.
        /// </summary>
        internal readonly int ActiveSetIndex;

        /// <summary>
        /// The index in the bank storage where the registers for this mode are stored.
        /// </summary>
        internal readonly int BankIndex;

        /// <summary>
        /// The number of registers banked in this mode.
        /// </summary>
        internal readonly int RegisterCount;

        /// <summary>
        /// The index in the SPSR bank where the SPSR for this mode is stored.
        /// </summary>
        internal readonly int SPSRIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="BankParameters"/> struct with the specified values.
        /// </summary>
        /// <param name="activeSetIndex">The index in the active register set where the registers for this mode start.</param>
        /// <param name="bankIndex">The index in the banked storage array where the registers for this mode are stored.</param>
        /// <param name="registerCount">The number of registers used for this mode.</param>
        /// <param name="spsrIndex">The index in the SPSR bank array where the SPSR for this mode is stored.</param>
        internal BankParameters(int activeSetIndex, int bankIndex, int registerCount, int spsrIndex)
        {
            ActiveSetIndex = activeSetIndex;
            BankIndex = bankIndex;
            RegisterCount = registerCount;
            SPSRIndex = spsrIndex;
        }
    }
}
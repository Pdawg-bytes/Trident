using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU.Registers
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
        private static readonly BankParameters UsrSysPartialBank = new(13, 5, 2, 0);

        private uint[] _bankStore = new uint[22];   // USR/SYS (7 regs, default set), FIQ (7 regs), other 4 modes (2 regs ea.)
        private Flags[] _bankedSpsr = new Flags[6]; // 6 distinct modes, usr/sys don't use SPSR, but our bank switch relies on it anyways.
        private RegisterArray _registers = new();

        public PrivilegeMode CurrentMode { get; private set; }

        /// <summary>The current program status register.</summary>
        public Flags CPSR;


        public RegisterSet()
        {
            _bankParams[(uint)PrivilegeMode.USR] =       new(8, 0, 7, 0);
            _bankParams[(uint)PrivilegeMode.SYS] =     new(8, 0, 7, 0);
            _bankParams[(uint)PrivilegeMode.FIQ] =        new(8, 7, 7, 1);
            _bankParams[(uint)PrivilegeMode.IRQ] =        new(13, 14, 2, 2);
            _bankParams[(uint)PrivilegeMode.SVC] = new(13, 16, 2, 3);
            _bankParams[(uint)PrivilegeMode.ABT] =      new(13, 18, 2, 4);
            _bankParams[(uint)PrivilegeMode.UND] =  new(13, 20, 2, 5);

            ResetRegisters();
            CurrentMode = PrivilegeMode.SYS;
            SwitchMode(PrivilegeMode.SYS);
        }

        internal unsafe ref uint GetRegisterRef(int index)
        {
            Debug.Assert(index >= 0 && index < 16);
            ref uint first = ref Unsafe.AsRef<uint>(Unsafe.AsPointer(ref _registers));
            return ref Unsafe.Add(ref first, index);
        }

        public uint this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetRegisterRef(index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => GetRegisterRef(index) = value;
        }

        public uint this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetRegisterRef((int)index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (newMode == CurrentMode) return;

            bool fromFIQ    = CurrentMode is PrivilegeMode.FIQ;
            bool toFIQ      = newMode is PrivilegeMode.FIQ;
            bool fromUsrSys = IsUserOrSystem(CurrentMode);
            bool toUsrSys   = IsUserOrSystem(newMode);

            BankParameters oldBank = _bankParams[(uint)CurrentMode];
            BankParameters newBank = (toUsrSys && !fromFIQ)
                ? UsrSysPartialBank
                : _bankParams[(uint)newMode];


            // Save current banked registers
            int maxRegisters = Math.Max(toFIQ && !fromUsrSys ? 5 : 0, oldBank.RegisterCount);
            for (int i = 0; i < maxRegisters; i++)
            {
                // If entering FIQ from a mode that's not USR/SYS, we need to save the current USR bank from r8-r12.
                if (toFIQ && !fromUsrSys && i < 5)
                    _bankStore[i] = _registers[8 + i];

                if (i < oldBank.RegisterCount)
                    _bankStore[oldBank.BankIndex + i] = _registers[oldBank.ActiveSetIndex + i];
            }

            // Copy in new banked registers
            maxRegisters = Math.Max(5, newBank.RegisterCount);
            for (int i = 0; i < maxRegisters; i++)
            {
                // Perform the inverse of what we did in the save phase. When entering a mode that is not USR/SYS from FIQ,
                // we need to copy in the USR r8-r12.
                if (fromFIQ && !toUsrSys && i < 5)
                    _registers[8 + i] = _bankStore[i];

                if (i < newBank.RegisterCount)
                    _registers[newBank.ActiveSetIndex + i] = _bankStore[newBank.BankIndex + i];
            }


            CPSR = (CPSR & ~(Flags)0x1F) | (Flags)(uint)newMode;
            CurrentMode = newMode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUserOrSystem(PrivilegeMode mode) => mode is PrivilegeMode.USR || mode is PrivilegeMode.SYS;



        public void SetBankForMode(PrivilegeMode mode, Span<uint> values)
        {
            BankParameters bank = _bankParams[(uint)mode];

            if (values.Length != bank.RegisterCount)
                throw new ArgumentException($"Expected {bank.RegisterCount} registers for mode {mode}, got {values.Length}");

            for (int i = 0; i < bank.RegisterCount; i++)
                _bankStore[bank.BankIndex + i] = values[i];
        }

        public void GetBankForMode(PrivilegeMode mode, Span<uint> destination)
        {
            BankParameters bank = _bankParams[(uint)mode];

            if (destination.Length != bank.RegisterCount)
                throw new ArgumentException("Destination span size does not match register count.");

            for (int i = 0; i < bank.RegisterCount; i++)
                destination[i] = _bankStore[bank.BankIndex + i];
        }

        public uint GetSPSRForMode(PrivilegeMode mode) => (uint)_bankedSpsr[_bankParams[(uint)mode].SPSRIndex];
        public void SetSPSRForMode(PrivilegeMode mode, Flags value) => _bankedSpsr[_bankParams[(uint)mode].SPSRIndex] = value;


        public void ResetRegisters()
        {
            for (int i = 0; i < 16; i++) _registers[i] = 0;
            Array.Clear(_bankStore, 0, _bankStore.Length);
            Array.Clear(_bankedSpsr, 0, _bankedSpsr.Length);
            CPSR = (Flags)((0b11 << 6) | (uint)PrivilegeMode.SVC);
            // I and F set; mode SVC
        }

        public void PrintRegisters()
        {
            Console.WriteLine($"Mode: {CurrentMode}");

            for (int i = 0; i < RegisterArray.Length; i++)
                Console.WriteLine($"R{i}: 0x{_registers[i]:X8}");

            Console.WriteLine($"CPSR: 0x{CPSR:X8}");
            if (!IsUserOrSystem(CurrentMode)) Console.WriteLine($"SPSR: 0x{(uint)SPSR:X8}");
            Console.WriteLine(new string('-', 30));
        }

        public uint[] CopyRegisters()
        {
            uint[] result = new uint[RegisterArray.Length];

            Unsafe.CopyBlockUnaligned
            (
                ref Unsafe.As<uint, byte>(ref result[0]),
                ref Unsafe.As<RegisterArray, byte>(ref _registers),
                (uint)(RegisterArray.Length * sizeof(uint))
            );

            return result;
        }


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

        /// <summary>The currently saved program status register.</summary>
        public Flags SPSR
        {
            get => IsUserOrSystem(CurrentMode) ? CPSR : _bankedSpsr[_bankParams[(uint)CurrentMode].SPSRIndex];
            set
            {
                if (!IsUserOrSystem(CurrentMode))
                    _bankedSpsr[_bankParams[(uint)CurrentMode].SPSRIndex] = value;
            }
        }
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
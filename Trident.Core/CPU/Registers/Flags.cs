namespace Trident.Core.CPU.Registers
{
    /// <summary>
    /// The flags used in the status registers.
    /// </summary>
    [Flags]
    public enum Flags : uint
    {
        /// <summary>Indicates Thumb execution mode (when 1)</summary>
        T = 1 << 5,

        /// <summary>Disables Fast Interrupt Requests (FIQ)</summary>
        F = 1 << 6,

        /// <summary>Disables Interrupt Requests (IRQ)</summary>
        I = 1 << 7,

        /// <summary>Indicates an overflow condition (signed overflow)</summary>
        V = 1 << 28,

        /// <summary>Indicates a carry or borrow occurred</summary>
        C = 1 << 29,

        /// <summary>Indicates the result was zero</summary>
        Z = 1 << 30,

        /// <summary>Indicates a negative result</summary>
        N = 1u << 31
    }
}
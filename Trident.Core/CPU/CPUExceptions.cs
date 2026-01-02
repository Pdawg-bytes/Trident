using Trident.Core.Bus;

namespace Trident.Core.CPU;

/// <summary>
/// Represents an exception thrown when the CPU encounters an invalid instruction.
/// </summary>
public class InvalidInstructionException<TBus> : Exception where TBus : struct, IDataBus
{
    public InvalidInstructionException() { }

    public InvalidInstructionException(string message, ARM7TDMI<TBus> cpu)
        : base($"Invalid {(cpu.Registers.IsFlagSet(Registers.Flags.T) ? "Thumb" : "ARM")} instruction! Message: {message}") { }

    public InvalidInstructionException(string message, Exception innerException, ARM7TDMI<TBus> cpu)
        : base($"Invalid {(cpu.Registers.IsFlagSet(Registers.Flags.T) ? "Thumb" : "ARM")} instruction! Message: {message}", innerException) { }
}
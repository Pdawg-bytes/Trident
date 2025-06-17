using Trident.Core.CPU;
using Trident.Core.Enums;
using Trident.Tests.SingleStep.Models;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal static class CPUHelper
    {
        internal static void ApplyInitialState(ARM7TDMI<TransactionalMemory> cpu, RegisterState state)
        {
            cpu.Registers.SwitchMode(PrivilegeMode.User);

            for (uint i = 0; i < 16; i++)
                cpu.Registers[i] = state.R[(int)i];

            cpu.Registers.SetBankForMode(state.RFiq, PrivilegeMode.FIQ);
            cpu.Registers.SetBankForMode(state.RIrq, PrivilegeMode.IRQ);
            cpu.Registers.SetBankForMode(state.RSvc, PrivilegeMode.Supervisor);
            cpu.Registers.SetBankForMode(state.RAbt, PrivilegeMode.Abort);
            cpu.Registers.SetBankForMode(state.RUnd, PrivilegeMode.Undefined);

            cpu.Registers.SetSpsrForMode(PrivilegeMode.FIQ, (Flags)state.Spsr[0]);
            cpu.Registers.SetSpsrForMode(PrivilegeMode.IRQ, (Flags)state.Spsr[3]);
            cpu.Registers.SetSpsrForMode(PrivilegeMode.Supervisor, (Flags)state.Spsr[1]);
            cpu.Registers.SetSpsrForMode(PrivilegeMode.Abort, (Flags)state.Spsr[2]);
            cpu.Registers.SetSpsrForMode(PrivilegeMode.Undefined, (Flags)state.Spsr[4]);

            cpu.Registers.CPSR = (Flags)state.Cpsr;
            cpu.Registers.SwitchMode((PrivilegeMode)(state.Cpsr & 0x1F));

            cpu.FillPipeline(state.Pipeline);
            cpu.SetPipelineAccess((PipelineAccess)state.Access);
        }

        internal static void AssertState(ARM7TDMI<TransactionalMemory> cpu, SystemState state)
        {

        }
    }
}
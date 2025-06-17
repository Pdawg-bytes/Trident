using System.Drawing;
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
            cpu.Registers.SwitchMode(PrivilegeMode.User);

            for (int i = 0; i < 16; i++)
                Assert.AreEqual(state.Final.R[i], cpu.Registers[i], $"R{i}");

            CompareBank(cpu, PrivilegeMode.FIQ, state.Final);
            CompareBank(cpu, PrivilegeMode.IRQ, state.Final);
            CompareBank(cpu, PrivilegeMode.Supervisor, state.Final);
            CompareBank(cpu, PrivilegeMode.Abort, state.Final);
            CompareBank(cpu, PrivilegeMode.Undefined, state.Final);
        }

        private static void CompareBank(ARM7TDMI<TransactionalMemory> cpu, PrivilegeMode mode, RegisterState expected)
        {
            (List<uint> expectedBank, int spsrIndex) = mode switch
            {
                PrivilegeMode.User or PrivilegeMode.System => (expected.R, -1),
                PrivilegeMode.FIQ => (expected.RFiq, 0),
                PrivilegeMode.IRQ => (expected.RIrq, 3),
                PrivilegeMode.Supervisor => (expected.RSvc, 1),
                PrivilegeMode.Abort => (expected.RAbt, 2),
                PrivilegeMode.Undefined => (expected.RUnd, 4),
                _ => throw new InvalidOperationException($"Unexpected mode: {mode} for bank comparison")
            };

            Span<uint> bank = stackalloc uint[expectedBank.Count];
            cpu.Registers.GetBankForMode(mode, bank);

            for (int i = 0; i < bank.Length; i++)
                Assert.AreEqual(expectedBank[i], bank[i], $"R_{mode}_{i}");

            if (mode != PrivilegeMode.User && mode != PrivilegeMode.System)
                Assert.AreEqual(cpu.Registers.GetSpsrForMode(mode), expected.Spsr[spsrIndex], $"SPSR_{mode}");
        }
    }
}
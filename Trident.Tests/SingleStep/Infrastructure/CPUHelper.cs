using System.Drawing;
using Trident.Core.CPU;
using Trident.Core.Enums;
using System.Runtime.InteropServices;
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

            SetBankAndSpsrForMode(cpu, PrivilegeMode.FIQ, state);
            SetBankAndSpsrForMode(cpu, PrivilegeMode.IRQ, state);
            SetBankAndSpsrForMode(cpu, PrivilegeMode.Supervisor, state);
            SetBankAndSpsrForMode(cpu, PrivilegeMode.Abort, state);
            SetBankAndSpsrForMode(cpu, PrivilegeMode.Undefined, state);

            cpu.Registers.CPSR = (Flags)state.Cpsr;
            cpu.Registers.SwitchMode((PrivilegeMode)(state.Cpsr & 0x1F));

            cpu.Pipeline.Prefetch[0] = state.Pipeline[0];
            cpu.Pipeline.Prefetch[1] = state.Pipeline[1];
            cpu.Pipeline.Access = (PipelineAccess)state.Access;
        }

        internal static void AssertState(ARM7TDMI<TransactionalMemory> cpu, RegisterState state)
        {
            Assert.AreEqual(state.Cpsr, (uint)cpu.Registers.CPSR, "CPSR");

            cpu.Registers.SwitchMode(PrivilegeMode.User);

            for (int i = 0; i < 16; i++)
                Assert.AreEqual(state.R[i], cpu.Registers[i], $"R{i}");

            CompareBank(cpu, PrivilegeMode.FIQ, state);
            CompareBank(cpu, PrivilegeMode.IRQ, state);
            CompareBank(cpu, PrivilegeMode.Supervisor, state);
            CompareBank(cpu, PrivilegeMode.Abort, state);
            CompareBank(cpu, PrivilegeMode.Undefined, state);

            Assert.AreEqual(state.Pipeline[0], cpu.Pipeline.Prefetch[0], "Pipeline[0]");
            Assert.AreEqual(state.Pipeline[1], cpu.Pipeline.Prefetch[1], "Pipeline[1]");
            Assert.AreEqual((PipelineAccess)state.Access, cpu.Pipeline.Access, "Pipeline access");
        }


        private static void SetBankAndSpsrForMode(ARM7TDMI<TransactionalMemory> cpu, PrivilegeMode mode, RegisterState state)
        {
            (List<uint> bank, Flags spsr) = mode switch
            {
                PrivilegeMode.FIQ =>        (state.RFiq, (Flags)state.Spsr[0]),
                PrivilegeMode.IRQ =>        (state.RIrq, (Flags)state.Spsr[3]),
                PrivilegeMode.Supervisor => (state.RSvc, (Flags)state.Spsr[1]),
                PrivilegeMode.Abort =>      (state.RAbt, (Flags)state.Spsr[2]),
                PrivilegeMode.Undefined =>  (state.RUnd, (Flags)state.Spsr[4]),
                _ => throw new InvalidOperationException($"Unsupported mode for bank/SPSR setting: {mode}")
            };

            cpu.Registers.SetBankForMode(mode, CollectionsMarshal.AsSpan(bank));
            cpu.Registers.SetSPSR(mode, spsr);
        }

        private static void CompareBank(ARM7TDMI<TransactionalMemory> cpu, PrivilegeMode mode, RegisterState expected)
        {
            (List<uint> expectedBank, Flags spsr) = mode switch
            {
                PrivilegeMode.User or PrivilegeMode.System => (expected.R, (Flags)0),
                PrivilegeMode.FIQ =>                          (expected.RFiq, (Flags)expected.Spsr[0]),
                PrivilegeMode.IRQ =>                          (expected.RIrq, (Flags)expected.Spsr[3]),
                PrivilegeMode.Supervisor =>                   (expected.RSvc, (Flags)expected.Spsr[1]),
                PrivilegeMode.Abort =>                        (expected.RAbt, (Flags)expected.Spsr[2]),
                PrivilegeMode.Undefined =>                    (expected.RUnd, (Flags)expected.Spsr[4]),
                _ => throw new InvalidOperationException($"Unexpected mode: {mode} for bank comparison")
            };

            Span<uint> bank = stackalloc uint[expectedBank.Count];
            cpu.Registers.GetBankForMode(mode, bank);

            for (int i = 0; i < bank.Length; i++)
                Assert.AreEqual(expectedBank[i], bank[i], $"R_{mode}_{i}");

            if (mode != PrivilegeMode.User && mode != PrivilegeMode.System)
                Assert.AreEqual(spsr, (Flags)cpu.Registers.GetSPSR(mode), $"SPSR_{mode}");
        }
    }
}
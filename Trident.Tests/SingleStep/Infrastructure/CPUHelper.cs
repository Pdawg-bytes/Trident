using Trident.Core.CPU;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using System.Runtime.InteropServices;
using Trident.Tests.SingleStep.Models;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal static class CPUHelper
    {
        internal static void ApplyInitialState(ARM7TDMI<TransactionalMemory> cpu, RegisterState state)
        {
            cpu.Registers.ResetRegisters();
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
            var errors = new List<string>();

            void AddError(string label, object expected, object actual) =>
                errors.Add($"    {label} mismatch: expected <{expected}>, actual <{actual}>");


            if (state.Cpsr != (uint)cpu.Registers.CPSR)
            {
                uint expected = state.Cpsr;
                uint actual = (uint)cpu.Registers.CPSR;
                string flagDiff = FormatPSRDiff(expected, actual);
                errors.Add($"    CPSR mismatch: expected <0x{expected:X8}>, actual <0x{actual:X8}>");
                errors.Add(string.Join("\n", flagDiff.Split('\n').Select(line => "        " + line)));
            }

            cpu.Registers.SwitchMode(PrivilegeMode.User);

            for (int i = 0; i < 16; i++)
            {
                if (state.R[i] != cpu.Registers[i])
                    AddError($"R{i}", $"0x{state.R[i]:X8}", $"0x{cpu.Registers[i]:X8}");
            }

            CompareBank(cpu, PrivilegeMode.FIQ, state, errors);
            CompareBank(cpu, PrivilegeMode.IRQ, state, errors);
            CompareBank(cpu, PrivilegeMode.Supervisor, state, errors);
            CompareBank(cpu, PrivilegeMode.Abort, state, errors);
            CompareBank(cpu, PrivilegeMode.Undefined, state, errors);

            if (state.Pipeline[0] != cpu.Pipeline.Prefetch[0])
                AddError("Pipeline[0]", $"0x{state.Pipeline[0]:X8}", $"0x{cpu.Pipeline.Prefetch[0]:X8}");

            if (state.Pipeline[1] != cpu.Pipeline.Prefetch[1])
                AddError("Pipeline[1]", $"0x{state.Pipeline[1]:X8}", $"0x{cpu.Pipeline.Prefetch[1]:X8}");

            if ((PipelineAccess)state.Access != cpu.Pipeline.Access)
                AddError("Pipeline access", (PipelineAccess)state.Access, cpu.Pipeline.Access);

            if (errors.Count > 0)
            {
                var message = "State assertion failed with the following mismatches:\n" + string.Join("\n", errors);
                Assert.Fail(message);
            }
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

        private static void CompareBank(ARM7TDMI<TransactionalMemory> cpu, PrivilegeMode mode, RegisterState expected, List<string> errors)
        {
            (List<uint> expectedBank, Flags spsr) = mode switch
            {
                PrivilegeMode.User or PrivilegeMode.System => (expected.R, (Flags)0),
                PrivilegeMode.FIQ => (expected.RFiq, (Flags)expected.Spsr[0]),
                PrivilegeMode.IRQ => (expected.RIrq, (Flags)expected.Spsr[3]),
                PrivilegeMode.Supervisor => (expected.RSvc, (Flags)expected.Spsr[1]),
                PrivilegeMode.Abort => (expected.RAbt, (Flags)expected.Spsr[2]),
                PrivilegeMode.Undefined => (expected.RUnd, (Flags)expected.Spsr[4]),
                _ => throw new InvalidOperationException($"Unexpected mode: {mode} for bank comparison")
            };

            Span<uint> bank = stackalloc uint[expectedBank.Count];
            cpu.Registers.GetBankForMode(mode, bank);

            for (int i = 0; i < bank.Length; i++)
            {
                if (expectedBank[i] != bank[i])
                    errors.Add($"    R_{mode}_{i} mismatch: expected 0x{expectedBank[i]:X8}, actual 0x{bank[i]:X8}");
            }

            if (mode != PrivilegeMode.User && mode != PrivilegeMode.System)
            {
                var actualSpsr = (Flags)cpu.Registers.GetSPSR(mode);
                if (spsr != actualSpsr)
                    errors.Add($"    SPSR_{mode} mismatch: expected {spsr}, actual {actualSpsr}");
            }
        }


        private static string FormatPSRDiff(uint expected, uint actual)
        {
            var flags = new[]
            {
                ("N", 31),
                ("Z", 30),
                ("C", 29),
                ("V", 28),
                ("I", 7),
                ("F", 6),
                ("T", 5)
            };

            var result = new List<string>();

            foreach (var (name, bit) in flags)
            {
                bool exp = (expected & (1u << bit)) != 0;
                bool act = (actual & (1u << bit)) != 0;

                if (exp != act)
                    result.Add($"{name}: {(exp ? "1" : "0")} -> {(act ? "1" : "0")}");
            }

            uint expectedMode = expected & 0b11111;
            uint actualMode = actual & 0b11111;
            if (expectedMode != actualMode)
                result.Add($"Mode: 0b{Convert.ToString(expectedMode, 2).PadLeft(5, '0')} -> 0b{Convert.ToString(actualMode, 2).PadLeft(5, '0')}\n");

            return string.Join("\n", result);
        }
    }
}
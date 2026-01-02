using Trident.Core.CPU;
using Trident.Core.CPU.Pipeline;
using Trident.Tests.SingleStep.Models;
using static Trident.Tests.SingleStep.Infrastructure.TestTypeResolver;

namespace Trident.Tests.SingleStep.Constraints;

internal class MRSWritebackConstraint : ITestConstraint
{
    public bool Matches(TestType type, SystemState testCase) => type is TestType.ArmMrs && ((testCase.Opcode >> 12) & 0x0F) == 15;

    public void Apply(SystemState testCase)
    {
        // Pipeline won't be touched so just skip 
        if (!ConditionMet(testCase.Opcode >> 28, testCase.Initial.Cpsr))
            return;

        bool isSPSR = ((testCase.Opcode >> 22) & 1) == 1;
        ProcessorMode mode = (ProcessorMode)(testCase.Initial.Cpsr & 0x1F);

        uint psrValue = isSPSR ? mode switch
        {
            ProcessorMode.USR or ProcessorMode.SYS => testCase.Initial.Cpsr,
            ProcessorMode.FIQ => testCase.Initial.Spsr[0],
            ProcessorMode.IRQ => testCase.Initial.Spsr[3],
            ProcessorMode.SVC => testCase.Initial.Spsr[1],
            ProcessorMode.ABT => testCase.Initial.Spsr[2],
            ProcessorMode.UND => testCase.Initial.Spsr[4],
        }
        : testCase.Initial.Cpsr;

        uint firstAddr = psrValue & ~3u;
        uint nextAddr = (psrValue & ~3u) + 4;

        testCase.Transactions.Add(new Transaction
        {
            Kind = 0,
            Size = 4,
            Addr = firstAddr,
            Data = firstAddr,
            Cycle = 3,
            Access = (int)(PipelineAccess.Code | PipelineAccess.NonSequential)
        });

        testCase.Transactions.Add(new Transaction
        {
            Kind = 0,
            Size = 4,
            Addr = nextAddr,
            Data = nextAddr,
            Cycle = 4,
            Access = (int)(PipelineAccess.Code | PipelineAccess.NonSequential)
        });

        testCase.Final.Pipeline[0] = firstAddr;
        testCase.Final.Pipeline[1] = nextAddr;

        testCase.Final.R[15] = psrValue + 8;
    }


    private bool ConditionMet(uint condition, uint cpsr) => (_conditionLUT[condition] & (1 << (int)(cpsr >> 28))) != 0;

    private static readonly ushort[] _conditionLUT =
    [
        0xF0F0, // EQ: Z == 1
			0x0F0F, // NE: Z == 0
			0xCCCC, // CS: C == 1
			0x3333, // CC: C == 0
			0xFF00, // MI: N == 1
			0x00FF, // PL: N == 0
			0xAAAA, // VS: V == 1
			0x5555, // VC: V == 0
			0x0C0C, // HI: (C == 1) && (Z == 0)
			0xF3F3, // LS: (C == 0) || (Z == 1)
			0xAA55, // GE: N == V
			0x55AA, // LT: N != V
			0x0A05, // GT: (Z == 0) && (N == V)
			0xF5FA, // LE: (Z == 1) || (N != V)
			0xFFFF, // AL: 1
			0x0000  // NV: 0
    ];
}
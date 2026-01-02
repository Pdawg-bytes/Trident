using Trident.Tests.SingleStep.Models;
using static Trident.Tests.SingleStep.Infrastructure.TestTypeResolver;

namespace Trident.Tests.SingleStep.Constraints;

internal class IgnoreCarryConstraint : ITestConstraint
{
    public bool Matches(TestType type, SystemState testCase)
    {
        return 
            type is TestType.ArmMulMla || 
            type is TestType.ArmMullMlal || 
            (type is TestType.ThumbDataProc && ((testCase.Opcode >> 6) & 0xF) == 0b1101);
    }

    public void Apply(SystemState testCase)
    {
        unchecked
        {
            const uint CarryMask = 1u << 29;
            testCase.Final.Cpsr = (testCase.Final.Cpsr & ~CarryMask) | (testCase.Initial.Cpsr & CarryMask);
        }
    }
}
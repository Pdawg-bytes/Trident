using Trident.Tests.SingleStep.Models;
using static Trident.Tests.SingleStep.Infrastructure.TestTypeResolver;

namespace Trident.Tests.SingleStep.Constraints
{
    internal class MulR15Constraint : ITestConstraint
    {
        public bool Matches(TestType type, SystemState testCase) =>
            (type == TestType.ArmMulMla && ((testCase.Opcode >> 16) & 0x0F) == 15) ||
            (type == TestType.ArmMullMlal &&
                (((testCase.Opcode >> 16) & 0x0F) == 15 || ((testCase.Opcode >> 12) & 0x0F) == 15));

        public void Apply(SystemState testCase)
        {
            testCase.Final.R[15] = testCase.Initial.R[15] + 4;
            testCase.Final.Pipeline[0] = testCase.Initial.Pipeline[1];
            testCase.Final.Pipeline[1] = testCase.Transactions[0].Data;
            testCase.IgnoreAccessMismatch = true;
        }
    }
}
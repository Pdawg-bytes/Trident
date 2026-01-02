using static Trident.Tests.SingleStep.Infrastructure.TestTypeResolver;

namespace Trident.Tests.SingleStep.Models;

internal interface ITestConstraint
{
    void Apply(SystemState testCase);
    bool Matches(TestType testType, SystemState testCase);
}
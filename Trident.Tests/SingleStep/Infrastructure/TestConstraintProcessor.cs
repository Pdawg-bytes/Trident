using Trident.Tests.SingleStep.Models;
using Trident.Tests.SingleStep.Constraints;
using static Trident.Tests.SingleStep.Infrastructure.TestTypeResolver;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal class TestConstraintProcessor
    {
        private readonly List<ITestConstraint> _constraints = new();

        internal void Register(ITestConstraint constraint) => _constraints.Add(constraint);

        internal void ApplyConstraints(TestType type, SystemState testCase)
        {
            foreach (var constraint in _constraints)
            {
                if (constraint.Matches(type, testCase))
                    constraint.Apply(testCase);
            }
        }

        internal static TestConstraintProcessor CreateConstraintProcessor()
        {
            var processor = new TestConstraintProcessor();
            processor.Register(new IgnoreCarryConstraint());
            return processor;
        }
    }
}
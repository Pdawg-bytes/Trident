using System.Reflection;
using System.Diagnostics;
using System.Threading.Channels;
using Trident.Tests.SingleStep.Models;
using Trident.Tests.SingleStep.Constraints;
using Trident.Tests.SingleStep.Infrastructure;

namespace Trident.Tests.SingleStep
{
    [TestClass]
    public class SingleStepRunner
    {
        [TestMethod]
        [DynamicData(nameof(GetJsonFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetFileName))]
        public async Task RunTestsFromFileAsync(string filePath)
        {
            object writeLock = new();

            var channel = Channel.CreateUnbounded<IndexedTestCase>();
            int consumerCount = Debugger.IsAttached ? 1 : Environment.ProcessorCount / 2;

            int failures = 0;
            var constraintProcessor = new TestConstraintProcessor();
            constraintProcessor.Register(new IgnoreCarryConstraint());
            constraintProcessor.Register(new MulR15Constraint());
            constraintProcessor.Register(new MRSWritebackConstraint());

            var tasks = TestManagement.CreateConsumers
            (
                channel, 
                consumerCount, 
                filePath, 
                constraintProcessor, 
                writeLock, 
                () => Interlocked.Increment(ref failures)
            );

            await TestManagement.EnqueueTestCasesAsync(filePath, channel);
            channel.Writer.Complete();

            await Task.WhenAll(tasks);

            if (failures > 0)
                Assert.Fail($"{failures} failures.");
        }


        public static IEnumerable<string[]> GetJsonFiles()
        {
            string baseDir = @"D:\Source\Git\ARM7TDMI\v1";
            foreach (var file in Directory.GetFiles(baseDir, "*.json"))
                yield return new string[] { file };
        }

        public static string GetFileName(MethodInfo methodInfo, object[] data)
        {
            string file = Path.GetFileNameWithoutExtension((string)data[0]);
            return $"{methodInfo.Name}_{file}";
        }
    }
}
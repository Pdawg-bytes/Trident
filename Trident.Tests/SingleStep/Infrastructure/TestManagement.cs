using System.Text.Json;
using Trident.Core.CPU;
using System.Threading.Channels;
using Trident.Tests.SingleStep.Models;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal static class TestManagement
    {
        internal static List<Task> CreateConsumers(
            Channel<IndexedTestCase> channel,
            int consumerCount,
            string filePath,
            TestConstraintProcessor constraintProcessor,
            object writeLock,
            Action incrementFailure)
        {
            var testType = TestTypeResolver.GetTestType(filePath);

            return Enumerable.Range(0, consumerCount).Select(workerId => Task.Run(async () =>
            {
                var cpu = new ARM7TDMI<TransactionalMemory>(new Core.Scheduling.Scheduler());
                cpu.AttachBus(new TransactionalMemory());
                cpu.Reset();

                await foreach (var entry in channel.Reader.ReadAllAsync())
                {
                    var testCase = entry.TestCase;
                    try
                    {
                        constraintProcessor.ApplyConstraints(testType, testCase);
                        CPUHelper.ApplyInitialState(cpu, testCase.Initial);
                        cpu.Bus.Initialize(testCase.Transactions);
                        cpu.Step();
                        CPUHelper.AssertState(cpu, testCase);
                    }
                    catch (Exception ex)
                    {
                        lock (writeLock)
                        {
                            incrementFailure();
                            Console.WriteLine($"[#{entry.Index}] failed: Opcode=0x{testCase.Opcode:X8}, message: {ex.Message}\n");
                        }
                    }
                }
            })).ToList();
        }


        internal static async Task EnqueueTestCasesAsync(string filePath, Channel<IndexedTestCase> channel)
        {
            await using FileStream stream = File.OpenRead(filePath);
            int index = 0;

            await foreach (var testCase in JsonSerializer.DeserializeAsyncEnumerable<SystemState>(stream))
            {
                await channel.Writer.WriteAsync(new IndexedTestCase(index++, testCase));
            }
        }
    }
}

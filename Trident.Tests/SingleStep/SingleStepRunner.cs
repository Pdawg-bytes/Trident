using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Trident.Tests.SingleStep.Infrastructure;
using Trident.Tests.SingleStep.Models;
using Trident.Core.CPU;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using System.Text.RegularExpressions;

namespace Trident.Tests.SingleStep
{
    [TestClass]
    public class SingleStepRunner
    {
        [TestMethod]
        [DynamicData(nameof(GetJsonFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetFileName))]
        public async Task RunTestsFromFileAsync(string filePath)
        {
            ConcurrentBag<string> failures = new();
            var channel = Channel.CreateUnbounded<IndexedTestCase>();

            int consumerCount = Environment.ProcessorCount / 2;
            List<Task> tasks = Enumerable.Range(0, consumerCount).Select(workerId => Task.Run(async () =>
            {
                var cpu = new ARM7TDMI<TransactionalMemory>();
                cpu.AttachBus(new TransactionalMemory());

                await foreach (var entry in channel.Reader.ReadAllAsync())
                {
                    try
                    {
                        SystemState testCase = entry.TestCase;
                        CPUInitializer.ApplyInitialState(cpu, testCase.Initial);

                        // TODO: actually step cpu and then test. This is just to see if the runner is working.
                        if (testCase.Opcode > 0xDDAA)
                            failures.Add($"[#{entry.Index}] failed: Opcode=0x{testCase.Opcode:X8}");
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"[Worker {workerId}, #{entry.Index}] exception: {ex.Message}");
                    }
                }
            })).ToList();

            await using var stream = File.OpenRead(filePath);
            int index = 0;
            await foreach (var testCase in JsonSerializer.DeserializeAsyncEnumerable<SystemState>(stream))
                await channel.Writer.WriteAsync(new IndexedTestCase(index++, testCase));

            channel.Writer.Complete();
            await Task.WhenAll(tasks);

            if (!failures.IsEmpty)
            {
                var orderedFailures = failures.OrderBy(failure =>
                {
                    Match match = Regex.Match(failure, @"\[#(\d+)\]");
                    return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
                }).ToList();
                Assert.Fail($"{orderedFailures.Count} failures:\n{string.Join("\n", orderedFailures)}");
                //Assert.IsFalse(failures.Count != 0);
            }
        }

        public static IEnumerable<string[]> GetJsonFiles()
        {
            string baseDir = @"";
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
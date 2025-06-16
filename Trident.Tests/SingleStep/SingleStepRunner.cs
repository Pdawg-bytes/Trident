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
            string failureFile = $"{filePath}.failures.tmp";
            object writeLock = new();

            using StreamWriter failureWriter = new(failureFile, false);
            var channel = Channel.CreateUnbounded<IndexedTestCase>();

            int consumerCount = Environment.ProcessorCount;
            List<Task> tasks = Enumerable.Range(0, consumerCount).Select(workerId => Task.Run(async () =>
            {
                ARM7TDMI<TransactionalMemory> cpu = new();
                cpu.AttachBus(new TransactionalMemory());

                await foreach (var entry in channel.Reader.ReadAllAsync())
                {
                    try
                    {
                        SystemState testCase = entry.TestCase;
                        CPUInitializer.ApplyInitialState(cpu, testCase.Initial);

                        if (testCase.Opcode > 0xDDAA)
                        {
                            // TODO: actually step cpu and then test. This is just to see if the runner is working.
                            lock (writeLock)
                                failureWriter.WriteLine($"[#{entry.Index}] failed: Opcode=0x{testCase.Opcode:X8}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (writeLock)
                            failureWriter.WriteLine($"[Worker {workerId}, #{entry.Index}] exception: {ex.Message}");
                    }
                }
            })).ToList();

            await using FileStream stream = File.OpenRead(filePath);
            int index = 0;
            await foreach (var testCase in JsonSerializer.DeserializeAsyncEnumerable<SystemState>(stream))
                await channel.Writer.WriteAsync(new IndexedTestCase(index++, testCase));

            channel.Writer.Complete();
            await Task.WhenAll(tasks);

            failureWriter.Flush();
            failureWriter.Close();

            if (File.Exists(failureFile))
            {
                int failureCount = 0;
                List<string> sampleLines = new();

                using (StreamReader reader = File.OpenText(failureFile))
                {
                    string? line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        failureCount++;
                        if (sampleLines.Count < 10)
                            sampleLines.Add(line);
                    }
                }

                if (failureCount > 0)
                {
                    string preview = string.Join("\n", sampleLines);
                    Assert.Fail(
                        $"{failureCount} failures. Showing first {sampleLines.Count}:\n" +
                        $"{preview}\n\n" +
                        $"Full log saved to: {failureFile}");
                }
                else
                    File.Delete(failureFile);
            }
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
using System.Text.Json;
using Trident.Core.CPU;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Channels;
using Trident.Tests.SingleStep.Models;
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
            string failureFile = $"{filePath}.failures.tmp";
            object writeLock = new();

            using StreamWriter failureWriter = new(failureFile, false);
            var channel = Channel.CreateUnbounded<IndexedTestCase>();

            int consumerCount = Debugger.IsAttached ? 1 : Environment.ProcessorCount / 2;

            List<Task> tasks = Enumerable.Range(0, consumerCount).Select(workerId => Task.Run(async () =>
            {
                ARM7TDMI<TransactionalMemory> cpu = new();
                cpu.AttachBus(new TransactionalMemory());
                cpu.Reset();

                await foreach (var entry in channel.Reader.ReadAllAsync())
                {
                    SystemState testCase = entry.TestCase;
                    try
                    {
                        CPUHelper.ApplyInitialState(cpu, testCase.Initial);
                        cpu.Bus.Initialize(testCase.BaseAddr, testCase.Opcode, testCase.Transactions);
                        cpu.Step();
                        CPUHelper.AssertState(cpu, testCase.Final);
                    }
                    catch (Exception ex)
                    {
                        lock (writeLock) 
                            failureWriter.WriteLine($"[#{entry.Index}] failed: Opcode={testCase.Opcode}, message: {ex.Message}");
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
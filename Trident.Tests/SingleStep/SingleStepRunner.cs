using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Trident.Tests.SingleStep.Infrastructure;
using Trident.Tests.SingleStep.Models;
using Trident.Core.CPU;

namespace Trident.Tests.SingleStep
{
    [TestClass]
    public class SingleStepRunner
    {
        private static List<SystemState>? _testCases;

        [ClassInitialize]
        public static void LoadTests(TestContext context)
        {
            string json = File.ReadAllText(@"G:\Source\Git\ARM7TDMI\v1\thumb_data_proc.json");
            _testCases = JsonSerializer.Deserialize<List<SystemState>>(json);
        }
    }
}
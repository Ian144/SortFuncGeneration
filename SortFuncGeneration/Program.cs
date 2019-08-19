using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// ReSharper disable UnusedVariable

namespace SortFuncGeneration
{
    static class Program
    {
        static void Main()
        {

            TestDataCreation.CreateAndPersistData(50000);

            var sb = new SortingBenchmarks();
            bool comparisonValid = sb.CheckValidBenchmarks();

            if (comparisonValid)
            {
                var summary = BenchmarkRunner.Run<SortingBenchmarks>(DefaultConfig.Instance.With(Job.RyuJitX64).With(ConfigOptions.DisableOptimizationsValidator));
            }

            else
            {
                Console.WriteLine("invalid benchmark, handcoded is not equivalent to generated");
            }
        }
    }
}

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

            // checks all sort methods produce the same results
            var bmark = new Benchmarks();

            if (bmark.IsValid())
            {
                var summary = BenchmarkRunner.Run<Benchmarks>(DefaultConfig.Instance.With(Job.RyuJitX64).With(ConfigOptions.DisableOptimizationsValidator));
            }
            else
            {
                Console.WriteLine("invalid benchmark");
            }
        }
    }
}

using BenchmarkDotNet.Running;

// ReSharper disable UnusedVariable

namespace SortFuncGeneration
{
    static class Program
    {
        static void Main()
        {

            TestDataCreation.CreateAndPersistData();

            
            var summary = BenchmarkRunner.Run<SortingBenchmarks>();


            //var xs = new List<Target>
            //{
            //    new Target{IntProp1 = 99, IntProp2 = 88, StrProp1 = "aa", StrProp2 ="bb"},
            //    new Target{IntProp1 = 11, IntProp2 = 22, StrProp1 = "pp", StrProp2 ="qq"},
            //    new Target{IntProp1 = 11, IntProp2 = 22, StrProp1 = "xx", StrProp2 ="yy"},
            //};

            //var sortBys = new List<SortBy>
            //{
            //    new SortBy{PropName = "IntProp2", Ascending = true},
            //    new SortBy{PropName = "StrProp1", Ascending = false}
            //};

            //Func<Target, Target, int> sortMyClass = SortFuncCompiler.MakeSortFunc<Target>(sortBys);

            //var ys = xs.OrderBy(x => x, new MyComparer<Target>(sortMyClass));

            //foreach (var mc in ys)
            //{
            //    Console.WriteLine(mc);
            //}
        }
    }
}

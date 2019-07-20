using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FsCheck;

namespace SortFuncGeneration
{
    public class MyClass
    {
        public int IntProp1 { get; set; }
        public int IntProp2 { get; set; }
        public string StrProp1 { get; set; }
        public string StrProp2 { get; set; }

        public override string ToString()
        {
            return $"{IntProp1} - {IntProp2} - {StrProp1} - {StrProp2}";
        }
    }





    class Benchmarks
    {



        [GlobalSetup]
        public void Setup()
        {
            //var arb = new Arbitrary<SortFuncGeneration.MyClass>();

            //var xs = arb.Generator.Sample(1000);

            //data = new byte[N];
            //new Random(42).NextBytes(data);
        }



        [Benchmark]
        public void Benchmark1()
        {

        }
    }
}

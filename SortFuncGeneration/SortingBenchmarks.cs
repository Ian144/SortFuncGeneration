using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using FsCheck;

namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class SortingBenchmarks
    {
        private List<Target> _xs;
        private MyComparer<Target> _generatedComparer;
        private MyComparer<Target> _handCodedComparer;


        [IterationSetup]
        public void Setup()
        {
            //var arb = Arb.From<Target>();
            //_xs = arb.Generator.Sample(100000).ToList();

            byte[] bs = File.ReadAllBytes("targetData.data");
            using (var ms = new MemoryStream())
            {
                ms.Write(bs, 0, bs.Length);
                ms.Seek(0, SeekOrigin.Begin);
                _xs = ProtoBuf.Serializer.Deserialize<Target[]>(ms).ToList();
            }

            var sortBys = new List<SortBy>
            {
                new SortBy{PropName = "IntProp2", Ascending = true},
                new SortBy{PropName = "IntProp1", Ascending = false}
            };

            Func<Target, Target, int> sortFunc = SortFuncCompiler.MakeSortFunc<Target>(sortBys);

            _generatedComparer = new MyComparer<Target>(sortFunc);

            _handCodedComparer = new MyComparer<Target>(SortTwoIntsHC);
        }


        private static int SortTwoIntsHC( Target aa, Target bb )
        {
            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);

            if (s1 != 0) return s1;

            return bb.IntProp2.CompareTo(aa.IntProp2);
        }


        [Benchmark]
        public void GeneratedListSort()
        {
            _xs.Sort(_generatedComparer);
        }

        [Benchmark]
        public void HandCodedListSort()
        {
            _xs.Sort(_handCodedComparer);
        }

        [Benchmark]
        public void GeneratedOrderBy()
        {
            int cc = _xs.OrderBy(x => x, _generatedComparer).Count();
        }

        [Benchmark]
        public void HandCodedOrderBy()
        {
            int cc = _xs.OrderBy(x => x, _handCodedComparer).Count();
        }

        [Benchmark]
        public void OrderByThenByDesc()
        {
            int cc = _xs.OrderBy(x => x.IntProp1).ThenByDescending(x => x.IntProp2).Count();
        }
    }
}

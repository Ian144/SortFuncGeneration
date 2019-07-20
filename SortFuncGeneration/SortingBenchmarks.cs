using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using static System.String;

namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class SortingBenchmarks
    {
        private List<Target> _xs;
        private MyComparer<Target> _generatedComparer;
        private MyComparer<Target> _handCodedComparer;

        private readonly Consumer _consumer = new Consumer();


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
                new SortBy{PropName = "IntProp1", Ascending = true},
                new SortBy{PropName = "StrProp1", Ascending = false},
                new SortBy{PropName = "IntProp2", Ascending = true},
                new SortBy{PropName = "StrProp2", Ascending = false},
            };

            Func<Target, Target, int> sortFunc = SortFuncCompiler.MakeSortFunc<Target>(sortBys);

            _generatedComparer = new MyComparer<Target>(sortFunc);

            _handCodedComparer = new MyComparer<Target>(SortIntStrDescIntStrDescHC);
        }


        private static int SortStr1DescHC(Target aa, Target bb)
        {
            return CompareOrdinal(bb.StrProp1, aa.StrProp1);
        }


        private static int SortOneIntTwoStrsHC(Target aa, Target bb)
        {
            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);

            if (s1 != 0) return s1;

            // aa and bb flipped, as this comparison is descending
            int s2 = CompareOrdinal(bb.StrProp1, aa.StrProp1);

            if (s2 != 0) return s2;

            return CompareOrdinal(aa.StrProp2, bb.StrProp2);
        }


        private static int SortIntStrDescIntStrDescHC(Target aa, Target bb)
        {
            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
            if (s1 != 0) return s1;

            // aa and bb flipped, as this comparison is descending
            int s2 = CompareOrdinal(bb.StrProp1, aa.StrProp1);
            if (s2 != 0) return s2;

            int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
            if (s3 != 0) return s3;

            return CompareOrdinal(bb.StrProp2, aa.StrProp2);
        }


        private static int SortOneIntOneStrHC(Target aa, Target bb)
        {
            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);

            if (s1 != 0) return s1;

            // aa and bb flipped, as this comparison is descending
            return CompareOrdinal(bb.StrProp1, aa.StrProp1);
        }

        private static int SortTwoIntsHC( Target aa, Target bb )
        {
            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);

            if (s1 != 0) return s1;

            // order flipped, descending
            return bb.IntProp2.CompareTo(aa.IntProp2);
        }


        public bool CheckSortsEquivalent()
        {
            Setup();
            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var hcSorted = _xs.OrderBy(tt => tt, _handCodedComparer).ToList();

            var ordByThenByDesc = _xs
                .OrderBy(x => x.IntProp1)
                .ThenByDescending(x => x.StrProp1)
                .ThenBy(x => x.IntProp2)
                .ThenByDescending(x => x.StrProp2)
                .ToList();

            bool hcOk = genSorted.SequenceEqual(hcSorted);
            bool ordByOk = genSorted.SequenceEqual(ordByThenByDesc);
            return hcOk && ordByOk;
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
            _xs.OrderBy(x => x, _generatedComparer).Consume(_consumer);
        }

        [Benchmark]
        public void HandCodedOrderBy()
        {
            _xs.OrderBy(x => x, _handCodedComparer).Consume(_consumer);
        }

        [Benchmark]
        public void OrderByThenByDesc()
        {
            _xs
                .OrderBy(x => x.IntProp1)
                .ThenByDescending(x => x.StrProp1)
                .ThenBy(x => x.IntProp2)
                .ThenByDescending(x => x.StrProp2)
                .Consume(_consumer);

        }
    }
}

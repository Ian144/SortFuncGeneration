using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FastExpressionCompiler;
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
        private MyComparer<Target> _genTernComparer;
        private IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;


        [IterationSetup]
        public void Setup()
        {
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
                new SortBy{PropName = "IntProp2", Ascending = false},
                new SortBy{PropName = "StrProp1", Ascending = true},
                new SortBy{PropName = "StrProp2", Ascending = false},
            };

            // lazy, evaluated in a benchmark and in the check-valid function
            _lazyLinqOrderByThenBy = _xs
                    .OrderBy(x => x.IntProp1)
                    .ThenByDescending(x => x.IntProp2)
                    .ThenBy(x => x.StrProp1)
                    .ThenByDescending(x => x.StrProp2);

            Func<Target, Target, int> sortFunc = SortFuncCompiler.MakeSortFuncCompToMeth<Target>(sortBys);

            _generatedComparer = new MyComparer<Target>(sortFunc);

            _handCodedComparer = new MyComparer<Target>(HandCoded);

            Func<Target, Target, int> genTernSortFunc = SortFuncCompilerTernary.MakeSortFunc<Target>(sortBys);
            _genTernComparer = new MyComparer<Target>(genTernSortFunc);
        }

        private static int HandCoded(Target aa, Target bb)
        {
            // aa and bb flipped, when the comparison is descending

            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
            if (s1 != 0) return s1;

            int s3 = bb.IntProp2.CompareTo(aa.IntProp2);
            if (s3 != 0) return s3;

            int s2 = CompareOrdinal(aa.StrProp1, bb.StrProp1);
            if (s2 != 0) return s2;

            return CompareOrdinal(bb.StrProp2, aa.StrProp2);
        }

        public bool CheckSortsEquivalent()
        {
            Setup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var hcSorted = _xs.OrderBy(tt => tt, _handCodedComparer).ToList();
            var genTernComparerSorted = _xs.OrderBy(m => m, _genTernComparer).ToList();

            bool hcOk = referenceOrdering.SequenceEqual(hcSorted);
            bool genSortedOk = referenceOrdering.SequenceEqual(genSorted);
            bool genTernaryOk = referenceOrdering.SequenceEqual(genTernComparerSorted);

            return hcOk && genSortedOk && genTernaryOk;
        }

        [Benchmark]
        public void GeneratedListSortTernary()
        {
            _xs.Sort(_genTernComparer);
        }

        [Benchmark]
        public void GeneratedListSortCompToMeth()
        {
            _xs.Sort(_generatedComparer);
        }

        [Benchmark]
        public void HandCodedListSort()
        {
            _xs.Sort(_handCodedComparer);
        }

        [Benchmark]
        public void OrderByThenByDesc()
        {
            _lazyLinqOrderByThenBy.Consume(_consumer);
        }
    }
}

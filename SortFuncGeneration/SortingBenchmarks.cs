using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Nito.Comparers;
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
        private IComparer<Target> _nitoComparer;
        private IComparer<Target> _handCodedComposedFunctionsComparer;
        private MyComparer<Target> _handCodedTernary;


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
                new SortBy{PropName = "StrProp1", Ascending = true},
                new SortBy{PropName = "IntProp2", Ascending = true},
                new SortBy{PropName = "StrProp2", Ascending = true},
            };

            // lazy, evaluated in a benchmark and in the check-valid function
            _lazyLinqOrderByThenBy = _xs
                .OrderBy(x => x.IntProp1)
                .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
                .ThenBy(x => x.IntProp2)
                .ThenBy(x => x.StrProp2, StringComparer.Ordinal);

            Func<Target, Target, int> sortFunc = SortFuncCompiler.MakeSortFuncCompToMeth<Target>(sortBys);

            _generatedComparer = new MyComparer<Target>(sortFunc);

            _handCodedComparer = new MyComparer<Target>(HandCoded);

            _handCodedTernary = new MyComparer<Target>(HandCodedTernary);

            Func<Target, Target, int> genTernSortFunc = SortFuncCompilerTernary.MakeSortFunc<Target>(sortBys);
            _genTernComparer = new MyComparer<Target>(genTernSortFunc);

            _handCodedComposedFunctionsComparer = new MyComparer<Target>(HandCodedComposedFuncs);

            _nitoComparer = ComparerBuilder.For<Target>()
                .OrderBy(p => p.IntProp1)
                .ThenBy(p => p.StrProp1, StringComparer.Ordinal)
                .ThenBy(p => p.IntProp2)
                .ThenBy(p => p.StrProp2, StringComparer.Ordinal);
        }

        private static int HandCoded(Target aa, Target bb)
        {
            // aa and bb flipped when the comparison is descending

            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
            if (s1 != 0) return s1;

            int s2 = string.CompareOrdinal(aa.StrProp1, bb.StrProp1);
            if (s2 != 0) return s2;

            int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
            if (s3 != 0) return s3;

            return CompareOrdinal(aa.StrProp2, bb.StrProp2);
        }

        private static int HandCodedStrCmpOrd(Target aa, Target bb)
        {
            // aa and bb flipped when the comparison is descending

            int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
            if (s1 != 0) return s1;

            int s2 = string.Compare(aa.StrProp1, bb.StrProp1, StringComparison.Ordinal);
            if (s2 != 0) return s2;

            int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
            if (s3 != 0) return s3;

            return Compare(aa.StrProp2, bb.StrProp2, StringComparison.Ordinal);
        }


        private static int HandCodedComposedFuncs(Target aa, Target bb)
        {
            int CmpIntProp1(Target aa1, Target bb1) => aa1.IntProp1.CompareTo(bb1.IntProp1);
            int CmpStrProp1(Target aa2, Target bb2) => string.CompareOrdinal(aa2.StrProp1, bb2.StrProp1);
            int CmpIntProp2(Target aa3, Target bb3) => aa3.IntProp2.CompareTo(bb3.IntProp2);
            int CmpStrProp2(Target aa4, Target bb4) => string.CompareOrdinal(aa4.StrProp2, bb4.StrProp2);

            Func<Target, Target, int> [] funcs = {CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2};

            foreach (var func in funcs)
            {
                int cmp = func(aa, bb);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        private static int HandCodedTernary(Target xx, Target yy)
        {
            int tmp = 0;

            // assignment is an expression, the value of which is compared to 0
            int Sorter(Target aa, Target bb) => 
                (tmp = aa.IntProp1.CompareTo(bb.IntProp1)) != 0
                    ? tmp
                    : (tmp = string.CompareOrdinal(aa.StrProp1, bb.StrProp1)) != 0
                        ? tmp
                        : (tmp = aa.IntProp2.CompareTo(bb.IntProp2)) != 0
                            ? tmp
                            : string.CompareOrdinal(aa.StrProp2, bb.StrProp2);

            int cmp = Sorter(xx, yy);
            return cmp;
        }


        public bool CheckValidBenchmarks()
        {
            Setup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var hcSorted = _xs.OrderBy(tt => tt, _handCodedComparer).ToList();
            var genTernComparerSorted = _xs.OrderBy(m => m, _genTernComparer).ToList();
            var nitoSorted = _xs.OrderBy(m => m, _nitoComparer);
            var handCodedComposedFunctionsSorted = _xs.OrderBy(m => m, _handCodedComposedFunctionsComparer);
            var handCodedTernarySorted = _xs.OrderBy(m => m, _handCodedTernary).ToList();

            bool hcOk = referenceOrdering.SequenceEqual(hcSorted);
            bool genSortedOk = referenceOrdering.SequenceEqual(genSorted);
            bool genTernaryOk = referenceOrdering.SequenceEqual(genTernComparerSorted);
            bool nitoOk = referenceOrdering.SequenceEqual(nitoSorted);
            bool handCodedComposedFunctionsOk = referenceOrdering.SequenceEqual(handCodedComposedFunctionsSorted);
            bool handCodedTernaryOk = referenceOrdering.SequenceEqual(handCodedTernarySorted);

            //for (int ctr = 0; ctr < referenceOrdering.Count; ++ctr)
            //{
            //    var refTarget = referenceOrdering[ctr];
            //    var xxTarget = hcSorted[ctr];
            //    if(refTarget != xxTarget)
            //        Console.WriteLine($"failure at: {ctr}");
            //}

            return hcOk && genSortedOk && genTernaryOk && nitoOk && handCodedComposedFunctionsOk && handCodedTernaryOk;
        }

        [Benchmark]
        public void ComposedFunctionsListSort()
        {
            _xs.Sort(_nitoComparer);
        }


        [Benchmark]
        public void NitoListSort()
        {
            _xs.Sort(_nitoComparer);
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
        public void HandCodedTernaryListSort()
        {
            _xs.Sort(_handCodedTernary);
        }


        //[Benchmark]
        //public void HandCodedOrderBy()
        //{
        //    _xs.OrderBy(m => m, _handCodedComparer).Consume(_consumer);
        //}

        [Benchmark]
        public void LinqOrderByThenBy()
        {
            _lazyLinqOrderByThenBy.Consume(_consumer);
        }
    }
}

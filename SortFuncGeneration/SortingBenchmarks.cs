using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Nito.Comparers;
using SortFuncCommon;
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

        private static readonly Func<Target, Target, int>[] _composedSubFuncs = {CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2};


        [IterationSetup]
        public void Setup()
        {
            var fs = new FileStream("targetData.data", FileMode.Open, FileAccess.Read);
            _xs = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);

            var sortBys = new List<SortBy>{
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

            _generatedComparer = new MyComparer<Target>(
                    SortFuncCompiler.MakeSortFuncCompToMeth<Target>(sortBys)
                );

            _genTernComparer = new MyComparer<Target>(
                SortFuncCompilerTernary.MakeSortFunc<Target>(sortBys)
            );

            _handCodedComparer = new MyComparer<Target>(HandCoded);

            _handCodedTernary = new MyComparer<Target>(HandCodedTernary);

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

            int s2 = CompareOrdinal(aa.StrProp1, bb.StrProp1);
            if (s2 != 0) return s2;

            int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
            if (s3 != 0) return s3;

            return CompareOrdinal(aa.StrProp2, bb.StrProp2);
        }

        //private static int HandCodedStrCmpOrd(Target aa, Target bb)
        //{
        //    // aa and bb flipped when the comparison is descending

        //    int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
        //    if (s1 != 0) return s1;

        //    int s2 = string.Compare(aa.StrProp1, bb.StrProp1, StringComparison.Ordinal);
        //    if (s2 != 0) return s2;

        //    int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
        //    if (s3 != 0) return s3;

        //    return Compare(aa.StrProp2, bb.StrProp2, StringComparison.Ordinal);
        //}

        // this is the same as other hande coded funcs



        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpIntProp1(Target aa1, Target bb1) => aa1.IntProp1.CompareTo(bb1.IntProp1);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpStrProp1(Target aa2, Target bb2) => CompareOrdinal(aa2.StrProp1, bb2.StrProp1);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpIntProp2(Target aa3, Target bb3) => aa3.IntProp2.CompareTo(bb3.IntProp2);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpStrProp2(Target aa4, Target bb4) => CompareOrdinal(aa4.StrProp2, bb4.StrProp2);

        //private static int HandCodedComposedFuncs(Target aa, Target bb)
        //{
        //    int tmp;

        //    return (tmp = CmpIntProp1(aa, bb)) != 0
        //        ? tmp
        //        : (tmp = CmpStrProp1(aa, bb)) != 0
        //            ? tmp
        //            : (tmp = CmpIntProp2(aa, bb)) != 0
        //                ? tmp
        //                : CmpStrProp2(aa, bb);
        //}

        private static int HandCodedComposedFuncs(Target aa, Target bb)
        {
            foreach (var func in _composedSubFuncs)
            {
                int cmp = func(aa, bb);
                if (cmp != 0)
                    return cmp;
            }
            return 0;
        }

        private static int HandCodedTernary(Target xx, Target yy)
        {
            //return xx.IntProp1.CompareTo(yy.IntProp1);

            int tmp;

            return 0 != (tmp = xx.IntProp1.CompareTo(yy.IntProp1))
                ? tmp
                : 0 != (tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1))
                    ? tmp
                    : 0 != (tmp = xx.IntProp2.CompareTo(yy.IntProp2))
                        ? tmp
                        : CompareOrdinal(xx.StrProp2, yy.StrProp2);
        }

        //private static int HandCodedTernary(Target xx, Target yy)
        //{
        //    int tmp;

        //    return (tmp = xx.IntProp1.CompareTo(yy.IntProp1)) != 0
        //        ? tmp
        //        : (tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1)) != 0
        //            ? tmp
        //            : (tmp = xx.IntProp2.CompareTo(yy.IntProp2)) != 0
        //                ? tmp
        //                : CompareOrdinal(xx.StrProp2, yy.StrProp2);
        //}


        public bool CheckValidBenchmarks()
        {
            Setup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var hcSorted = _xs.OrderBy(tt => tt, _handCodedComparer).ToList();
            var genTernarySorted = _xs.OrderBy(m => m, _genTernComparer).ToList();
            var nitoSorted = _xs.OrderBy(m => m, _nitoComparer);
            var handCodedComposedFunctionsSorted = _xs.OrderBy(m => m, _handCodedComposedFunctionsComparer);
            var handCodedTernarySorted = _xs.OrderBy(m => m, _handCodedTernary).ToList();

            bool hcOk = referenceOrdering.SequenceEqual(hcSorted);
            bool genSortedOk = referenceOrdering.SequenceEqual(genSorted);
            bool genTernaryOk = referenceOrdering.SequenceEqual(genTernarySorted);
            bool nitoOk = referenceOrdering.SequenceEqual(nitoSorted);
            bool handCodedComposedFunctionsOk = referenceOrdering.SequenceEqual(handCodedComposedFunctionsSorted);
            bool handCodedTernaryOk = referenceOrdering.SequenceEqual(handCodedTernarySorted);

            //for (int ctr = 0; ctr < referenceOrdering.Count; ++ctr)
            //{
            //    var refTarget = referenceOrdering[ctr];
            //    var xxTarget = genTernarySorted[ctr];
            //    if (refTarget != xxTarget)
            //        Console.WriteLine($"failure at: {ctr}");
            //}

            return 
                hcOk && 
                genSortedOk && 
                genTernaryOk && 
                nitoOk && 
                handCodedComposedFunctionsOk && 
                handCodedTernaryOk;}

        [Benchmark]
        public void ComposedFunctionsListSort()
        {
            _xs.Sort(_handCodedComposedFunctionsComparer);
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

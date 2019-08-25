﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Nito.Comparers;
using SortFuncCommon;
using static System.String;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedMember.Local

namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class SortingBenchmarks
    {
        private List<Target> _xs;
        private readonly Consumer _consumer = new Consumer();

        private MyComparer<Target> _generatedComparer;
        private MyComparer<Target> _handCodedImperativeComparer;
        private MyComparer<Target> _generatedComparerFEC;
        private MyComparer<Target> _handCodedTernary;
        private MyComparer<Target> _emittedComparer;

        private IComparer<Target> _nitoComparer;
        private IComparer<Target> _handCodedComposedFunctionsComparer;

        private IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;

        private static readonly Func<Target, Target, int>[] _composedSubFuncs = {CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2};
        //private static readonly Func<Target, Target, int>[] _composedSubFuncs = {CmpIntProp1, CmpStrProp1};


        [IterationSetup]
        public void Setup()
        {
            var fs = new FileStream("targetData.data", FileMode.Open, FileAccess.Read);
            _xs = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);

            var sortBys = new List<SortBy>
            {
                new SortBy {PropName = "IntProp1", Ascending = true},
                new SortBy {PropName = "StrProp1", Ascending = true},
                new SortBy {PropName = "IntProp2", Ascending = true},
                new SortBy {PropName = "StrProp2", Ascending = true},
            };

            // lazy, evaluated in a benchmark and in the check-valid function
            _lazyLinqOrderByThenBy = _xs
                .OrderBy(x => x.IntProp1)
                .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
                .ThenBy(x => x.IntProp2)
                .ThenBy(x => x.StrProp2, StringComparer.Ordinal);

            _generatedComparer = new MyComparer<Target>(
                SortFuncCompiler.MakeSortFunc<Target>(sortBys)
            );

            _generatedComparerFEC = new MyComparer<Target>(
                SortFuncCompilerFEC.MakeSortFunc<Target>(sortBys)
            );

            _emittedComparer = new MyComparer<Target>( 
                MakeDynamic()
            );
            
            _handCodedImperativeComparer = new MyComparer<Target>(HandCoded);

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

        private static int HandCodedTernary2(Target xx, Target yy)
        {
            int tmp;
            return 0 != (tmp = xx.IntProp1.CompareTo(yy.IntProp1))
                ? tmp
                : CompareOrdinal(xx.StrProp1, yy.StrProp1);
        }


        private static int HandCodedTernary(Target xx, Target aa)
        {
            int tmp;

            return (tmp = xx.IntProp1.CompareTo(aa.IntProp1)) != 0
                ? tmp
                : (tmp = CompareOrdinal(xx.StrProp1, aa.StrProp1)) != 0
                    ? tmp
                    : (tmp = xx.IntProp2.CompareTo(aa.IntProp2)) != 0
                        ? tmp
                        : CompareOrdinal(xx.StrProp2, aa.StrProp2);
        }


        public static Func<Target, Target, int> MakeDynamic()
        {
            MethodInfo strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] {typeof(string), typeof(string)});
            MethodInfo intCompareTo = typeof(int).GetMethod("CompareTo", new[] {typeof(int)});

            MethodInfo getIntProp1 = typeof(Target).GetMethod("get_IntProp1", Type.EmptyTypes);
            MethodInfo getIntProp2 = typeof(Target).GetMethod("get_IntProp2", Type.EmptyTypes);
            MethodInfo getStrProp1 = typeof(Target).GetMethod("get_StrProp1", Type.EmptyTypes);
            MethodInfo getStrProp2 = typeof(Target).GetMethod("get_StrProp2", Type.EmptyTypes);

            Type returnType = typeof(int);
            Type[] methodParamTypes = {typeof(Target), typeof(Target)};

            //https://blogs.msdn.microsoft.com/jmstall/2005/02/03/debugging-dynamically-generated-code-reflection-emit/

            3.CompareTo(3);


            var method = new DynamicMethod(
                name: Empty,
                returnType: returnType,
                parameterTypes: methodParamTypes,
                owner: typeof(Program),
                skipVisibility: true);

            //var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("assembly"), AssemblyBuilderAccess.Run);
            //var dynamicModule = ab.DefineDynamicModule("module");
            //var method = new DynamicMethod(
            //    name: Empty,
            //    returnType: returnType,
            //    parameterTypes: methodParamTypes,
            //    m: dynamicModule);

            var il = method.GetILGenerator();

            /*LocalBuilder v_1 =*/ il.DeclareLocal(typeof(int));

            var label1 = il.DefineLabel(); 


            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getIntProp1);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca, 0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, getIntProp1);
            il.Emit(OpCodes.Call, intCompareTo);
            il.Emit(OpCodes.Dup); // Brtrue will pop the compare value off the top of the stack, but the value needs to be available to return if it is non-zero, so making a copy

            il.Emit(OpCodes.Brtrue_S, label1);
            il.Emit(OpCodes.Pop); // the first comparison is zero, and is no longer required

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getStrProp1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, getStrProp1);
            il.Emit(OpCodes.Call, strCompareOrdinal);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Brtrue_S, label1);
            il.Emit(OpCodes.Pop); // the second comparison is zero

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getIntProp2);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca, 0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, getIntProp2);
            il.Emit(OpCodes.Call, intCompareTo);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Brtrue_S, label1);
            il.Emit(OpCodes.Pop); // the third comparison is zero

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getStrProp2);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, getStrProp2);
            il.Emit(OpCodes.Call, strCompareOrdinal);
            
            il.MarkLabel(label1);
            il.Emit(OpCodes.Ret);

            return (Func<Target, Target, int>) method.CreateDelegate(typeof(Func<Target, Target, int>));
        }

        public bool CheckValidBenchmarks()
        {
            Setup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var hcSorted = _xs.OrderBy(tt => tt, _handCodedImperativeComparer).ToList();
            var genTernarySorted = _xs.OrderBy(m => m, _generatedComparerFEC).ToList();
            var nitoSorted = _xs.OrderBy(m => m, _nitoComparer);
            var handCodedComposedFunctionsSorted = _xs.OrderBy(m => m, _handCodedComposedFunctionsComparer);
            var handCodedTernarySorted = _xs.OrderBy(m => m, _handCodedTernary).ToList();

            var genEmitSorted = _xs.OrderBy(m => m, _emittedComparer).ToList();

            bool hcOk = referenceOrdering.SequenceEqual(hcSorted);
            bool genSortedOk = referenceOrdering.SequenceEqual(genSorted);
            bool genTernaryOk = referenceOrdering.SequenceEqual(genTernarySorted);
            bool nitoOk = referenceOrdering.SequenceEqual(nitoSorted);
            bool handCodedComposedFunctionsOk = referenceOrdering.SequenceEqual(handCodedComposedFunctionsSorted);
            bool handCodedTernaryOk = referenceOrdering.SequenceEqual(handCodedTernarySorted);

            bool emittedOk = referenceOrdering.SequenceEqual(genEmitSorted);

            for (int ctr = 0; ctr < referenceOrdering.Count; ++ctr)
            {
                var refTarget = referenceOrdering[ctr];
                var xxTarget = genEmitSorted[ctr];
                if (refTarget != xxTarget)
                    Console.WriteLine($"failure at: {ctr}");
            }

            return
                hcOk &&
                genSortedOk &&
                genTernaryOk &&
                nitoOk &&
                handCodedComposedFunctionsOk &&
                handCodedTernaryOk &&
                emittedOk;
        }

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
        public void GeneratedListSortFEC()
        {
            _xs.Sort(_generatedComparerFEC);
        }

        [Benchmark]
        public void EmittedListSort()
        {
            _xs.Sort(_emittedComparer);
        }

        [Benchmark]
        public void GeneratedListSort()
        {
            _xs.Sort(_generatedComparer);
        }

        [Benchmark]
        public void HandCodedImperativeListSort()
        {
            _xs.Sort(_handCodedImperativeComparer);
        }

        [Benchmark]
        public void HandCodedListSort()
        {
            _xs.Sort(_handCodedTernary);
        }

        [Benchmark]
        public void HandCodedOrderBy()
        {
            _xs.OrderBy(m => m, _handCodedImperativeComparer).Consume(_consumer);
        }

        [Benchmark]
        public void LinqOrderByThenBy()
        {
            _lazyLinqOrderByThenBy.Consume(_consumer);
        }
    }
}
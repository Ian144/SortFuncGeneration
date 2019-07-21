using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

// ReSharper disable PossibleMultipleEnumeration

namespace SortFuncGeneration
{
    public static class SortFuncCompilerTernary
    {
        private static readonly MethodInfo _strCompareTo = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });
        private static readonly MethodInfo _dateTimeCompareTo = typeof(DateTime).GetMethod("CompareTo", new[] { typeof(DateTime) });

        private static Expression MakePropertyCompareExpression(SortBy sortDescriptor, ParameterExpression rm1, ParameterExpression rm2)
        {
            try
            {
                MemberExpression propA = Expression.Property(rm1, sortDescriptor.PropName);
                MemberExpression propB = Expression.Property(rm2, sortDescriptor.PropName);
                var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);

                Expression compareExpr;

                if (prop1.Type == typeof(string))
                {
                    compareExpr = Expression.Call(_strCompareTo, prop1, prop2);
                }
                else if (prop1.Type == typeof(int))
                {
                    compareExpr = Expression.Call(prop1, _intCompareTo, prop2);
                }
                else if (prop1.Type == typeof(DateTime))
                {
                    compareExpr = Expression.Call(prop1, _dateTimeCompareTo, prop2);
                }
                else
                {
                    throw new ApplicationException($"unsupported property type: {prop1.Type}");
                }

                return compareExpr;

            }
            catch
            {
                throw new ApplicationException($"unknown property: {sortDescriptor.PropName}");
            }
        }

        private static Expression MakeSortExpression<T>(IEnumerable<SortBy> sortDescriptors, ParameterExpression param1Expr, ParameterExpression param2Expr)
        {

            if (sortDescriptors.Count() == 1)
            {
                return MakePropertyCompareExpression(sortDescriptors.First(), param1Expr, param2Expr);
            }
            
            Expression compare = MakePropertyCompareExpression(sortDescriptors.First(), param1Expr, param2Expr);

            //var variableExpr = Expression.Variable(typeof(int), "comp1result");
            //var assignExpr = Expression.Assign(variableExpr, compare); // does this require a block? thereby making it the same as ?; ternary

            // could i write my own NotZeroThen expression? might not be able to write an operrator, but could do this

            var condExpr =
                Expression.Condition(
                    Expression.NotEqual(Expression.Constant(0), compare),
                    compare,
                    MakeSortExpression<T>(sortDescriptors.Skip(1), param1Expr, param2Expr)
                );

            return condExpr;
        }

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortDescriptors)
        {
            ParameterExpression param1Expr = Expression.Parameter(typeof(T));
            ParameterExpression param2Expr = Expression.Parameter(typeof(T));
            Expression compositeCompare = MakeSortExpression<T>(sortDescriptors, param1Expr, param2Expr);
            Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(compositeCompare, param1Expr, param2Expr);
            return lambda.CompileFast();
        }

    }
}

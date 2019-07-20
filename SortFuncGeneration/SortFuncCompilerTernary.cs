using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SortFuncGeneration
{
    public static class SortFuncCompilerTernary
    {
        private static readonly MethodInfo _strCompareTo = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

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

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortDescriptors)
        {
            ParameterExpression param1Expr = Expression.Parameter(typeof(T));
            ParameterExpression param2Expr = Expression.Parameter(typeof(T));
            //BlockExpression exprSd = MakeCompositeCompare(param1Expr, param2Expr, sortDescriptors);
            //Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(exprSd, param1Expr, param2Expr);
            //return lambda.Compile();

            if (sortDescriptors.Count == 1)
            {
                Expression compare = MakePropertyCompareExpression(sortDescriptors.First(), param1Expr, param2Expr);
                Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(compare, param1Expr, param2Expr);
                return lambda.Compile();
            }


            if (sortDescriptors.Count == 2)
            {
                Expression compare = MakePropertyCompareExpression(sortDescriptors.First(), param1Expr, param2Expr);
                Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(compare, param1Expr, param2Expr);
                return lambda.Compile();
            }


            return null;
        }
    }
}

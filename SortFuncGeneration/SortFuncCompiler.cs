﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SortFuncGeneration
{
    public class SortBy
    {
        public bool Ascending { get; set; }
        public string PropName { get; set; }
    }

    public static class SortFuncCompiler
    {
        private static readonly MethodInfo _strCompareTo = typeof(string).GetMethod("CompareTo", new[] { typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortDescriptors)
        {
            ParameterExpression param1Expr = Expression.Parameter(typeof(T));
            ParameterExpression param2Expr = Expression.Parameter(typeof(T));
            BlockExpression exprSd = MakeCompositeCompare(param1Expr, param2Expr, sortDescriptors);
            Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(exprSd, param1Expr, param2Expr);
            return lambda.Compile();
        }

        private static BlockExpression MakePropertyCompareBlock(
            SortBy sortDescriptor,
            ParameterExpression rm1,
            ParameterExpression rm2,
            LabelTarget labelReturn,
            ParameterExpression result)
        {
   
            try
            {
                MemberExpression propA = Expression.Property(rm1, sortDescriptor.PropName);
                MemberExpression propB = Expression.Property(rm2, sortDescriptor.PropName);
                var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);

                Expression compareExpr;

                if (prop1.Type == typeof(string))
                {
                    compareExpr = Expression.Call(prop1, _strCompareTo, prop2);
                }
                else if (prop1.Type == typeof(int))
                {
                    compareExpr = Expression.Call(prop1, _intCompareTo, prop2);
                }
                else
                {
                    throw new ApplicationException($"unsupported property type: {prop1.Type}");
                }

                IEnumerable<ParameterExpression> variables = new[] { result };

                IEnumerable<Expression> expressions = new Expression[]
                {
                Expression.Assign(result, compareExpr),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Constant(0), result),
                    Expression.Goto(labelReturn, result))
                };

                return Expression.Block(variables, expressions);
            }
            catch
            {
                throw new ApplicationException($"unknown property: {sortDescriptor.PropName}");
            }
        }

        private static BlockExpression MakeCompositeCompare(ParameterExpression param1Expr, ParameterExpression param2Expr, IEnumerable<SortBy> sortBys)
        {
            ParameterExpression result = Expression.Variable(typeof(int), "result");
            LabelTarget labelReturn = Expression.Label(typeof(int));
            LabelExpression labelExpression = Expression.Label(labelReturn, result);
            IEnumerable<Expression> compareBlocks = sortBys.Select(propName => MakePropertyCompareBlock(propName, param1Expr, param2Expr, labelReturn, result));
            return Expression.Block(new[] { result }, compareBlocks.Append(labelExpression));
        }
    }
}

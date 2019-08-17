using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using FastExpressionCompiler;
using static System.Linq.Expressions.Expression;

// ReSharper disable PossibleMultipleEnumeration

namespace SortFuncGeneration
{
    public static class SortFuncCompilerTernary
    {
        private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });
        private static readonly ConstantExpression _zeroExpr = Constant(0);

        //private static readonly Expression<Func<string, string, int>> _exprStrComp = (s1, s2) => string.CompareOrdinal(s1, s2);
        //private static readonly Expression<Func<int, int, int>> _exprIntComp = (aa, bb) => aa.CompareTo(bb);

        //private static readonly MethodInfo _objectEqualsMethod = ((Func<int, int, int>)int.CompareTo).Method;


        //public static TDelegate TryCompileWithoutClosure<TDelegate>(this LambdaExpression lambdaExpr)
        //    where TDelegate : class
        //{
        //    var closureInfo = new ClosureInfo(true);
        //    var paramTypes = Tools.GetParamTypes(lambdaExpr.Parameters);

        //    var method = new DynamicMethod(
        //        string.Empty,
        //        lambdaExpr.ReturnType,
        //        paramTypes,
        //        typeof(SortFuncCompilerTernary), skipVisibility: true);

        //    var il = method.GetILGenerator();
        //    var parentFlags = lambdaExpr.ReturnType == typeof(void) ? ParentFlags.IgnoreResult : ParentFlags.Empty;
        //    if (!EmittingVisitor.TryEmit(lambdaExpr.Body, lambdaExpr.Parameters, il, ref closureInfo, parentFlags))
        //        return null;
        //    il.Emit(OpCodes.Ret);

        //    var delegateType = typeof(TDelegate) != typeof(Delegate) ? typeof(TDelegate) : Tools.GetFuncOrActionType(paramTypes, lambdaExpr.ReturnType);
        //    return (TDelegate)(object)method.CreateDelegate(delegateType);
        //}


        private static Expression MakePropertyCompareExpressionCall(SortBy sortDescriptor, ParameterExpression rm1, ParameterExpression rm2)
        {
            try
            {
                MemberExpression propA = Property(rm1, sortDescriptor.PropName);
                MemberExpression propB = Property(rm2, sortDescriptor.PropName);
                var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);

                Expression compareExpr;

                if (prop1.Type == typeof(string))
                {
                    compareExpr = Call(_strCompareOrdinal, prop1, prop2);

                }
                else if (prop1.Type == typeof(int))
                {
                    compareExpr = Call(prop1, _intCompareTo, prop2);
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




        private static Expression MakeSortExpression<T>(IEnumerable<SortBy> sortDescriptors, ParameterExpression param1Expr, ParameterExpression param2Expr, ParameterExpression tmpInt)
        {
            if (sortDescriptors.Count() == 1)
            {
                return MakePropertyCompareExpressionCall(sortDescriptors.First(), param1Expr, param2Expr);
            }
            
            Expression compare = MakePropertyCompareExpressionCall(sortDescriptors.First(), param1Expr, param2Expr);

            return Condition(
                NotEqual(Assign(tmpInt, compare), _zeroExpr), // perform the comparison and assign the value to tmpInt, assignments are expressions and have a value
                tmpInt,
                MakeSortExpression<T>(sortDescriptors.Skip(1), param1Expr, param2Expr, tmpInt)
            );
        }

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortDescriptors)
        {
            ParameterExpression param1Expr = Parameter(typeof(T));
            ParameterExpression param2Expr = Parameter(typeof(T));
            ParameterExpression tmpInt = Variable(typeof(int), "tmp");

            Expression compositeCompare = MakeSortExpression<T>(sortDescriptors, param1Expr, param2Expr, tmpInt);

            ParameterExpression[] variables = { tmpInt };
            Expression[] body = { compositeCompare };
            var block = Block(variables, body);

            Expression<Func<T, T, int>> lambda = Lambda<Func<T, T, int>>(block, param1Expr, param2Expr);

            //return lambda.Compile();
            //return lambda.CompileFast(true);
            return lambda.TryCompileWithoutClosure<Func<T, T, int>>();
            //return lambda.TryCompile<Func<T, T, int>>();
            //return lambda.TryCompileWithPreCreatedClosure<Func<T, T, int>>();
        }
    }
}


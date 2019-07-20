using System;
using System.Collections.Generic;

namespace SortFuncGeneration
{
    public class MyComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _sortFunc;

        public MyComparer(Func<T, T, int> sortFunc)
        {
            _sortFunc = sortFunc;
        }

        public int Compare(T x, T y) => _sortFunc(x, y);
    }
}
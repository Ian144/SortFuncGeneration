using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class MyClass1
    {
        public int IntProp1 { get; set; }
        public int IntProp2 { get; set; }
        public string StrProp1 { get; set; }
        public string StrProp2 { get; set; }

        public override string ToString()
        {
            return $"{IntProp1} - {IntProp2} - {StrProp1} - {StrProp2}";
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var xs = new List<MyClass1>
            {
                new MyClass1{IntProp1 = 99, IntProp2 = 88, StrProp1 = "aa", StrProp2 ="bb"},
                new MyClass1{IntProp1 = 11, IntProp2 = 22, StrProp1 = "pp", StrProp2 ="qq"},
                new MyClass1{IntProp1 = 11, IntProp2 = 22, StrProp1 = "xx", StrProp2 ="yy"},
            };

            var sortBys = new List<SortBy>
            {
                new SortBy{PropName = "IntProp2", Ascending = true},
                new SortBy{PropName = "StrProp1", Ascending = false}
            };

            Func<MyClass1, MyClass1, int> sortMyClass = SortFuncCompiler.MakeSortFunc<MyClass1>(sortBys);

            var ys = xs.OrderBy(x => x, new MyComparer<MyClass1>(sortMyClass));

            foreach (var mc in ys)
            {
                Console.WriteLine(mc);    
            }
        }
    }
}

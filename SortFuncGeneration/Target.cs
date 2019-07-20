// ReSharper disable MemberCanBePrivate.Global

namespace SortFuncGeneration
{
    public class Target
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
}
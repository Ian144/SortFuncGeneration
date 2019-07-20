// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Linq;
using FsCheck;
using ProtoBuf;

namespace SortFuncGeneration
{
    [ProtoContract]
    public class Target
    {
        [ProtoMember(1)]
        public int IntProp1 { get; set; }

        [ProtoMember(2)]
        public int IntProp2 { get; set; }

        [ProtoMember(3)]
        public string StrProp1 { get; set; }

        [ProtoMember(4)]
        public string StrProp2 { get; set; }

        public override string ToString()
        {
            //return $"{IntProp1} : {IntProp2} : {StrProp1} : {StrProp2}";
            return $"{StrProp1}";
        }
    }
    

    public class TargetEx
    {
        private readonly Gen<string> genNonEmptyAlphaString =
            from ss in Arb.Generate<string>()
            where !string.IsNullOrEmpty(ss)
            where ss.ToCharArray().All(char.IsLetter)
            where ss.Length > 1
            select ss;


        public TargetEx(int int1, int int2, Guid g1, Guid g2)
        {
            IntProp1 = int1;
            IntProp2 = int2;
            //StrProp1 = neStr1.Get.ToUpper();
            //StrProp2 = neStr2.Get.ToUpper();
            StrProp1 = g1.ToString();
            StrProp2 = g2.ToString();
        }

        public int IntProp1 { get; }
        public int IntProp2 { get; }
        public string StrProp1 { get; }
        public string StrProp2 { get; }
    }
}
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Linq;
using FsCheck;
using ProtoBuf;
// ReSharper disable ClassNeverInstantiated.Global

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
            return $"{IntProp1} : {IntProp2} : {StrProp1} : {StrProp2}";
        }
    }

    public class TargetEx
    {
        public TargetEx(int int1, int int2, NonEmptyString nes1, NonEmptyString nes2)
        {
            IntProp1 = int1;
            IntProp2 = int2;
            StrProp1 = nes1.Get;
            StrProp2 = nes2.Get;
        }

        public int IntProp1 { get; }
        public int IntProp2 { get; }
        public string StrProp1 { get; }
        public string StrProp2 { get; }
    }
}
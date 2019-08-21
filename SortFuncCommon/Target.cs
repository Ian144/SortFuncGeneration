

using System;
using FsCheck;
using ProtoBuf;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global used implicitly by fscheck
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SortFuncCommon
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
            return $"{IntProp1} : {StrProp1}";
        }
    }

    public class TargetBuilder
    {
        public TargetBuilder(int int1, int int2, Guid nes1, Guid nes2)
        {
            IntProp1 = int1;
            IntProp2 = int2;
            StrProp1 = nes1.ToString().Substring(0, 12);
            StrProp2 = nes2.ToString().Substring(0, 12);
        }

        public int IntProp1 { get; }
        public int IntProp2 { get; }
        public string StrProp1 { get; }
        public string StrProp2 { get; }
    }
}
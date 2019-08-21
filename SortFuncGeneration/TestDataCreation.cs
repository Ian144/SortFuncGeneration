using System.IO;
using System.Linq;
using FsCheck;
using SortFuncCommon;

namespace SortFuncGeneration
{
    public static class TestDataCreation
    {
        public static void CreateAndPersistData(int size){
            var rawItems = Arb.From<TargetBuilder>().Generator.Sample(size);
            var targets = rawItems.Select(tb => new Target{IntProp1 = tb.IntProp1, IntProp2 = tb.IntProp2, StrProp1 = tb.StrProp1, StrProp2 = tb.StrProp2});

            using (var fs = new FileStream("targetData.data", FileMode.Create)){
                ProtoBuf.Serializer.Serialize(fs, targets);
            }
        }
    }
}

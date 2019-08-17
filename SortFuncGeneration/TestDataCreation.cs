using System.IO;
using FsCheck;
using SortFuncCommon;

namespace SortFuncGeneration
{
    public static class TestDataCreation
    {
        public static void CreateAndPersistData(int size){
            var rawItems = Arb.From<Target>().Generator.Sample(size);
            using(var fs = new FileStream("targetData.data",FileMode.Create)) {
                ProtoBuf.Serializer.Serialize(fs, rawItems);
            }
        }
    }
}

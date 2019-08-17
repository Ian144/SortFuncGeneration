using System.IO;
using System.Linq;
using FsCheck;
using Microsoft.IO;

namespace SortFuncGeneration
{
    public static class TestDataCreation
    {
        public static void CreateAndPersistData(int size){
            var arb = Arb.From<TargetEx>();
            TargetEx[] rawItems = arb.Generator.Sample(size);
            var items = rawItems.Select( te => new Target{ IntProp1=te.IntProp1, IntProp2=te.IntProp2, StrProp1=te.StrProp1, StrProp2=te.StrProp2} );
            using(var fs = new FileStream("targetData.data",FileMode.Create)) {
                ProtoBuf.Serializer.Serialize(fs, items);
            }
        }
    }
}

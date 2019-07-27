using System.IO;
using System.Linq;
using FsCheck;

namespace SortFuncGeneration
{
    public static class TestDataCreation
    {
        public static void CreateAndPersistData()
        {
            var arb = Arb.From<TargetEx>();

            TargetEx[] rawItems = arb.Generator.Sample(1000000);

            Target[] items = rawItems.Select( te => new Target{ IntProp1  = te.IntProp1, IntProp2 = te.IntProp2, StrProp1 = te.StrProp1, StrProp2 = te.StrProp2} ).ToArray();

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, items);
                var bs = ms.ToArray();
                File.WriteAllBytes("targetData.data", bs);
            }
        }
    }
}

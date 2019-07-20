using System.IO;
using System.Linq;
using FsCheck;

namespace SortFuncGeneration
{
    public static class TestDataCreation
    {
        public static void CreateAndPersistData()
        {
            var arb = Arb.From<Target>();

            Target[] items = arb.Generator.Sample(100000);

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, items);
                var tmp = ProtoBuf.Serializer.Deserialize<Target[]>(ms);
                var bs = ms.ToArray();
                File.WriteAllBytes("targetData.data", bs);
            }
        }
    }
}

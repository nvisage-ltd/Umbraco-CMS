using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Exceptions;

namespace Umbraco.Cms.Infrastructure.PublishedCache.DataSource;

public class BTree
{
    public static BPlusTree<int, ContentNodeKit> GetTree(string filepath, bool exists, NuCacheSettings settings, ContentDataSerializer? contentDataSerializer = null)
    {
        var keySerializer = new PrimitiveSerializer();
        var valueSerializer = new ContentNodeKitSerializer(contentDataSerializer);
        var options = new BPlusTree<int, ContentNodeKit>.OptionsV2(keySerializer, valueSerializer)
        {
            CreateFile = exists ? CreatePolicy.IfNeeded : CreatePolicy.Always,
            FileName = filepath,

            // read or write but do *not* keep in memory
            CachePolicy = CachePolicy.None,

            // default is 4096, min 2^9 = 512, max 2^16 = 64K
            FileBlockSize = GetBlockSize(settings),

            // Since .NET 5, we can use the fastest cross-platform Storage Performance Mode.
            // This is safer but a bit slower then what was used in Umbraco 8 (LogFileInCache (Windows only)).
            // But much faster than what was used in Umbraco 9 (CommitToDisk), Especially on Linux for some reason..
            StoragePerformance = StoragePerformance.CommitToCache,

            // other options?
        };

        var tree = new BPlusTree<int, ContentNodeKit>(options);

        // anything?
        // btree.
        return tree;
    }

    private static int GetBlockSize(NuCacheSettings settings)
    {
        var blockSize = 4096;

        var appSetting = settings.BTreeBlockSize;
        if (!appSetting.HasValue)
        {
            return blockSize;
        }

        blockSize = appSetting.Value;

        var bit = 0;
        for (var i = blockSize; i != 1; i >>= 1)
        {
            bit++;
        }

        if (1 << bit != blockSize)
        {
            throw new ConfigurationException($"Invalid block size value \"{blockSize}\": must be a power of two.");
        }

        if (blockSize < 512 || blockSize > 65536)
        {
            throw new ConfigurationException($"Invalid block size value \"{blockSize}\": must be >= 512 and <= 65536.");
        }

        return blockSize;
    }

    /*
    class ListOfIntSerializer : ISerializer<List<int>>
    {
        public List<int> ReadFrom(Stream stream)
        {
            var list = new List<int>();
            var count = PrimitiveSerializer.Int32.ReadFrom(stream);
            for (var i = 0; i < count; i++)
                list.Add(PrimitiveSerializer.Int32.ReadFrom(stream));
            return list;
        }

        public void WriteTo(List<int> value, Stream stream)
        {
            PrimitiveSerializer.Int32.WriteTo(value.Count, stream);
            foreach (var item in value)
                PrimitiveSerializer.Int32.WriteTo(item, stream);
        }
    }
    */
}

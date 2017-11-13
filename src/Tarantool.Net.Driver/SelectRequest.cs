using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public struct SelectRequest<TKey>
    {
        public SelectRequest(uint spaceId, uint indexId, uint limit, uint offset, IteratorType iterator, TKey key)
        {
            SpaceId = spaceId;
            IndexId = indexId;
            Limit = limit;
            Offset = offset;
            Iterator = iterator;
            Key = key;
        }

        [MapKey(Driver.Key.SpaceId)]
        public uint SpaceId { get; }

        [MapKey(Driver.Key.IndexId)]
        public uint IndexId { get; }

        [MapKey(Driver.Key.Limit)]
        public uint Limit { get; }

        [MapKey(Driver.Key.Offset)]
        public uint Offset { get; }

        [MapKey(Driver.Key.Iterator)]
        public IteratorType Iterator { get; }

        [MapKey(Driver.Key.Key)]
        public TKey Key { get; }
    }
}

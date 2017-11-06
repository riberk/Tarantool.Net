namespace Tarantool.Net.Driver
{
    public struct SelectRequest<TKey>  : ISelectRequest<TKey>
    {
        public SelectRequest(uint spaceId, uint indexId, uint? limit, uint? offset, IteratorType iterator, TKey key)
        {
            SpaceId = spaceId;
            IndexId = indexId;
            Limit = limit;
            Offset = offset;
            Iterator = iterator;
            Key = key;
        }

        public uint SpaceId { get; }
        public uint IndexId { get; }
        public uint? Limit { get; }
        public uint? Offset { get; }
        public IteratorType Iterator { get; }
        public TKey Key { get; }
    }
}

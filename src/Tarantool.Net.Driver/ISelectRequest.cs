namespace Tarantool.Net.Driver
{
    public interface ISelectRequest<out TKey>
    {
        uint SpaceId { get; }

        uint IndexId { get; }

        uint? Limit { get; }

        uint? Offset { get; }

        IteratorType Iterator { get; }

        TKey Key { get; }
    }
}

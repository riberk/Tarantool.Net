namespace Tarantool.Net.Abstractions
{
    public struct Header
    {
        public Header(RequestType requestType, ulong sync) : this(requestType, sync, null)
        {
        }

        public Header(RequestType requestType, ulong sync, uint? schemaId)
        {
            RequestType = requestType;
            Sync = sync;
            SchemaId = schemaId;
            ErrorCode = (RequestType & RequestType.TypeError) == RequestType.TypeError
                            ? (ErrorCode) (RequestType ^ RequestType.TypeError)
                            : (ErrorCode?) null;
        }

        public RequestType RequestType { get; }

        public ulong Sync { get; }

        public uint? SchemaId { get; }

        public ErrorCode? ErrorCode { get; }

        public bool HasError => ErrorCode.HasValue;
    }
}

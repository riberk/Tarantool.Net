using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class Header
    {
        public Header()
        {
        }

        public Header(RequestType requestType, ulong sync, uint? schemaVersion)
        {
            RequestType = requestType;
            Sync = sync;
            SchemaVersion = schemaVersion;
        }

        [MapKey(Key.RequestType)]
        public RequestType RequestType { get; set; }

        [MapKey(Key.Sync)]
        public ulong Sync { get; set; }

        [MapKey(Key.SchemaVersion)]
        public uint? SchemaVersion { get; set; }

        public ErrorCode? ErrorCode => (RequestType & RequestType.TypeError) == RequestType.TypeError
                                           ? (ErrorCode)(RequestType ^ RequestType.TypeError)
                                           : (ErrorCode?)null;

        public bool HasError => ErrorCode.HasValue;
    }
}

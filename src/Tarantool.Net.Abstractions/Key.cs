namespace Tarantool.Net.Abstractions
{
    public enum Key
    {
        RequestType = 0x00,
        Sync = 0x01,

        /* Replication keys (header) */
        ReplicaId = 0x02,
        Lsn = 0x03,
        Timestamp = 0x04,
        SchemaVersion = 0x05,
        ServerVersion = 0x06,

        /* Leave a gap for other keys in the header. */
        SpaceId = 0x10,
        IndexId = 0x11,
        Limit = 0x12,
        Offset = 0x13,
        Iterator = 0x14,
        IndexBase = 0x15,

        /* Leave a gap between integer values and other keys */
        Key = 0x20,
        Tuple = 0x21,
        FunctionName = 0x22,
        UserName = 0x23,

        /* Replication keys (body) */
        InstanceUuid = 0x24,
        ClusterUuid = 0x25,
        Vclock = 0x26,
        Expr = 0x27, /* EVAL */
        Ops = 0x28, /* UPSERT but not UPDATE ops, because of legacy */

        /* Leave a gap between request keys and response keys */
        Data = 0x30,
        Error = 0x31,
        KeyMax
    }
}

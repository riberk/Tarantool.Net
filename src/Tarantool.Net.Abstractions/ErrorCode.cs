namespace Tarantool.Net.Abstractions
{
    public enum ErrorCode
    {
        /// <summary>Unknown error</summary>
        Unknown = 0,

        /// <summary>Illegal parameters, %s</summary>
        IllegalParams = 1,

        /// <summary>Failed to allocate %u bytes in %s for %s</summary>
        MemoryIssue = 2,

        /// <summary>Duplicate key exists in unique index '%s' in space '%s'</summary>
        TupleFound = 3,

        /// <summary>Tuple doesn't exist in index '%s' in space '%s'</summary>
        TupleNotFound = 4,

        /// <summary>%s does not support %s</summary>
        Unsupported = 5,

        /// <summary>Can't modify data on a replication slave. My master is: %s</summary>
        Nonmaster = 6,

        /// <summary>Can't modify data because this instance is in read-only mode.</summary>
        Readonly = 7,

        /// <summary>Error injection '%s'</summary>
        Injection = 8,

        /// <summary>Failed to create space '%s': %s</summary>
        CreateSpace = 9,

        /// <summary>Space '%s' already exists</summary>
        SpaceExists = 10,

        /// <summary>Can't drop space '%s': %s</summary>
        DropSpace = 11,

        /// <summary>Can't modify space '%s': %s</summary>
        AlterSpace = 12,

        /// <summary>Unsupported index type supplied for index '%s' in space '%s'</summary>
        IndexType = 13,

        /// <summary>Can't create or modify index '%s' in space '%s': %s</summary>
        ModifyIndex = 14,

        /// <summary>Can't drop the primary key in a system space, space '%s'</summary>
        LastDrop = 15,

        /// <summary>Tuple format limit reached: %u</summary>
        TupleFormatLimit = 16,

        /// <summary>Can't drop primary key in space '%s' while secondary keys exist</summary>
        DropPrimaryKey = 17,

        /// <summary>Supplied key type of part %u does not match index part type: expected %s</summary>
        KeyPartType = 18,

        /// <summary>Invalid key part count in an exact match (expected %u, got %u)</summary>
        ExactMatch = 19,

        /// <summary>Invalid MsgPack - %s</summary>
        InvalidMsgpack = 20,

        /// <summary>msgpack.encode: can not encode Lua type '%s'</summary>
        ProcRet = 21,

        /// <summary>Tuple/Key must be MsgPack array</summary>
        TupleNotArray = 22,

        /// <summary>Tuple field %u type does not match one required by operation: expected %s</summary>
        FieldType = 23,

        /// <summary>Ambiguous field type, field %u. Requested type is %s but the field has previously been defined as %s</summary>
        FieldTypeMismatch = 24,

        /// <summary>SPLICE error on field %u: %s</summary>
        Splice = 25,

        /// <summary>Argument type in operation '%c' on field %u does not match field type: expected %s</summary>
        UpdateArgType = 26,

        /// <summary>Tuple is too long %u</summary>
        TupleIsTooLong = 27,

        /// <summary>Unknown UPDATE operation</summary>
        UnknownUpdateOp = 28,

        /// <summary>Field %u UPDATE error: %s</summary>
        UpdateField = 29,

        /// <summary>Can not create a new fiber: recursion limit reached</summary>
        FiberStack = 30,

        /// <summary>Invalid key part count (expected [0..%u], got %u)</summary>
        KeyPartCount = 31,

        /// <summary>%s</summary>
        ProcLua = 32,

        /// <summary>Procedure '%.*s' is not defined</summary>
        NoSuchProc = 33,

        /// <summary>Trigger is not found</summary>
        NoSuchTrigger = 34,

        /// <summary>No index #%u is defined in space '%s'</summary>
        NoSuchIndex = 35,

        /// <summary>Space '%s' does not exist</summary>
        NoSuchSpace = 36,

        /// <summary>Field %d was not found in the tuple</summary>
        NoSuchField = 37,

        /// <summary>Tuple field count %u does not match space field count %u</summary>
        ExactFieldCount = 38,

        /// <summary>Tuple field count %u is less than required (expected at least %u)</summary>
        IndexFieldCount = 39,

        /// <summary>Failed to write to disk</summary>
        WalIo = 40,

        /// <summary>Get() doesn't support partial keys and non-unique indexes</summary>
        MoreThanOneTuple = 41,

        /// <summary>%s access on %s is denied for user '%s'</summary>
        AccessDenied = 42,

        /// <summary>Failed to create user '%s': %s</summary>
        CreateUser = 43,

        /// <summary>Failed to drop user or role '%s': %s</summary>
        DropUser = 44,

        /// <summary>User '%s' is not found</summary>
        NoSuchUser = 45,

        /// <summary>User '%s' already exists</summary>
        UserExists = 46,

        /// <summary>Incorrect password supplied for user '%s'</summary>
        PasswordMismatch = 47,

        /// <summary>Unknown request type %u</summary>
        UnknownRequestType = 48,

        /// <summary>Unknown object type '%s'</summary>
        UnknownSchemaObject = 49,

        /// <summary>Failed to create function '%s': %s</summary>
        CreateFunction = 50,

        /// <summary>Function '%s' does not exist</summary>
        NoSuchFunction = 51,

        /// <summary>Function '%s' already exists</summary>
        FunctionExists = 52,

        /// <summary>%s access is denied for user '%s' to function '%s'</summary>
        FunctionAccessDenied = 53,

        /// <summary>A limit on the total number of functions has been reached: %u</summary>
        FunctionMax = 54,

        /// <summary>%s access is denied for user '%s' to space '%s'</summary>
        SpaceAccessDenied = 55,

        /// <summary>A limit on the total number of users has been reached: %u</summary>
        UserMax = 56,

        /// <summary>Space engine '%s' does not exist</summary>
        NoSuchEngine = 57,

        /// <summary>Can't set option '%s' dynamically</summary>
        ReloadCfg = 58,

        /// <summary>Incorrect value for option '%s': %s</summary>
        Cfg = 59,

        /// <summary>Can not set a savepoint in an empty transaction</summary>
        SavepointEmptyTx = 60,

        /// <summary>Can not rollback to savepoint: the savepoint does not exist</summary>
        NoSuchSavepoint = 61,

        /// <summary>Replica %s is not registered with replica set %s</summary>
        UnknownReplica = 62,

        /// <summary>Replica set UUID of the replica %s doesn't match replica set UUID of the master %s</summary>
        ReplicasetUuidMismatch = 63,

        /// <summary>Invalid UUID: %s</summary>
        InvalidUuid = 64,

        /// <summary>Can't reset replica set UUID: it is already assigned</summary>
        ReplicasetUuidIsRo = 65,

        /// <summary>Remote ID mismatch for %s: expected %u, got %u</summary>
        InstanceUuidMismatch = 66,

        /// <summary>Can't initialize replica id with a reserved value %u</summary>
        ReplicaIdIsReserved = 67,

        /// <summary>Invalid LSN order for instance %u: previous LSN = %llu, new lsn = %llu</summary>
        InvalidOrder = 68,

        /// <summary>Missing mandatory field '%s' in request</summary>
        MissingRequestField = 69,

        /// <summary>Invalid identifier '%s' (expected letters, digits or an underscore)</summary>
        Identifier = 70,

        /// <summary>Can't drop function %u: %s</summary>
        DropFunction = 71,

        /// <summary>Unknown iterator type '%s'</summary>
        IteratorType = 72,

        /// <summary>Replica count limit reached: %u</summary>
        ReplicaMax = 73,

        /// <summary>Failed to read xlog: %lld</summary>
        InvalidXlog = 74,

        /// <summary>Invalid xlog name: expected %lld got %lld</summary>
        InvalidXlogName = 75,

        /// <summary>Invalid xlog order: %lld and %lld</summary>
        InvalidXlogOrder = 76,

        /// <summary>Connection is not established</summary>
        NoConnection = 77,

        /// <summary>Timeout exceeded</summary>
        Timeout = 78,

        /// <summary>Operation is not permitted when there is an active transaction </summary>
        ActiveTransaction = 79,

        /// <summary>The transaction the cursor belongs to has ended</summary>
        CursorNoTransaction = 80,

        /// <summary>A multi-statement transaction can not use multiple storage engines</summary>
        CrossEngineTransaction = 81,

        /// <summary>Role '%s' is not found</summary>
        NoSuchRole = 82,

        /// <summary>Role '%s' already exists</summary>
        RoleExists = 83,

        /// <summary>Failed to create role '%s': %s</summary>
        CreateRole = 84,

        /// <summary>Index '%s' already exists</summary>
        IndexExists = 85,

        /// <summary>Tuple reference counter overflow</summary>
        TupleRefOverflow = 86,

        /// <summary>Granting role '%s' to role '%s' would create a loop</summary>
        RoleLoop = 87,

        /// <summary>Incorrect grant arguments: %s</summary>
        Grant = 88,

        /// <summary>User '%s' already has %s access on %s '%s'</summary>
        PrivGranted = 89,

        /// <summary>User '%s' already has role '%s'</summary>
        RoleGranted = 90,

        /// <summary>User '%s' does not have %s access on %s '%s'</summary>
        PrivNotGranted = 91,

        /// <summary>User '%s' does not have role '%s'</summary>
        RoleNotGranted = 92,

        /// <summary>Can't find snapshot</summary>
        MissingSnapshot = 93,

        /// <summary>Attempt to modify a tuple field which is part of index '%s' in space '%s'</summary>
        CantUpdatePrimaryKey = 94,

        /// <summary>Integer overflow when performing '%c' operation on field %u</summary>
        UpdateIntegerOverflow = 95,

        /// <summary>Setting password for guest user has no effect</summary>
        GuestUserPassword = 96,

        /// <summary>      Transaction has been aborted by conflict</summary>
        TransactionConflict = 97,

        /// <summary>     Unsupported role privilege '%s'</summary>
        UnsupportedRolePriv = 98,

        /// <summary>Failed to dynamically load function '%s': %s</summary>
        LoadFunction = 99,

        /// <summary>Unsupported language '%s' specified for function '%s'</summary>
        FunctionLanguage = 100,

        /// <summary>RTree: %s must be an array with %u (point) or %u (rectangle/box) numeric coordinates</summary>
        RtreeRect = 101,

        /// <summary>%s</summary>
        ProcC = 102,

        /// <summary>Unknown RTREE index distance type %s</summary>
        UnknownRtreeIndexDistanceType = 103,

        /// <summary>%s</summary>
        Protocol = 104,

        /// <summary> Space %s has a unique secondary index and does not support UPSERT</summary>
        UpsertUniqueSecondaryKey = 105,

        /// <summary>Wrong record in _index space: got {%s}, expected {%s}</summary>
        WrongIndexRecord = 106,

        /// <summary>Wrong index parts: %s; expected field1 id (number), field1 type (string), ...</summary>
        WrongIndexParts = 107,

        /// <summary>Wrong index options (field %u): %s</summary>
        WrongIndexOptions = 108,

        /// <summary>Wrong schema version, current: %d, in request: %u</summary>
        WrongSchemaVersion = 109,

        /// <summary>Failed to allocate %u bytes for tuple: tuple is too large. Check 'memtx_max_tuple_size' configuration option.</summary>
        MemtxMaxTupleSize = 110,

        /// <summary>Wrong space options (field %u): %s</summary>
        WrongSpaceOptions = 111,

        /// <summary>Index '%s' (%s) of space '%s' (%s) does not support %s</summary>
        UnsupportedIndexFeature = 112,

        /// <summary>View '%s' is read-only</summary>
        ViewIsRo = 113,

        /// <summary>Can not set a savepoint in absence of active transaction</summary>
        SavepointNoTransaction = 114,

        /// <summary>%s</summary>
        System = 115,

        /// <summary>Instance bootstrap hasn't finished yet</summary>
        Loading = 116,

        /// <summary>Connection to self</summary>
        ConnectionToSelf = 117,

        /// <summary>Key part is too long: %u of %u bytes</summary>
        KeyPartIsTooLong = 118,

        /// <summary>Compression error: %s</summary>
        Compression = 119,

        /// <summary>Snapshot is already in progress</summary>
        CheckpointInProgress = 120,

        /// <summary>Can not execute a nested statement: nesting limit reached</summary>
        SubStmtMax = 121,

        /// <summary>Can not commit transaction in a nested statement</summary>
        CommitInSubStmt = 122,

        /// <summary>Rollback called in a nested statement</summary>
        RollbackInSubStmt = 123,

        /// <summary>Decompression error: %s</summary>
        Decompression = 124,

        /// <summary>Invalid xlog type: expected %s, got %s</summary>
        InvalidXlogType = 125,

        /// <summary>Failed to lock WAL directory %s and hot_standby mode is off</summary>
        AlreadyRunning = 126,

        /// <summary>Indexed field count limit reached: %d indexed fields</summary>
        IndexFieldCountLimit = 127,

        /// <summary> The local instance id %u is read-only</summary>
        LocalInstanceIdIsReadOnly = 128,

        /// <summary>Backup is already in progress</summary>
        BackupInProgress = 129,

        /// <summary>The read view is aborted</summary>
        ReadViewAborted = 130,

        /// <summary>Invalid INDEX file %s: %s</summary>
        InvalidIndexFile = 131,

        /// <summary>Invalid RUN file: %s</summary>
        InvalidRunFile = 132,

        /// <summary>Invalid VYLOG file: %s</summary>
        InvalidVylogFile = 133,

        /// <summary>Can't start a checkpoint while in cascading rollback</summary>
        CheckpointRollback = 134,

        /// <summary>Timed out waiting for Vinyl memory quota</summary>
        VyQuotaTimeout = 135,

        /// <summary>
        ///     %s index  does not support selects via a partial key (expected %u parts, got %u). Please Consider changing
        ///     index type to TREE.
        /// </summary>
        PartialKey = 136,

        /// <summary>Can't truncate a system space, space '%s'</summary>
        TruncateSystemSpace = 137,

        /// <summary>Failed to dynamically load module '%.*s': %s</summary>
        LoadModule = 138,

        /// <summary>Failed to allocate %u bytes for tuple: tuple is too large. Check 'vinyl_max_tuple_size' configuration option.</summary>
        VinylMaxTupleSize = 139,

        /// <summary>Wrong _schema version: expected 'major.minor[.patch]'</summary>
        WrongDdVersion = 140,

        /// <summary>Wrong space format (field %u): %s</summary>
        WrongSpaceFormat = 141,

        /// <summary>Failed to create sequence '%s': %s</summary>
        CreateSequence = 142,

        /// <summary>Can't modify sequence '%s': %s</summary>
        AlterSequence = 143,

        /// <summary>Can't drop sequence '%s': %s</summary>
        DropSequence = 144,

        /// <summary>Sequence '%s' does not exist</summary>
        NoSuchSequence = 145,

        /// <summary>Sequence '%s' already exists</summary>
        SequenceExists = 146,

        /// <summary>Sequence '%s' has overflowed</summary>
        SequenceOverflow = 147,

        /// <summary>%s access is denied for user '%s' to sequence '%s'</summary>
        SequenceAccessDenied = 148,

        /// <summary>Space field '%s' is duplicate</summary>
        SpaceFieldIsDuplicate = 149,

        /// <summary>Failed to initialize collation: %s.</summary>
        CantCreateCollation = 150,

        /// <summary>Wrong collation options (field %u): %s</summary>
        WrongCollationOptions = 151,

        /// <summary>Primary index of the space '%s' can not contain nullable parts</summary>
        NullablePrimary = 152,

        /// <summary>Field %d is %s in space format, but %s in index parts</summary>
        NullableMismatch = 153
    }
}

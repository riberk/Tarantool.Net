namespace Tarantool.Net.Driver
{
    public enum RequestType
    {
        /// <summary>Acknowledgement that request or command is successful</summary>
        Ok = 0,

        /// <summary>SELECT request</summary>
        Select = 1,

        /// <summary>INSERT request</summary>
        Insert = 2,

        /// <summary>REPLACE request</summary>
        Replace = 3,

        /// <summary>UPDATE request</summary>
        Update = 4,

        /// <summary>DELETE request</summary>
        Delete = 5,

        /// <summary>CALL request - wraps result into [tuple, tuple, ...] format</summary>
        Call16 = 6,

        /// <summary>AUTH request</summary>
        Auth = 7,

        /// <summary>EVAL request</summary>
        Eval = 8,

        /// <summary>UPSERT request</summary>
        Upsert = 9,

        /// <summary>CALL request - returns arbitrary MessagePack</summary>
        Call = 10,

        //TODO TypeStatMax

        /// <summary>PING request</summary>
        Ping = 64,

        /// <summary>Replication JOIN command</summary>
        Join = 65,

        /// <summary>Replication SUBSCRIBE command</summary>
        Subscribe = 66,

        /// <summary>Vinyl run info stored in .index file</summary>
        VyIndexRunInfo = 100,

        /// <summary>Vinyl page info stored in .index file</summary>
        VyIndexPageInfo = 101,

        /// <summary>Vinyl row index stored in .run file</summary>
        VyRunRowIndex = 102,


        /// <summary>Error codes = (TypeError | ER_XXX from errcode.h)</summary>
        TypeError = 1 << 15
    }
}

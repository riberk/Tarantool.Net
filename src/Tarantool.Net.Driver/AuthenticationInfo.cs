using System;

namespace Tarantool.Net.Driver
{
    public struct AuthenticationInfo
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public AuthenticationInfo(string userName, byte[] chapSha1)
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            ChapSha1 = chapSha1 ?? throw new ArgumentNullException(nameof(chapSha1));
        }

        public string UserName { get; }

        public byte[] ChapSha1 { get; }
    }
}
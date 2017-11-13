using System;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class AuthenticationInfo
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public AuthenticationInfo(string userName, string method, [NotNull] byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Auth = ("chap-sha1", value);
        }

        [MapKey(Key.Tuple)]
        public (string, byte[]) Auth { get; }

        [MapKey(Key.UserName)]
        public string UserName { get; }

        public byte[] ChapSha1 => Auth.Item2;
    }
}

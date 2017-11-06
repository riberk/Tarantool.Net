using System;

namespace Tarantool.Net.Driver
{
    public struct ConnectionInfo
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public ConnectionInfo(string version, byte[] salt)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        }

        public string Version { get; }

        public byte[] Salt { get; }

    }
}
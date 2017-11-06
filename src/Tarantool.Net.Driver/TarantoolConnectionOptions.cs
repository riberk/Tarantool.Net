using System;

namespace Tarantool.Net.Driver
{
    public class TarantoolConnectionOptions
    {
        public TarantoolConnectionOptions(string host, int port)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        public TarantoolConnectionOptions(string host, int port, string userName, string password)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }

        public string Host { get; }

        public int Port { get; }

        public string UserName { get; }

        public string Password { get; }
    }
}
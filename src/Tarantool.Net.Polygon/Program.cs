using System;
using System.Threading;
using System.Threading.Tasks;
using Tarantool.Net.Driver;
using Tarantool.Net.MessagePackCli;

namespace Tarantool.Net.Polygon
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var resolver = new MessagePackResolver();
            using (var connection = new BinaryConnection(resolver, resolver))
            {
                await connection.OpenAsync("localhost", 3301);
                await connection.AuthenticateAsync("myuser", "X", CancellationToken.None);
            }
        }
    }
}

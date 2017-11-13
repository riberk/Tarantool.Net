using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Serialization;
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
            var @int = await Serialize(resolver, () => 1029);
            var intTuple = await Serialize(resolver, () => Tuple.Create(1029));
            var intVTuple = await Serialize(resolver, () => ValueTuple.Create(1029));
            var intAr = await Serialize(resolver, () => new []{1029});
            var res = @int + "\n" + intTuple + "\n" + intVTuple + "\n" + intAr;

            using (var connection = new BinaryConnection(resolver, resolver, new ReaderFactory(), new ChapSha1AuthenticationInfoFactory(), resolver))
            {
                await connection.OpenAsync("localhost", 3301, CancellationToken.None);
                
                await connection.AuthenticateAsync("admin", "adminPassword", CancellationToken.None);
                var enumerator2 = await connection.Select<int[], MyEntity>(new SelectRequest<int[]>(512, 0, uint.MaxValue, 0, IteratorType.Eq, new []{2}), CancellationToken.None);
                
                var result = await AsyncEnumerable.CreateEnumerable(() => enumerator2).ToList();
            }
        }

        private static async Task<string> Serialize<T>(ISerializerResolver resolver, Func<T> func)
        {
            var ms = new MemoryStream();
            var s = resolver.Resolve<SelectRequest<T>>();
            await s.SerializeAsync(ms, new SelectRequest<T>(513, 0, uint.MaxValue, 0, IteratorType.Eq, func()), CancellationToken.None);
            var sb = new StringBuilder();
            ms.ToArray().Aggregate(sb, (b, v) => b.Append(v.ToString("X2") + " "));
            return sb.ToString();
        }
    }

    public class MyEntity
    {
        [MessagePackMember(1)]
        public int A { get; set; }

        [MessagePackMember(2)]
        public string B { get; set; }
    }
}

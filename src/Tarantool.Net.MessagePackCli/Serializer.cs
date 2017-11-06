using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.MessagePackCli
{
    public class Serializer<T> : ISerializer<T>
    {
        public Task SerializeAsync(Stream s, T value, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }
    }
}

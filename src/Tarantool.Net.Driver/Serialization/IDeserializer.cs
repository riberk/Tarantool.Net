using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Driver.Serialization
{
    public interface IDeserializer<T>
    {
        [NotNull]
        Task<T> DeserializeAsync(Stream s, CancellationToken ct);
    }
}

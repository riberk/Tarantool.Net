using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Abstractions.Serialization
{
    public interface IDeserializer<T>
    {
        [NotNull]
        Task<(int DeserializedBytes, T Value)> DeserializeAsync(Stream s, CancellationToken ct);
    }
}

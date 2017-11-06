using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Abstractions.Serialization
{
    public interface ISerializer<in T>
    {
        /// <summary>
        /// Serialize object as message pack to a stream
        /// </summary>
        /// <param name="s">Destiation stream</param>
        /// <param name="value">Serialized value</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Writed bytes</returns>
        [NotNull]
        Task<int> SerializeAsync(Stream s, T value, CancellationToken ct);
    }
}

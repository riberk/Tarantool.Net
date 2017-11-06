using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Driver.Serialization
{
    public interface ISerializer<in T>
    {
        /// <summary>
        /// Serialize object as message pack to the stream
        /// </summary>
        /// <param name="s">Destiation stream</param>
        /// <param name="value">Serialized value</param>
        /// <param name="ct">Cancellation token</param>
        [NotNull]
        Task SerializeAsync(Stream s, T value, CancellationToken ct);
    }
}

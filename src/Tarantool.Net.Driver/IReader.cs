using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public interface IReader
    {
        /// <summary>
        /// Read the header of next message and lock reading. Lock released on <seealso cref="ResponseInfo"/> disposed
        /// </summary>
        /// <returns><seealso cref="ResponseInfo">Information of current response</seealso></returns>
        [NotNull]
        Task<ResponseInfo> ReadNextAsync(CancellationToken ct);

        /// <summary>
        /// Read the current response to end. 
        /// </summary>
        /// <exception cref="TarantoolException">If current response has error</exception>
        /// <returns>A task that will complete when the response has been readed.</returns>
        [NotNull]
        Task ReadBodyAsync(ResponseInfo responseInfo, CancellationToken ct);

        [NotNull]
        Task ReadBodyToAsync(ResponseInfo responseInfo, Stream s, CancellationToken ct);

        /// <summary>
        /// Read the current response to end and deserilize body.
        /// </summary>
        /// <exception cref="TarantoolException">If current response has error</exception>
        /// <typeparam name="T">The type of deserialized value</typeparam>
        /// <returns>A task that will complete when the response has been readed and deserialized.</returns>
        [NotNull]
        Task<T> ReadBodyAsync<T>(ResponseInfo responseInfo, CancellationToken ct);

        [NotNull]
        Task<ConnectionInfo> ReadConnectionInfo(CancellationToken ct);

        Task<AsyncResult<ErrorResponse>> TryReadError(ResponseInfo resul, CancellationToken ct);
        Task<int> ReadArrayHeader(CancellationToken ct);
        Task<int> ReadMapHeader(CancellationToken ct);
        Task<int> ReadInt(CancellationToken ct);
        Task<T> ReadAsync<T>(IDeserializer<T> deserializer, CancellationToken ct);
        Task EnsureSuccessResponse(ResponseInfo result, CancellationToken ct);
    }
}
